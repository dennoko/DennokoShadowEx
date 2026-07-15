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
        MaterialProperty customSSAODither;
        MaterialProperty customSSAOQuality;

        // Extra Normal Maps (2 packed)
        MaterialProperty customExtraNormalEnabled;
        MaterialProperty customExtraNormalTex;
        MaterialProperty customExtraNormalStrengthA;
        MaterialProperty customExtraNormalStrengthB;
        MaterialProperty customExtraNormal1stScale;
        MaterialProperty customExtraNormal2ndScale;

        // Rim Light 2nd
        MaterialProperty customRim2ndEnabled;
        MaterialProperty customRim2ndMode;
        MaterialProperty customRim2ndColor;
        MaterialProperty customRim2ndPower;
        MaterialProperty customRim2ndBorder;
        MaterialProperty customRim2ndBlur;
        MaterialProperty customRim2ndEnableLighting;
        MaterialProperty customRim2ndShadowMask;
        MaterialProperty customRim2ndDepthWidth;
        MaterialProperty customRim2ndDepthThreshold;

        private static bool isShowCustomProperties;
        private static bool isShowExtraNormal;
        private static bool isShowRim2nd;
        private const string shaderName = "ShadowEx";

        protected override void LoadCustomProperties(MaterialProperty[] props, Material material)
        {
            isCustomShader = true;

            // If you want to change rendering modes in the editor, specify the shader here
            ReplaceToCustomShaders();
            isShowRenderMode = !material.shader.name.Contains("Optional");

            customSSAOEnabled      = FindProperty("_CustomSSAOEnabled",      props, false);
            customSSAOColor        = FindProperty("_CustomSSAOColor",        props, false);
            customSSAOStrength     = FindProperty("_CustomSSAOStrength",     props, false);
            customSSAOPower        = FindProperty("_CustomSSAOPower",        props, false);
            customSSAOSampleLength = FindProperty("_CustomSSAOSampleLength", props, false);
            customSSAOMinDistance  = FindProperty("_CustomSSAOMinDistance",  props, false);
            customSSAOMaxDistance  = FindProperty("_CustomSSAOMaxDistance",  props, false);
            customSSAOBias         = FindProperty("_CustomSSAOBias",         props, false);
            customSSAODither       = FindProperty("_CustomSSAODither",       props, false);
            customSSAOQuality      = FindProperty("_CustomSSAOQuality",      props, false);

            customExtraNormalEnabled   = FindProperty("_CustomExtraNormalEnabled",   props, false);
            customExtraNormalTex       = FindProperty("_CustomExtraNormalTex",       props, false);
            customExtraNormalStrengthA = FindProperty("_CustomExtraNormalStrengthA", props, false);
            customExtraNormalStrengthB = FindProperty("_CustomExtraNormalStrengthB", props, false);
            customExtraNormal1stScale  = FindProperty("_CustomExtraNormal1stScale",  props, false);
            customExtraNormal2ndScale  = FindProperty("_CustomExtraNormal2ndScale",  props, false);

            customRim2ndEnabled        = FindProperty("_CustomRim2ndEnabled",        props, false);
            customRim2ndMode           = FindProperty("_CustomRim2ndMode",           props, false);
            customRim2ndColor          = FindProperty("_CustomRim2ndColor",          props, false);
            customRim2ndPower          = FindProperty("_CustomRim2ndPower",          props, false);
            customRim2ndBorder         = FindProperty("_CustomRim2ndBorder",         props, false);
            customRim2ndBlur           = FindProperty("_CustomRim2ndBlur",           props, false);
            customRim2ndEnableLighting = FindProperty("_CustomRim2ndEnableLighting", props, false);
            customRim2ndShadowMask     = FindProperty("_CustomRim2ndShadowMask",     props, false);
            customRim2ndDepthWidth     = FindProperty("_CustomRim2ndDepthWidth",     props, false);
            customRim2ndDepthThreshold = FindProperty("_CustomRim2ndDepthThreshold", props, false);
        }

        protected override void DrawCustomProperties(Material material)
        {
            isShowCustomProperties = Foldout("SSAO (Angle Based)", "SSAO (Angle Based)", isShowCustomProperties);
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
                    "VRChatではシャドウ付きDirectional Lightが存在するワールドで有効になります。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowExtraNormal = Foldout("Extra Normals (Packed)", "Extra Normals (Packed)", isShowExtraNormal);
            if(isShowExtraNormal)
            {
                EditorGUILayout.BeginVertical(boxOuter);
                EditorGUILayout.LabelField("Extra Normals (Packed)", customToggleFont);
                EditorGUILayout.BeginVertical(boxInnerHalf);

                if(customExtraNormalEnabled != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool exEnabled = EditorGUILayout.Toggle("Enable Extra Normals", customExtraNormalEnabled.floatValue > 0.5f);
                    if(EditorGUI.EndChangeCheck()) customExtraNormalEnabled.floatValue = exEnabled ? 1f : 0f;

                    EditorGUI.BeginDisabledGroup(!exEnabled);
                }

                if(customExtraNormalTex != null)
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Packed Normals (RG=1st / BA=2nd)"), customExtraNormalTex);

                EditorGUILayout.LabelField("Normal 1st (RG)", EditorStyles.boldLabel);
                if(customExtraNormalStrengthA != null) m_MaterialEditor.ShaderProperty(customExtraNormalStrengthA, "Strength");
                DrawTilingField(customExtraNormal1stScale, "Tiling");

                EditorGUILayout.LabelField("Normal 2nd (BA)", EditorStyles.boldLabel);
                if(customExtraNormalStrengthB != null) m_MaterialEditor.ShaderProperty(customExtraNormalStrengthB, "Strength");
                DrawTilingField(customExtraNormal2ndScale, "Tiling");

                if(customExtraNormalEnabled != null) EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(
                    "1枚のRGBAに2枚分のノーマルをパックします (RG=1枚目のXY / BA=2枚目のXY)。" +
                    "1st/2ndはそれぞれのTilingで別々にサンプルされます。" +
                    "テクスチャのインポート設定は「Normal map」ではなく「Default」かつ sRGB(Color Texture) をオフ(Linear)にしてください。" +
                    "Normal mapインポートはチャンネルをスウィズルするためパッキングが壊れます。" +
                    "また本機能はlilToon本体のノーマルマップ機能が有効なときに合成されます。",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            isShowRim2nd = Foldout("Rim Light 2nd", "Rim Light 2nd", isShowRim2nd);
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
        [MenuItem("Assets/ShadowEx/Convert material to custom shader", false, 1100)]
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