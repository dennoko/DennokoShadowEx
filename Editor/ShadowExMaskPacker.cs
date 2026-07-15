#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace dennokoworks
{
    // ShadowEx FX Mask Packer
    // 1~4枚のマスク画像をRGBAチャンネルへパックしたPNGを生成するエディタ拡張。
    //   - 解像度が異なる場合は最大解像度に合わせてバイリニア拡大して合成する
    //   - 「合成先」に既存のパックマスクを指定すると、そのテクスチャを下地に
    //     画像を割り当てたチャンネルだけを上書きできる (チャンネル単位の差し替え)
    //   - デフォルトの保存先は最初に割り当てたマスク画像のフォルダ
    // ピクセル値はPNG/JPGならファイルバイトから直接読むため、インポート設定
    // (圧縮/最大サイズ/sRGB/Read-Write) に左右されない。
    public class ShadowExMaskPacker : EditorWindow
    {
        static readonly string[] channelNames = { "R", "G", "B", "A" };
        static readonly string[] sourceChannelNames = { "読取 R", "読取 G", "読取 B", "読取 A" };

        Texture2D baseTex;                                // 合成先 (任意)
        readonly Texture2D[] sources = new Texture2D[4];  // 出力R/G/B/Aチャンネルへの入力画像
        readonly int[] sourceChannels = new int[4];       // 各入力画像から読み取るチャンネル

        [MenuItem("Tools/dennokoworks/ShadowEx/FX Mask Packer")]
        public static void Open()
        {
            var window = GetWindow<ShadowExMaskPacker>("FX Mask Packer");
            window.minSize = new Vector2(360f, 340f);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "マスク画像(1~4枚)をRGBAチャンネルへパックしたPNGを生成します。\n" +
                "解像度が異なる場合は最大解像度に合わせて拡大合成します。\n" +
                "合成先を指定すると、そのテクスチャを下地に画像を割り当てたチャンネルだけ上書きします。\n" +
                "グレースケール画像はそのまま(読取R)、パック済み画像から抽出する場合は読取chを変更してください。",
                MessageType.Info);

            baseTex = (Texture2D)EditorGUILayout.ObjectField("合成先 (任意)", baseTex, typeof(Texture2D), false);

            EditorGUILayout.Space();
            for(int i = 0; i < 4; i++)
            {
                EditorGUILayout.BeginHorizontal();
                sources[i] = (Texture2D)EditorGUILayout.ObjectField("出力 " + channelNames[i], sources[i], typeof(Texture2D), false);
                EditorGUI.BeginDisabledGroup(sources[i] == null);
                sourceChannels[i] = EditorGUILayout.Popup(sourceChannels[i], sourceChannelNames, GUILayout.Width(72f));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }

            int outW, outH;
            GetOutputSize(out outW, out outH);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("出力解像度", outW > 0 ? outW + " x " + outH + " (入力の最大解像度)" : "-");
            EditorGUILayout.LabelField("空きチャンネル", baseTex != null ? "合成先の値を保持" : "白(1)で埋める");

            EditorGUI.BeginDisabledGroup(outW <= 0);
            if(GUILayout.Button("生成して保存...")) Pack(outW, outH);
            EditorGUI.EndDisabledGroup();
        }

        // 出力解像度 = 割り当てた入力(+合成先)の最大解像度。入力が無ければ0。
        void GetOutputSize(out int width, out int height)
        {
            width = 0;
            height = 0;
            for(int i = 0; i < 4; i++)
            {
                if(sources[i] == null) continue;
                width  = Mathf.Max(width,  sources[i].width);
                height = Mathf.Max(height, sources[i].height);
            }
            if(width <= 0) return;
            if(baseTex != null)
            {
                width  = Mathf.Max(width,  baseTex.width);
                height = Mathf.Max(height, baseTex.height);
            }
        }

        void Pack(int width, int height)
        {
            string savePath = GetSavePath();
            if(string.IsNullOrEmpty(savePath)) return;

            try
            {
                var pixels = new Color[width * height];
                if(baseTex != null)
                {
                    EditorUtility.DisplayProgressBar("FX Mask Packer", "合成先を読み込み中...", 0f);
                    ReadInto(baseTex, width, height, pixels, -1, -1);
                }
                else
                {
                    for(int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                }

                for(int ch = 0; ch < 4; ch++)
                {
                    if(sources[ch] == null) continue;
                    EditorUtility.DisplayProgressBar("FX Mask Packer", "出力 " + channelNames[ch] + " チャンネルを合成中...", (ch + 1) / 5f);
                    ReadInto(sources[ch], width, height, pixels, sourceChannels[ch], ch);
                }

                EditorUtility.DisplayProgressBar("FX Mask Packer", "PNGを書き出し中...", 0.9f);
                var outTex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                outTex.SetPixels(pixels);
                outTex.Apply();
                byte[] png = outTex.EncodeToPNG();
                DestroyImmediate(outTex);
                File.WriteAllBytes(savePath, png);

                AssetDatabase.ImportAsset(savePath);
                SetupImporter(savePath);
                var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);
                if(asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                    EditorUtility.FocusProjectWindow();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // デフォルトの保存先は最初に割り当てたマスク画像のフォルダ。
        // 合成先を指定している場合はその名前を既定にし、同じ場所を選べばそのまま上書きできる。
        string GetSavePath()
        {
            string dir = "Assets";
            string name = "FXMask_packed";
            for(int i = 0; i < 4; i++)
            {
                if(sources[i] == null) continue;
                string p = AssetDatabase.GetAssetPath(sources[i]);
                if(!string.IsNullOrEmpty(p))
                {
                    dir = Path.GetDirectoryName(p).Replace('\\', '/');
                    name = Path.GetFileNameWithoutExtension(p) + "_packed";
                }
                break;
            }
            if(baseTex != null)
            {
                string bp = AssetDatabase.GetAssetPath(baseTex);
                if(!string.IsNullOrEmpty(bp) && Path.GetExtension(bp).ToLowerInvariant() == ".png")
                    name = Path.GetFileNameWithoutExtension(bp);
            }
            return EditorUtility.SaveFilePanelInProject("FX Maskを保存", name, "png", "パックしたFXマスクの保存先を選択してください", dir);
        }

        // tex を出力解像度へリサンプルしながら pixels へ書き込む。
        //   srcCh < 0 : 全チャンネルをコピー (合成先の下地読み込み用)
        //   srcCh >= 0: tex の srcCh を pixels の dstCh へ書き込む
        static void ReadInto(Texture2D tex, int width, int height, Color[] pixels, int srcCh, int dstCh)
        {
            Texture2D readable = LoadReadable(tex);
            try
            {
                if(readable.width == width && readable.height == height)
                {
                    var src = readable.GetPixels();
                    for(int i = 0; i < pixels.Length; i++)
                    {
                        if(srcCh < 0) pixels[i] = src[i];
                        else pixels[i][dstCh] = src[i][srcCh];
                    }
                }
                else
                {
                    for(int y = 0; y < height; y++)
                    {
                        float v = (y + 0.5f) / height;
                        for(int x = 0; x < width; x++)
                        {
                            Color c = readable.GetPixelBilinear((x + 0.5f) / width, v);
                            int i = y * width + x;
                            if(srcCh < 0) pixels[i] = c;
                            else pixels[i][dstCh] = c[srcCh];
                        }
                    }
                }
            }
            finally
            {
                DestroyImmediate(readable);
            }
        }

        // インポート設定に左右されず元のピクセル値を読める複製を作る。
        // PNG/JPGはファイルバイトからデコード (圧縮・最大サイズ・sRGBの影響なし)。
        // それ以外(TGA/PSD等)はRenderTexture経由で複製する。この場合は
        // インポート結果の値になるため、sRGB設定を往復させて値の変化を抑える。
        static Texture2D LoadReadable(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if(!string.IsNullOrEmpty(path))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if(ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                    if(loaded.LoadImage(File.ReadAllBytes(path))) return loaded;
                    DestroyImmediate(loaded);
                }
            }

            bool srgb = true;
            var importer = string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer != null) srgb = importer.sRGBTexture;

            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32,
                srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            var prevActive = RenderTexture.active;
            bool prevSRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = srgb;
            Graphics.Blit(tex, rt);
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, true);
            RenderTexture.active = rt;
            copy.ReadPixels(new Rect(0f, 0f, tex.width, tex.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prevActive;
            GL.sRGBWrite = prevSRGBWrite;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        // マスクはリニアデータなので sRGB をオフにする (シェーダー側の想定と一致させる)。
        static void SetupImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer == null) return;
            if(!importer.sRGBTexture && !importer.alphaIsTransparency) return;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
    }
}
#endif
