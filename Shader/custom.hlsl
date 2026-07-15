//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : Angle-based SSAO (UE4 style) for lilToon 2.x
// ref: https://takumifukasawa.hatenablog.com/entry/unity-ssao-custom-post-process
// ref: https://github.com/takumifukasawa/UnitySSAOBuiltinPipeline (SSAOAngleBased.shader)
//
// AO計算の本体は custom_insert.hlsl の lilShadowExCalcSSAO() に実装。
// _CameraDepthTexture が有効な場合のみ動作する(VRChatではシャドウ付き
// Directional Light が存在するワールドで有効になる)。無効時は素通し。
//----------------------------------------------------------------------------------------------------------------------

//----------------------------------------------------------------------------------------------------------------------
// Macro

// Custom variables
#define LIL_CUSTOM_PROPERTIES \
    float4 _CustomSSAOColor; \
    float  _CustomSSAOEnabled; \
    float  _CustomSSAOStrength; \
    float  _CustomSSAOPower; \
    float  _CustomSSAOSampleLength; \
    float  _CustomSSAOMinDistance; \
    float  _CustomSSAOMaxDistance; \
    float  _CustomSSAOBias; \
    float  _CustomSSAODither; \
    float  _CustomSSAOQuality;

// Custom textures
// (_CameraDepthTexture は lilToon 側 (lil_common_input.hlsl) で宣言済みのため追加宣言しない)
#define LIL_CUSTOM_TEXTURES

// Add vertex shader output
// SSAOの中心点計算にワールド座標を使うため強制的に v2f へ含める
#define LIL_V2F_FORCE_POSITION_WS

// Inserting a process into pixel shader
// エミッション加算の直前に適用することで、発光部分を暗くせずにAOをかける。
// LIL_ENABLED_DEPTH_TEX: 深度テクスチャが無いワールドでは自動的に無効化される。
#define BEFORE_EMISSION_1ST \
    if (_CustomSSAOEnabled > 0.5 && LIL_ENABLED_DEPTH_TEX) \
    { \
        float aoRate = lilShadowExCalcSSAO(fd.positionWS, fd.positionCS); \
        float aoFactor = saturate(pow(saturate(aoRate), _CustomSSAOPower) * _CustomSSAOStrength); \
        fd.col.rgb = lerp(fd.col.rgb, _CustomSSAOColor.rgb, aoFactor * _CustomSSAOColor.a); \
    }
