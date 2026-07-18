#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using lilToon;

namespace dennokoworks
{
    public class ShadowExInspector : lilToonInspector
    {
        // Custom properties
        MaterialProperty customSSAOEnabled;
        MaterialProperty customSSAOColor;
        MaterialProperty customSSAOStrength;
        MaterialProperty customSSAOPower;
        MaterialProperty customSSAOSampleLength;
        MaterialProperty customSSAOMinDistance;
        MaterialProperty customSSAOMaxDistance;
        MaterialProperty customSSAOBias;
        MaterialProperty customSSAOBlur;
        MaterialProperty customSSAODither;
        MaterialProperty customSSAOQuality;

        // Extra Normal Maps (2 packed)
        MaterialProperty customExtraNormalEnabled;
        MaterialProperty customExtraNormal1stTex;
        MaterialProperty customExtraNormal2ndTex;
        MaterialProperty customExtraNormalStrengthA;
        MaterialProperty customExtraNormalStrengthB;
        MaterialProperty customExtraNormal1stScale;
        MaterialProperty customExtraNormal2ndScale;
        MaterialProperty customExtraNormal1stMaskChannel;
        MaterialProperty customExtraNormal2ndMaskChannel;

        // Rim Light 2nd
        MaterialProperty customRim2ndEnabled;
        MaterialProperty customRim2ndMode;
        MaterialProperty customRim2ndColor;
        MaterialProperty customRim2ndPower;
        MaterialProperty customRim2ndBorder;
        MaterialProperty customRim2ndBlur;
        MaterialProperty customRim2ndEnableLighting;
        MaterialProperty customRim2ndMainStrength;
        MaterialProperty customRim2ndShadowMask;
        MaterialProperty customRim2ndDepthWidth;
        MaterialProperty customRim2ndDepthThreshold;

        // Contact Shadow (Screen Space Shadows)
        MaterialProperty customContactShadowEnabled;
        MaterialProperty customContactShadowColor;
        MaterialProperty customContactShadowLength;
        MaterialProperty customContactShadowThickness;
        MaterialProperty customContactShadowBias;
        MaterialProperty customContactShadowBlur;
        MaterialProperty customContactShadowBlurStrength;
        MaterialProperty customContactShadowQuality;
        MaterialProperty customContactShadowDither;
        MaterialProperty customContactShadowMaskChannel;

        // Rim Shade
        MaterialProperty customRimShadeEnabled;
        MaterialProperty customRimShadeColor;
        MaterialProperty customRimShadeBorder;
        MaterialProperty customRimShadeBlur;
        MaterialProperty customRimShadeFresnelPower;
        MaterialProperty customRimShadeMaskChannel;

        // MatCap Layers (up to 3)
        readonly MaterialProperty[] customMatCapLayerEnabled        = new MaterialProperty[3];
        readonly MaterialProperty[] customMatCapLayerTex            = new MaterialProperty[3];
        readonly MaterialProperty[] customMatCapLayerColor          = new MaterialProperty[3];
        readonly MaterialProperty[] customMatCapLayerBlendMode      = new MaterialProperty[3];
        readonly MaterialProperty[] customMatCapLayerEnableLighting = new MaterialProperty[3];
        readonly MaterialProperty[] customMatCapLayerShadowMask     = new MaterialProperty[3];
        readonly MaterialProperty[] customMatCapLayerMaskChannel    = new MaterialProperty[3];

        // Additional Specular + shared FX mask
        MaterialProperty customSpecEnabled;
        MaterialProperty customSpecColor;
        MaterialProperty customSpecSmoothness;
        MaterialProperty customSpecStrength;
        MaterialProperty customSpecBlendMode;
        MaterialProperty customSpecEnableLighting;
        MaterialProperty customSpecShadowMask;
        MaterialProperty customSpecMaskChannel;
        MaterialProperty customFXMask;
        MaterialProperty customFXMask2;

        // 共有FXマスクのチャンネル選択肢 (0-3=Mask1 RGBA / 4-7=Mask2 RGBA)
        private static readonly string[] maskChannelNames =
            { "Mask1 R", "Mask1 G", "Mask1 B", "Mask1 A", "Mask2 R", "Mask2 G", "Mask2 B", "Mask2 A" };

