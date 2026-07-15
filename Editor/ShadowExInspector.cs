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

        private static bool isShowCustomProperties;
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
        }

        protected override void ReplaceToCustomShaders()
        {
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

            ltsoo       = Shader.Find(shaderName + "/[Optional] OutlineOnly/Opaque");
            ltscoo      = Shader.Find(shaderName + "/[Optional] OutlineOnly/Cutout");
            ltstoo      = Shader.Find(shaderName + "/[Optional] OutlineOnly/Transparent");

            ltstess     = Shader.Find("Hidden/" + shaderName + "/Tessellation/Opaque");
            ltstessc    = Shader.Find("Hidden/" + shaderName + "/Tessellation/Cutout");
            ltstesst    = Shader.Find("Hidden/" + shaderName + "/Tessellation/Transparent");
            ltstessot   = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparent");
            ltstesstt   = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparent");

            ltstesso    = Shader.Find("Hidden/" + shaderName + "/Tessellation/OpaqueOutline");
            ltstessco   = Shader.Find("Hidden/" + shaderName + "/Tessellation/CutoutOutline");
            ltstessto   = Shader.Find("Hidden/" + shaderName + "/Tessellation/TransparentOutline");
            ltstessoto  = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparentOutline");
            ltstesstto  = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparentOutline");

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

            ltsref      = Shader.Find("Hidden/" + shaderName + "/Refraction");
            ltsrefb     = Shader.Find("Hidden/" + shaderName + "/RefractionBlur");
            ltsfur      = Shader.Find("Hidden/" + shaderName + "/Fur");
            ltsfurc     = Shader.Find("Hidden/" + shaderName + "/FurCutout");
            ltsfurtwo   = Shader.Find("Hidden/" + shaderName + "/FurTwoPass");
            ltsfuro     = Shader.Find(shaderName + "/[Optional] FurOnly/Transparent");
            ltsfuroc    = Shader.Find(shaderName + "/[Optional] FurOnly/Cutout");
            ltsfurotwo  = Shader.Find(shaderName + "/[Optional] FurOnly/TwoPass");
            ltsgem      = Shader.Find("Hidden/" + shaderName + "/Gem");
            ltsfs       = Shader.Find(shaderName + "/[Optional] FakeShadow");

            ltsover     = Shader.Find(shaderName + "/[Optional] Overlay");
            ltsoover    = Shader.Find(shaderName + "/[Optional] OverlayOnePass");
            ltslover    = Shader.Find(shaderName + "/[Optional] LiteOverlay");
            ltsloover   = Shader.Find(shaderName + "/[Optional] LiteOverlayOnePass");

            ltsm        = Shader.Find(shaderName + "/lilToonMulti");
            ltsmo       = Shader.Find("Hidden/" + shaderName + "/MultiOutline");
            ltsmref     = Shader.Find("Hidden/" + shaderName + "/MultiRefraction");
            ltsmfur     = Shader.Find("Hidden/" + shaderName + "/MultiFur");
            ltsmgem     = Shader.Find("Hidden/" + shaderName + "/MultiGem");
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