        private static bool isShowFXMask
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowFXMask", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowFXMask", value);
        }
        private static bool isShowCustomProperties
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowCustomProperties", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowCustomProperties", value);
        }
        private static bool isShowContactShadow
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowContactShadow", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowContactShadow", value);
        }
        private static bool isShowExtraNormal
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowExtraNormal", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowExtraNormal", value);
        }
        private static bool isShowRim2nd
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowRim2nd", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowRim2nd", value);
        }
        private static bool isShowRimShade
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowRimShade", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowRimShade", value);
        }
        private static bool isShowSpec
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowSpec", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowSpec", value);
        }
        private static bool isShowMatCapLayers
        {
            get => EditorPrefs.GetBool("ShadowExInspector_isShowMatCapLayers", false);
            set => EditorPrefs.SetBool("ShadowExInspector_isShowMatCapLayers", value);
        }
        private const string shaderName = "dennokoworks/ShadowEx";

        // レンダーモード一覧を一時的にコア3種へ絞る際、元の一覧を退避しておく。
        private static string[] savedRenderingModeList;

        public enum CustomPropertyBlock
        {
            SharedFXMask,
            SSAO,
            ContactShadow,
            ExtraNormals,
            RimLight2nd,
            RimShade,
            AdditionalSpecular,
            MatCapLayers,
            MatCapLayer1,
            MatCapLayer2,
            MatCapLayer3
        }

        private static readonly System.Collections.Generic.Dictionary<string, MaterialProperty> copiedProperties = new System.Collections.Generic.Dictionary<string, MaterialProperty>();
        private static readonly System.Collections.Generic.Dictionary<string, Vector4> copiedVectorProperties = new System.Collections.Generic.Dictionary<string, Vector4>();

        private struct CustomPropertyBlockData
        {
            public CustomPropertyBlock propertyBlock;
            public bool shouldCopyTex;
        }

        protected override void LoadCustomProperties(MaterialProperty[] props, Material material)
        {
            isCustomShader = true;

            // If you want to change rendering modes in the editor, specify the shader here
            ReplaceToCustomShaders();
            isShowRenderMode = !material.shader.name.Contains("Optional");

            // レンダーモードのドロップダウン整理:
            // 最小コア化(Phase 0)で Fur/Gem/Refraction/Tessellation を削除したため、
            // 一覧をコア3種(Opaque/Cutout/Transparent)へ絞り、未対応モードを選べなくする。
            // sRenderingModeList はプロジェクト共有の静的配列なので、ここで一時的に差し替え、
            // DrawCustomProperties 冒頭で必ず元へ戻し、他シェーダーへ波及させない。
            if(savedRenderingModeList == null)
            {
                var full = lilLanguageManager.sRenderingModeList;
                if(full != null && full.Length > 3)
                {
                    savedRenderingModeList = full;
                    lilLanguageManager.sRenderingModeList = new[]{ full[0], full[1], full[2] };
                }
            }

            customSSAOEnabled      = FindProperty("_CustomSSAOEnabled",      props, false);
            customSSAOColor        = FindProperty("_CustomSSAOColor",        props, false);
            customSSAOStrength     = FindProperty("_CustomSSAOStrength",     props, false);
            customSSAOPower        = FindProperty("_CustomSSAOPower",        props, false);
            customSSAOSampleLength = FindProperty("_CustomSSAOSampleLength", props, false);
            customSSAOMinDistance  = FindProperty("_CustomSSAOMinDistance",  props, false);
            customSSAOMaxDistance  = FindProperty("_CustomSSAOMaxDistance",  props, false);
            customSSAOBias         = FindProperty("_CustomSSAOBias",         props, false);
            customSSAOBlur         = FindProperty("_CustomSSAOBlur",         props, false);
            customSSAODither       = FindProperty("_CustomSSAODither",       props, false);
            customSSAOQuality      = FindProperty("_CustomSSAOQuality",      props, false);

            customContactShadowEnabled     = FindProperty("_CustomContactShadowEnabled",     props, false);
            customContactShadowColor       = FindProperty("_CustomContactShadowColor",       props, false);
            customContactShadowLength      = FindProperty("_CustomContactShadowLength",      props, false);
            customContactShadowThickness   = FindProperty("_CustomContactShadowThickness",   props, false);
            customContactShadowBias        = FindProperty("_CustomContactShadowBias",        props, false);
            customContactShadowBlur         = FindProperty("_CustomContactShadowBlur",         props, false);
            customContactShadowBlurStrength = FindProperty("_CustomContactShadowBlurStrength", props, false);
            customContactShadowQuality      = FindProperty("_CustomContactShadowQuality",      props, false);
            customContactShadowDither      = FindProperty("_CustomContactShadowDither",      props, false);
            customContactShadowMaskChannel = FindProperty("_CustomContactShadowMaskChannel", props, false);

            customExtraNormalEnabled        = FindProperty("_CustomExtraNormalEnabled",        props, false);
            customExtraNormal1stTex         = FindProperty("_CustomExtraNormal1stTex",         props, false);
            customExtraNormal2ndTex         = FindProperty("_CustomExtraNormal2ndTex",         props, false);
            customExtraNormalStrengthA      = FindProperty("_CustomExtraNormalStrengthA",      props, false);
            customExtraNormalStrengthB      = FindProperty("_CustomExtraNormalStrengthB",      props, false);
            customExtraNormal1stScale       = FindProperty("_CustomExtraNormal1stScale",       props, false);
            customExtraNormal2ndScale       = FindProperty("_CustomExtraNormal2ndScale",       props, false);
            customExtraNormal1stMaskChannel = FindProperty("_CustomExtraNormal1stMaskChannel", props, false);
            customExtraNormal2ndMaskChannel = FindProperty("_CustomExtraNormal2ndMaskChannel", props, false);

            customRim2ndEnabled        = FindProperty("_CustomRim2ndEnabled",        props, false);
            customRim2ndMode           = FindProperty("_CustomRim2ndMode",           props, false);
            customRim2ndColor          = FindProperty("_CustomRim2ndColor",          props, false);
            customRim2ndPower          = FindProperty("_CustomRim2ndPower",          props, false);
            customRim2ndBorder         = FindProperty("_CustomRim2ndBorder",         props, false);
            customRim2ndBlur           = FindProperty("_CustomRim2ndBlur",           props, false);
            customRim2ndEnableLighting = FindProperty("_CustomRim2ndEnableLighting", props, false);
            customRim2ndMainStrength   = FindProperty("_CustomRim2ndMainStrength",   props, false);
            customRim2ndShadowMask     = FindProperty("_CustomRim2ndShadowMask",     props, false);
            customRim2ndDepthWidth     = FindProperty("_CustomRim2ndDepthWidth",     props, false);
            customRim2ndDepthThreshold = FindProperty("_CustomRim2ndDepthThreshold", props, false);

            customRimShadeEnabled      = FindProperty("_CustomRimShadeEnabled",      props, false);
            customRimShadeColor        = FindProperty("_CustomRimShadeColor",        props, false);
            customRimShadeBorder       = FindProperty("_CustomRimShadeBorder",       props, false);
            customRimShadeBlur         = FindProperty("_CustomRimShadeBlur",         props, false);
            customRimShadeFresnelPower = FindProperty("_CustomRimShadeFresnelPower", props, false);
            customRimShadeMaskChannel  = FindProperty("_CustomRimShadeMaskChannel",  props, false);

            for(int i = 0; i < 3; i++)
            {
                string prefix = "_CustomMatCapLayer" + (i + 1);
                customMatCapLayerEnabled[i]        = FindProperty(prefix + "Enabled",        props, false);
                customMatCapLayerTex[i]            = FindProperty(prefix + "Tex",            props, false);
                customMatCapLayerColor[i]          = FindProperty(prefix + "Color",          props, false);
                customMatCapLayerBlendMode[i]      = FindProperty(prefix + "BlendMode",      props, false);
                customMatCapLayerEnableLighting[i] = FindProperty(prefix + "EnableLighting", props, false);
                customMatCapLayerShadowMask[i]     = FindProperty(prefix + "ShadowMask",     props, false);
                customMatCapLayerMaskChannel[i]    = FindProperty(prefix + "MaskChannel",    props, false);
            }

            customSpecEnabled        = FindProperty("_CustomSpecEnabled",        props, false);
            customSpecColor          = FindProperty("_CustomSpecColor",          props, false);
            customSpecSmoothness     = FindProperty("_CustomSpecSmoothness",     props, false);
            customSpecStrength       = FindProperty("_CustomSpecStrength",       props, false);
            customSpecBlendMode      = FindProperty("_CustomSpecBlendMode",      props, false);
            customSpecEnableLighting = FindProperty("_CustomSpecEnableLighting", props, false);
            customSpecShadowMask     = FindProperty("_CustomSpecShadowMask",     props, false);
            customSpecMaskChannel    = FindProperty("_CustomSpecMaskChannel",    props, false);
            customFXMask             = FindProperty("_CustomFXMask",             props, false);
            customFXMask2            = FindProperty("_CustomFXMask2",            props, false);
        }

        protected override void DrawCustomProperties(Material material)
        {
            // レンダーモード一覧を元へ戻す(ドロップダウンは既に描画済み。他シェーダーへ波及させない)。
            if(savedRenderingModeList != null)
            {
                lilLanguageManager.sRenderingModeList = savedRenderingModeList;
                savedRenderingModeList = null;
            }

            isShowFXMask = Foldout("Shared FX Mask", "Shared FX Mask", isShowFXMask);
            DrawMenuButton(CustomPropertyBlock.SharedFXMask);
            if(isShowFXMask)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Shared FX Mask", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customFXMask != null)
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent("FX Mask 1 (RGBA)"), customFXMask);
                if(customFXMask2 != null)
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent("FX Mask 2 (RGBA)"), customFXMask2);

                if(GUILayout.Button("FX Mask Packer を開く"))
                    ShadowExMaskPacker.Open();

                EditorGUILayout.HelpBox(
                    "複数の質感FX(リムシェード / MatCapレイヤー / コンタクトシャドウ / 追加スペキュラ)で共有するRGBAパックマスクです。\n" +
                    "各FXのMask Channelで2枚xRGBAの計8チャンネルから使用chを選びます。\n" +
                    "有効なFXが実際に参照しているマスクだけがサンプルされます(FXの数によらずピクセルあたり最大2サンプル)。\n" +
                    "マスク画像のパッキング(1~4枚をRGBAへ合成)はFX Mask Packerで行えます。" +
                    "インポート設定はsRGB(Color Texture)をオフ(Linear)推奨です。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowCustomProperties = Foldout("SSAO (Angle Based)", "SSAO (Angle Based)", isShowCustomProperties);
            DrawMenuButton(CustomPropertyBlock.SSAO);
            if(isShowCustomProperties)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("SSAO (Angle Based)", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customSSAOEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool ssaoEnabled = EditorGUILayout.Toggle("Enable SSAO", customSSAOEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customSSAOEnabled.floatValue = ssaoEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!ssaoEnabled);
                }

                if(customSSAOColor        != null) m_MaterialEditor.ShaderProperty(customSSAOColor,        "Occlusion Color");
                if(customSSAOStrength     != null) m_MaterialEditor.ShaderProperty(customSSAOStrength,     "Strength");
                if(customSSAOPower        != null) m_MaterialEditor.ShaderProperty(customSSAOPower,        "Power");
                if(customSSAOSampleLength != null) m_MaterialEditor.ShaderProperty(customSSAOSampleLength, "Sample Length (m)");
                if(customSSAOMinDistance  != null) m_MaterialEditor.ShaderProperty(customSSAOMinDistance,  "Min Distance (m)");
                if(customSSAOMaxDistance  != null) m_MaterialEditor.ShaderProperty(customSSAOMaxDistance,  "Max Distance (m)");
                if(customSSAOBias         != null) m_MaterialEditor.ShaderProperty(customSSAOBias,         "Depth Bias (m)");
                if(customSSAOBlur         != null) m_MaterialEditor.ShaderProperty(customSSAOBlur,         "Blur");

                if(customSSAOQuality != null) m_MaterialEditor.ShaderProperty(customSSAOQuality, "Quality (x12 samples)");

                if(customSSAODither != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool dither = EditorGUILayout.Toggle("Dither (IGN)", customSSAODither.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customSSAODither.floatValue = dither ? 1f : 0f;
                }

                if(customSSAOEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "SSAOは_CameraDepthTextureが有効な環境でのみ描画されます。" +
                    "VRChatではシャドウ付きDirectional Lightが存在するワールドで有効になります。\n" +
                    "Blur: AOの境界とバンディングをソフト化します(追加コストなし)。" +
                    "Powerを1未満にすると濃淡のグラデーションも柔らかくなります。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowContactShadow = Foldout("Contact Shadow", "Contact Shadow", isShowContactShadow);
            DrawMenuButton(CustomPropertyBlock.ContactShadow);
            if(isShowContactShadow)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Contact Shadow", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customContactShadowEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool csEnabled = EditorGUILayout.Toggle("Enable Contact Shadow", customContactShadowEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customContactShadowEnabled.floatValue = csEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!csEnabled);
                }

                if(customContactShadowColor     != null) m_MaterialEditor.ShaderProperty(customContactShadowColor,     "Shadow Color (Multiply)");
                if(customContactShadowLength    != null) m_MaterialEditor.ShaderProperty(customContactShadowLength,    "Ray Length (m)");
                if(customContactShadowThickness != null) m_MaterialEditor.ShaderProperty(customContactShadowThickness, "Thickness (m)");
                if(customContactShadowBias      != null) m_MaterialEditor.ShaderProperty(customContactShadowBias,      "Depth Bias (m)");
                if(customContactShadowBlur      != null) m_MaterialEditor.ShaderProperty(customContactShadowBlur,      "Blur");
                if(customContactShadowBlurStrength != null) m_MaterialEditor.ShaderProperty(customContactShadowBlurStrength, "Blur Strength");
                if(customContactShadowQuality   != null) m_MaterialEditor.ShaderProperty(customContactShadowQuality,   "Quality (x8 steps)");

                if(customContactShadowDither != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool dither = EditorGUILayout.Toggle("Dither (IGN)", customContactShadowDither.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customContactShadowDither.floatValue = dither ? 1f : 0f;
                }

                DrawMaskChannelPopup(customContactShadowMaskChannel);

                if(customContactShadowEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "深度バッファをライト方向へレイマーチして近距離の接地影を出すスクリーンスペースシャドウです。\n" +
                    "SSAO同様、シャドウ付きDirectional Lightのあるワールドでのみ動作します(無効時は素通し)。\n" +
                    "画面内に映っている遮蔽物しか影を落とせないため、Ray Lengthは短め(数cm)の近距離用途が前提です。\n" +
                    "Blurで境界をぼかせます(0=くっきり)。Blur Strengthはその効き具合の倍率で、1超でさらに強くぼかせます(影は薄く柔らかくなります)。" +
                    "遮蔽の深さと遮蔽物までの距離に応じて解析的にソフト化するため追加コストはありません。\n" +
                    "影はShadow Colorの乗算で暗くなり、リム2nd/追加スペキュラのShadow Maskとも連動します。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowExtraNormal = Foldout("Extra Normals", "Extra Normals", isShowExtraNormal);
            DrawMenuButton(CustomPropertyBlock.ExtraNormals);
            if(isShowExtraNormal)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Extra Normals", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customExtraNormalEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool exEnabled = EditorGUILayout.Toggle("Enable Extra Normals", customExtraNormalEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customExtraNormalEnabled.floatValue = exEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!exEnabled);
                }

                EditorGUILayout.LabelField("Normal 1st", EditorStyles.boldLabel);
                if(customExtraNormal1stTex != null)
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Normal Map 1st"), customExtraNormal1stTex);
                if(customExtraNormalStrengthA != null) m_MaterialEditor.ShaderProperty(customExtraNormalStrengthA, "Strength");
                DrawTilingField(customExtraNormal1stScale, "Tiling");
                DrawMaskChannelPopup(customExtraNormal1stMaskChannel);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Normal 2nd", EditorStyles.boldLabel);
                if(customExtraNormal2ndTex != null)
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Normal Map 2nd"), customExtraNormal2ndTex);
                if(customExtraNormalStrengthB != null) m_MaterialEditor.ShaderProperty(customExtraNormalStrengthB, "Strength");
                DrawTilingField(customExtraNormal2ndScale, "Tiling");
                DrawMaskChannelPopup(customExtraNormal2ndMaskChannel);

                if(customExtraNormalEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "個別の接線空間ノーマルマップを2枚合成します。1st/2ndはそれぞれのTilingで別々にサンプルされます。" +
                    "各StrengthはMask Channel(最上部のShared FX Mask 2枚xRGBA=8ch)の値で範囲制御されます(マスク白=全開)。" +
                    "テクスチャのインポート設定は「Normal map」にしてください(lilToon本体のノーマルマップと同じ扱い)。" +
                    "また本機能はlilToon本体のノーマルマップ機能が有効なときに合成されます。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowRim2nd = Foldout("Rim Light 2nd", "Rim Light 2nd", isShowRim2nd);
            DrawMenuButton(CustomPropertyBlock.RimLight2nd);
            if(isShowRim2nd)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Rim Light 2nd", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                bool rimEnabled = false;
                if(customRim2ndEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    rimEnabled = EditorGUILayout.Toggle("Enable Rim 2nd", customRim2ndEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customRim2ndEnabled.floatValue = rimEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!rimEnabled);
                }

                // Mode: 0=Fresnel / 1=Depth Contour
                int mode = 0;
                if(customRim2ndMode != null)
                {
                    EditorGUI.BeginChangeCheck();
                    mode = EditorGUILayout.Popup("Mode", (int)(customRim2ndMode.floatValue + 0.5f), new string[]{ "Fresnel", "Depth Contour" });
                    if(EditorGUI.EndChangeCheck()) customRim2ndMode.floatValue = mode;
                }

                if(customRim2ndColor          != null) m_MaterialEditor.ShaderProperty(customRim2ndColor,          "Rim Color (HDR)");
                if(customRim2ndMainStrength   != null) m_MaterialEditor.ShaderProperty(customRim2ndMainStrength,   "Main Color Strength");
                if(customRim2ndEnableLighting != null) m_MaterialEditor.ShaderProperty(customRim2ndEnableLighting, "Enable Lighting");
                if(customRim2ndShadowMask     != null) m_MaterialEditor.ShaderProperty(customRim2ndShadowMask,     "Shadow Mask");

                if(mode == 0)
                {
                    EditorGUILayout.LabelField("Fresnel", EditorStyles.boldLabel);
                    if(customRim2ndPower  != null) m_MaterialEditor.ShaderProperty(customRim2ndPower,  "Power");
                    if(customRim2ndBorder != null) m_MaterialEditor.ShaderProperty(customRim2ndBorder, "Border");
                    if(customRim2ndBlur   != null) m_MaterialEditor.ShaderProperty(customRim2ndBlur,   "Blur");
                }
                else
                {
                    EditorGUILayout.LabelField("Depth Contour", EditorStyles.boldLabel);
                    if(customRim2ndDepthWidth     != null) m_MaterialEditor.ShaderProperty(customRim2ndDepthWidth,     "Width (px)");
                    if(customRim2ndDepthThreshold != null) m_MaterialEditor.ShaderProperty(customRim2ndDepthThreshold, "Threshold (m)");
                }

                if(customRim2ndEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "エミッション段(ベースパス)で加算するリムライトです。追加ライトのパスでは加算されません。\n" +
                    "Fresnel: 視線と法線の角度から輪郭を出します(追加サンプル無し)。\n" +
                    "Depth Contour: _CameraDepthTextureでシルエット境界を検出します。" +
                    "SSAO同様シャドウ付きDirectional Lightのあるワールドでのみ動作します(無効時は素通し)。\n" +
                    "Enable Lighting=0で定数色、1でライト色に追従します。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowRimShade = Foldout("Rim Shade", "Rim Shade", isShowRimShade);
            DrawMenuButton(CustomPropertyBlock.RimShade);
            if(isShowRimShade)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Rim Shade", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customRimShadeEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool rsEnabled = EditorGUILayout.Toggle("Enable Rim Shade", customRimShadeEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customRimShadeEnabled.floatValue = rsEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!rsEnabled);
                }

                if(customRimShadeColor        != null) m_MaterialEditor.ShaderProperty(customRimShadeColor,        "Shade Color (Multiply)");
                if(customRimShadeBorder       != null) m_MaterialEditor.ShaderProperty(customRimShadeBorder,       "Border");
                if(customRimShadeBlur         != null) m_MaterialEditor.ShaderProperty(customRimShadeBlur,         "Blur");
                if(customRimShadeFresnelPower != null) m_MaterialEditor.ShaderProperty(customRimShadeFresnelPower, "Fresnel Power");

                DrawMaskChannelPopup(customRimShadeMaskChannel);

                if(customRimShadeEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "視線と法線の角度から輪郭部をShade Colorの乗算で暗くするシンプルなリムシェードです。\n" +
                    "計算はlilToon本体のRim Shadeと同等(頭方向ベースでVR両目差を抑制)ですが、" +
                    "本体と異なりLiteシェーダーでも動作します。\n" +
                    "ベースパスのエミッション直前で適用されるため、エミッションは暗くならず、" +
                    "追加ライトのパスにもコストが増えません。\n" +
                    "BorderとBlurで陰の境界位置とぼかしを調整します。アルファで全体の強度を下げられます。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowSpec = Foldout("Additional Specular", "Additional Specular", isShowSpec);
            DrawMenuButton(CustomPropertyBlock.AdditionalSpecular);
            if(isShowSpec)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Additional Specular", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customSpecEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool specEnabled = EditorGUILayout.Toggle("Enable Add Specular", customSpecEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customSpecEnabled.floatValue = specEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!specEnabled);
                }

                if(customSpecColor      != null) m_MaterialEditor.ShaderProperty(customSpecColor,      "Specular Color (HDR)");
                if(customSpecSmoothness != null) m_MaterialEditor.ShaderProperty(customSpecSmoothness, "Smoothness");
                if(customSpecStrength   != null) m_MaterialEditor.ShaderProperty(customSpecStrength,   "Strength");

                // Blend mode: 0=Normal / 1=Add / 2=Screen / 3=Multiply
                if(customSpecBlendMode != null)
                {
                    EditorGUI.BeginChangeCheck();
                    int blend = EditorGUILayout.Popup("Blend Mode", (int)(customSpecBlendMode.floatValue + 0.5f), new string[]{ "Normal", "Add", "Screen", "Multiply" });
                    if(EditorGUI.EndChangeCheck()) customSpecBlendMode.floatValue = blend;
                }

                if(customSpecEnableLighting != null) m_MaterialEditor.ShaderProperty(customSpecEnableLighting, "Enable Lighting");
                if(customSpecShadowMask     != null) m_MaterialEditor.ShaderProperty(customSpecShadowMask,     "Shadow Mask");

                DrawMaskChannelPopup(customSpecMaskChannel);

                if(customSpecEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "エミッション段(ベースパス)で加算するスタイライズドスペキュラです。追加ライトのパスでは加算されません。\n" +
                    "メインライト方向とのハーフベクトルからBlinn-Phongハイライトを算出します(追加サンプルは共有FXマスクのみ)。\n" +
                    "Mask Channelは最上部のShared FX Mask(2枚xRGBA=8ch)の使用チャンネルです。\n" +
                    "Shadow Mask=1で影部のスペキュラを抑制、Enable Lighting=1でライト色に追従します。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowMatCapLayers = Foldout("MatCap Layers", "MatCap Layers", isShowMatCapLayers);
            DrawMenuButton(CustomPropertyBlock.MatCapLayers);
            if(isShowMatCapLayers)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("MatCap Layers", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                for(int i = 0; i < 3; i++)
                {
                    if(i > 0) EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Layer " + (i + 1), EditorStyles.boldLabel);
                    DrawMenuButton(i == 0 ? CustomPropertyBlock.MatCapLayer1 : (i == 1 ? CustomPropertyBlock.MatCapLayer2 : CustomPropertyBlock.MatCapLayer3));

                    if(customMatCapLayerEnabled[i] != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        bool layerEnabled = EditorGUILayout.Toggle("Enable", customMatCapLayerEnabled[i].floatValue > 0.5f);
                        if(EditorGUI.EndChangeCheck()) customMatCapLayerEnabled[i].floatValue = layerEnabled ? 1f : 0f;

                        EditorGUI.BeginDisabledGroup(!layerEnabled);
                    }

                    if(customMatCapLayerTex[i] != null && customMatCapLayerColor[i] != null)
                        m_MaterialEditor.TexturePropertySingleLine(new GUIContent("MatCap / Color (HDR)"), customMatCapLayerTex[i], customMatCapLayerColor[i]);
                    else if(customMatCapLayerTex[i] != null)
                        m_MaterialEditor.TexturePropertySingleLine(new GUIContent("MatCap"), customMatCapLayerTex[i]);

                    // Blend mode: 0=Normal / 1=Add / 2=Screen / 3=Multiply
                    if(customMatCapLayerBlendMode[i] != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        int blend = EditorGUILayout.Popup("Blend Mode", (int)(customMatCapLayerBlendMode[i].floatValue + 0.5f), new string[]{ "Normal", "Add", "Screen", "Multiply" });
                        if(EditorGUI.EndChangeCheck()) customMatCapLayerBlendMode[i].floatValue = blend;
                    }

                    if(customMatCapLayerEnableLighting[i] != null) m_MaterialEditor.ShaderProperty(customMatCapLayerEnableLighting[i], "Enable Lighting");
                    if(customMatCapLayerShadowMask[i]     != null) m_MaterialEditor.ShaderProperty(customMatCapLayerShadowMask[i],     "Shadow Mask");

                    DrawMaskChannelPopup(customMatCapLayerMaskChannel[i]);

                    if(customMatCapLayerEnabled[i] != null) EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.HelpBox(
                    "lilToon本体のMatCap(1st/2nd)とは別に、最大3枚のMatCapを追加合成します。\n" +
                    "UVはlilToonのMatCap UV(fd.uvMat)を再利用し、サンプラーも共有するため軽量です" +
                    "(有効なレイヤー数ぶんのテクスチャサンプルのみ追加)。\n" +
                    "AddやScreenで光沢を足す場合は黒背景のMatCap、Multiplyで陰影を乗せる場合は白背景のMatCapを使ってください。\n" +
                    "ベースパス限定のため追加ライトで二重加算されず、SSAO/コンタクトシャドウはレイヤーの上からも暗く乗ります。\n" +
                    "Shadow Mask=1で影部のレイヤーを抑制、Enable Lighting=1でライト色に追従します。" +
                    "Mask Channelは最上部のShared FX Mask(2枚xRGBA=8ch)の使用チャンネルです(マスクのサンプルは全FXで共有)。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
        }

        // 共有FXマスクのチャンネル選択ポップアップ (0-3=Mask1 RGBA / 4-7=Mask2 RGBA)。
        // マスクのテクスチャスロット自体は最上部のShared FX Maskセクションに集約している。
        private void DrawMaskChannelPopup(MaterialProperty prop)
        {
            if(prop == null) return;
            EditorGUI.BeginChangeCheck();
            int current = Mathf.Clamp((int)(prop.floatValue + 0.5f), 0, maskChannelNames.Length - 1);
            int ch = EditorGUILayout.Popup("Mask Channel", current, maskChannelNames);
            if(EditorGUI.EndChangeCheck()) prop.floatValue = ch;
        }

        // Tiling(スケール)のみを2要素で編集する。オフセットは扱わない (zwは0固定)。
        private void DrawTilingField(MaterialProperty prop, string label)
        {
            if(prop == null) return;
            EditorGUI.BeginChangeCheck();
            Vector4 v = prop.vectorValue;
            Vector2 tiling = EditorGUILayout.Vector2Field(label, new Vector2(v.x, v.y));
            if(EditorGUI.EndChangeCheck()) prop.vectorValue = new Vector4(tiling.x, tiling.y, 0f, 0f);
        }

        private System.Collections.Generic.List<MaterialProperty> GetPropertiesForBlock(CustomPropertyBlock propertyBlock)
        {
            var list = new System.Collections.Generic.List<MaterialProperty>();
            switch(propertyBlock)
            {
                case CustomPropertyBlock.SharedFXMask:
                    if(customFXMask != null) list.Add(customFXMask);
                    if(customFXMask2 != null) list.Add(customFXMask2);
                    break;
                case CustomPropertyBlock.SSAO:
                    if(customSSAOEnabled != null) list.Add(customSSAOEnabled);
                    if(customSSAOColor != null) list.Add(customSSAOColor);
                    if(customSSAOStrength != null) list.Add(customSSAOStrength);
                    if(customSSAOPower != null) list.Add(customSSAOPower);
                    if(customSSAOSampleLength != null) list.Add(customSSAOSampleLength);
                    if(customSSAOMinDistance != null) list.Add(customSSAOMinDistance);
                    if(customSSAOMaxDistance != null) list.Add(customSSAOMaxDistance);
                    if(customSSAOBias != null) list.Add(customSSAOBias);
                    if(customSSAOBlur != null) list.Add(customSSAOBlur);
                    if(customSSAOQuality != null) list.Add(customSSAOQuality);
                    if(customSSAODither != null) list.Add(customSSAODither);
                    break;
                case CustomPropertyBlock.ContactShadow:
                    if(customContactShadowEnabled != null) list.Add(customContactShadowEnabled);
                    if(customContactShadowColor != null) list.Add(customContactShadowColor);
                    if(customContactShadowLength != null) list.Add(customContactShadowLength);
                    if(customContactShadowThickness != null) list.Add(customContactShadowThickness);
                    if(customContactShadowBias != null) list.Add(customContactShadowBias);
                    if(customContactShadowBlur != null) list.Add(customContactShadowBlur);
                    if(customContactShadowBlurStrength != null) list.Add(customContactShadowBlurStrength);
                    if(customContactShadowQuality != null) list.Add(customContactShadowQuality);
                    if(customContactShadowDither != null) list.Add(customContactShadowDither);
                    if(customContactShadowMaskChannel != null) list.Add(customContactShadowMaskChannel);
                    break;
                case CustomPropertyBlock.ExtraNormals:
                    if(customExtraNormalEnabled != null) list.Add(customExtraNormalEnabled);
                    if(customExtraNormal1stTex != null) list.Add(customExtraNormal1stTex);
                    if(customExtraNormalStrengthA != null) list.Add(customExtraNormalStrengthA);
                    if(customExtraNormal1stScale != null) list.Add(customExtraNormal1stScale);
                    if(customExtraNormal1stMaskChannel != null) list.Add(customExtraNormal1stMaskChannel);
                    if(customExtraNormal2ndTex != null) list.Add(customExtraNormal2ndTex);
                    if(customExtraNormalStrengthB != null) list.Add(customExtraNormalStrengthB);
                    if(customExtraNormal2ndScale != null) list.Add(customExtraNormal2ndScale);
                    if(customExtraNormal2ndMaskChannel != null) list.Add(customExtraNormal2ndMaskChannel);
                    break;
                case CustomPropertyBlock.RimLight2nd:
                    if(customRim2ndEnabled != null) list.Add(customRim2ndEnabled);
                    if(customRim2ndMode != null) list.Add(customRim2ndMode);
                    if(customRim2ndColor != null) list.Add(customRim2ndColor);
                    if(customRim2ndPower != null) list.Add(customRim2ndPower);
                    if(customRim2ndBorder != null) list.Add(customRim2ndBorder);
                    if(customRim2ndBlur != null) list.Add(customRim2ndBlur);
                    if(customRim2ndEnableLighting != null) list.Add(customRim2ndEnableLighting);
                    if(customRim2ndMainStrength != null) list.Add(customRim2ndMainStrength);
                    if(customRim2ndShadowMask != null) list.Add(customRim2ndShadowMask);
                    if(customRim2ndDepthWidth != null) list.Add(customRim2ndDepthWidth);
                    if(customRim2ndDepthThreshold != null) list.Add(customRim2ndDepthThreshold);
                    break;
                case CustomPropertyBlock.RimShade:
                    if(customRimShadeEnabled != null) list.Add(customRimShadeEnabled);
                    if(customRimShadeColor != null) list.Add(customRimShadeColor);
                    if(customRimShadeBorder != null) list.Add(customRimShadeBorder);
                    if(customRimShadeBlur != null) list.Add(customRimShadeBlur);
                    if(customRimShadeFresnelPower != null) list.Add(customRimShadeFresnelPower);
                    if(customRimShadeMaskChannel != null) list.Add(customRimShadeMaskChannel);
                    break;
                case CustomPropertyBlock.AdditionalSpecular:
                    if(customSpecEnabled != null) list.Add(customSpecEnabled);
                    if(customSpecColor != null) list.Add(customSpecColor);
                    if(customSpecSmoothness != null) list.Add(customSpecSmoothness);
                    if(customSpecStrength != null) list.Add(customSpecStrength);
                    if(customSpecBlendMode != null) list.Add(customSpecBlendMode);
                    if(customSpecEnableLighting != null) list.Add(customSpecEnableLighting);
                    if(customSpecShadowMask != null) list.Add(customSpecShadowMask);
                    if(customSpecMaskChannel != null) list.Add(customSpecMaskChannel);
                    break;
                case CustomPropertyBlock.MatCapLayer1:
                    AddMatCapLayerProperties(list, 0);
                    break;
                case CustomPropertyBlock.MatCapLayer2:
                    AddMatCapLayerProperties(list, 1);
                    break;
                case CustomPropertyBlock.MatCapLayer3:
                    AddMatCapLayerProperties(list, 2);
                    break;
                case CustomPropertyBlock.MatCapLayers:
                    AddMatCapLayerProperties(list, 0);
                    AddMatCapLayerProperties(list, 1);
                    AddMatCapLayerProperties(list, 2);
                    break;
            }
            return list;
        }

        private void AddMatCapLayerProperties(System.Collections.Generic.List<MaterialProperty> list, int index)
        {
            if(index < 0 || index >= 3) return;
            if(customMatCapLayerEnabled[index] != null) list.Add(customMatCapLayerEnabled[index]);
            if(customMatCapLayerTex[index] != null) list.Add(customMatCapLayerTex[index]);
            if(customMatCapLayerColor[index] != null) list.Add(customMatCapLayerColor[index]);
            if(customMatCapLayerBlendMode[index] != null) list.Add(customMatCapLayerBlendMode[index]);
            if(customMatCapLayerEnableLighting[index] != null) list.Add(customMatCapLayerEnableLighting[index]);
            if(customMatCapLayerShadowMask[index] != null) list.Add(customMatCapLayerShadowMask[index]);
            if(customMatCapLayerMaskChannel[index] != null) list.Add(customMatCapLayerMaskChannel[index]);
        }

        private void CopyProperties(CustomPropertyBlock propertyBlock)
        {
            var material = (Material)m_MaterialEditor.target;
            var props = GetPropertiesForBlock(propertyBlock);
            foreach(var p in props)
            {
                copiedProperties[p.name] = p;
                if(p.type == MaterialProperty.PropType.Texture)
                {
                    string stPropertyName = p.name + "_ST";
                    if(material.HasProperty(stPropertyName))
                    {
                        copiedVectorProperties[stPropertyName] = material.GetVector(stPropertyName);
                    }
                }
            }
        }

        private void PasteProperties(CustomPropertyBlock propertyBlock, bool shouldCopyTex)
        {
            var material = (Material)m_MaterialEditor.target;
            var props = GetPropertiesForBlock(propertyBlock);
            foreach(var p in props)
            {
                if(p.type == MaterialProperty.PropType.Texture && !shouldCopyTex) continue;
                if(!copiedProperties.ContainsKey(p.name) || copiedProperties[p.name] == null) continue;

                var copiedProp = copiedProperties[p.name];
                var propType = p.type;
                if(propType == MaterialProperty.PropType.Color)   p.colorValue = copiedProp.colorValue;
                if(propType == MaterialProperty.PropType.Vector)  p.vectorValue = copiedProp.vectorValue;
                if(propType == MaterialProperty.PropType.Float)   p.floatValue = copiedProp.floatValue;
                if(propType == MaterialProperty.PropType.Range)   p.floatValue = copiedProp.floatValue;
                if(propType == MaterialProperty.PropType.Texture) p.textureValue = copiedProp.textureValue;

                if(p.type == MaterialProperty.PropType.Texture && shouldCopyTex)
                {
                    string stPropertyName = p.name + "_ST";
                    if(copiedVectorProperties.ContainsKey(stPropertyName) && material.HasProperty(stPropertyName))
                    {
                        material.SetVector(stPropertyName, copiedVectorProperties[stPropertyName]);
                    }
                }
            }
        }

        private void ResetProperties(CustomPropertyBlock propertyBlock)
        {
            #if UNITY_2019_3_OR_NEWER
            var props = GetPropertiesForBlock(propertyBlock);
            foreach(var p in props)
            {
                if(p.targets == null || p.targets.Length == 0) continue;
                if(!(p.targets[0] is Material mat) || mat.shader == null) continue;

                var shader = mat.shader;
                int propID = shader.FindPropertyIndex(p.name);
                if(propID == -1) continue;

                var propType = p.type;
                if(propType == MaterialProperty.PropType.Color)   p.colorValue = shader.GetPropertyDefaultVectorValue(propID);
                if(propType == MaterialProperty.PropType.Vector)  p.vectorValue = shader.GetPropertyDefaultVectorValue(propID);
                if(propType == MaterialProperty.PropType.Float)   p.floatValue = shader.GetPropertyDefaultFloatValue(propID);
                if(propType == MaterialProperty.PropType.Range)   p.floatValue = shader.GetPropertyDefaultFloatValue(propID);
                if(propType == MaterialProperty.PropType.Texture) p.textureValue = null;
            }
            #endif
        }

        private void CopyPropertiesMenu(object obj)
        {
            CopyProperties((CustomPropertyBlock)obj);
        }

        private void PastePropertiesMenu(object obj)
        {
            var data = (CustomPropertyBlockData)obj;
            PasteProperties(data.propertyBlock, data.shouldCopyTex);
        }

        private void ResetPropertiesMenu(object obj)
        {
            ResetProperties((CustomPropertyBlock)obj);
        }

        private void OpenHelpPage(object helpAnchor)
        {
            if(helpAnchor != null)
            {
                Application.OpenURL(GetLoc("sManualURL") + helpAnchor.ToString());
            }
        }

        private void DrawMenuButton(CustomPropertyBlock propertyBlock, string helpAnchor = null)
        {
            var position = GUILayoutUtility.GetLastRect();
            position.x += position.width - 24;
            position.width = 24;

            if(GUI.Button(position, EditorGUIUtility.IconContent("_Popup"), middleButton))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent(GetLoc("sCopy")),               false, CopyPropertiesMenu,  propertyBlock);
                menu.AddItem(new GUIContent(GetLoc("sPaste")),              false, PastePropertiesMenu, new CustomPropertyBlockData{propertyBlock = propertyBlock, shouldCopyTex = false});
                menu.AddItem(new GUIContent(GetLoc("sPasteWithTexture")),   false, PastePropertiesMenu, new CustomPropertyBlockData{propertyBlock = propertyBlock, shouldCopyTex = true});
                #if UNITY_2019_3_OR_NEWER
                    menu.AddItem(new GUIContent(GetLoc("sReset")),              false, ResetPropertiesMenu, propertyBlock);
                #endif
                if(!string.IsNullOrEmpty(helpAnchor))
                {
                    menu.AddItem(new GUIContent(GetLoc("sOpenManual")),         false, OpenHelpPage,        helpAnchor);
                }
                menu.ShowAsContext();
            }
        }

        protected override void ReplaceToCustomShaders()
        {
            // --- 最小コア (Phase 0) : 標準5モード x Outline有無 + Lite のみを提供する ---
            // 対応するコンテナ(.lilcontainer)のみ残し、Tess/Fur/Gem/Refraction/OutlineOnly/
            // Overlay/Multi は削除済み。ここでは残した20バリアントを Shader.Find する。
            lts         = Shader.Find(shaderName + "/lilToon");
            ltsc        = Shader.Find("Hidden/" + shaderName + "/Cutout");
            ltst        = Shader.Find("Hidden/" + shaderName + "/Transparent");
            ltsot       = Shader.Find("Hidden/" + shaderName + "/OnePassTransparent");
            ltstt       = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparent");

            ltso        = Shader.Find("Hidden/" + shaderName + "/OpaqueOutline");
            ltsco       = Shader.Find("Hidden/" + shaderName + "/CutoutOutline");
            ltsto       = Shader.Find("Hidden/" + shaderName + "/TransparentOutline");
            ltsoto      = Shader.Find("Hidden/" + shaderName + "/OnePassTransparentOutline");
            ltstto      = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparentOutline");

            ltsl        = Shader.Find(shaderName + "/lilToonLite");
            ltslc       = Shader.Find("Hidden/" + shaderName + "/Lite/Cutout");
            ltslt       = Shader.Find("Hidden/" + shaderName + "/Lite/Transparent");
            ltslot      = Shader.Find("Hidden/" + shaderName + "/Lite/OnePassTransparent");
            ltsltt      = Shader.Find("Hidden/" + shaderName + "/Lite/TwoPassTransparent");

            ltslo       = Shader.Find("Hidden/" + shaderName + "/Lite/OpaqueOutline");
            ltslco      = Shader.Find("Hidden/" + shaderName + "/Lite/CutoutOutline");
            ltslto      = Shader.Find("Hidden/" + shaderName + "/Lite/TransparentOutline");
            ltsloto     = Shader.Find("Hidden/" + shaderName + "/Lite/OnePassTransparentOutline");
            ltsltto     = Shader.Find("Hidden/" + shaderName + "/Lite/TwoPassTransparentOutline");

            // --- 削除したバリアントのフォールバック ---
            // lilMaterialUtils.SetupMaterialWithRenderingMode は null チェック無しで
            // material.shader へ代入するため、削除モードを選ぶとマテリアルが壊れる(ピンク化)。
            // これを防ぐため、削除したバリアントは最寄りのコアシェーダーに向けておく
            // (追加シェーダーは存在しないので「最小コア」は維持される)。
            ltsoo       = ltso;   // OutlineOnly -> Outline
            ltscoo      = ltsco;
            ltstoo      = ltsto;

            ltstess     = lts;    // Tessellation -> 非Tess相当
            ltstessc    = ltsc;
            ltstesst    = ltst;
            ltstessot   = ltsot;
            ltstesstt   = ltstt;
            ltstesso    = ltso;
            ltstessco   = ltsco;
            ltstessto   = ltsto;
            ltstessoto  = ltsoto;
            ltstesstto  = ltstto;

            ltsref      = ltst;   // Refraction -> Transparent
            ltsrefb     = ltst;
            ltsgem      = ltst;   // Gem -> Transparent

            ltsfur      = lts;    // Fur -> 非Fur相当
            ltsfurc     = ltsc;
            ltsfurtwo   = ltstt;
            ltsfuro     = lts;
            ltsfuroc    = ltsc;
            ltsfurotwo  = ltstt;
            ltsfs       = lts;    // FakeShadow -> Opaque

            ltsover     = lts;    // Overlay -> Opaque / Lite
            ltsoover    = lts;
            ltslover    = ltsl;
            ltsloover   = ltsl;

            ltsm        = lts;    // Multi -> 単一コア相当
            ltsmo       = ltso;
            ltsmref     = ltst;
            ltsmfur     = lts;
            ltsmgem     = ltst;
        }

        // You can create a menu like this
        /*
        [MenuItem("Assets/dennokoworks/ShadowEx/Convert material to custom shader", false, 1100)]
        private static void ConvertMaterialToCustomShaderMenu()
        {
            if(Selection.objects.Length == 0) return;
            ShadowExInspector inspector = new ShadowExInspector();
            for(int i = 0; i < Selection.objects.Length; i++)
            {
                if(Selection.objects[i] is Material)
                {
                    inspector.ConvertMaterialToCustomShader((Material)Selection.objects[i]);
                }
            }
        }
        */
    }
}
#endif