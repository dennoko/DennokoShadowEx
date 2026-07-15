//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : Angle-based SSAO (UE4 style) for lilToon 2.x
// ref: https://takumifukasawa.hatenablog.com/entry/unity-ssao-custom-post-process
// ref: https://github.com/takumifukasawa/UnitySSAOBuiltinPipeline (SSAOAngleBased.shader)
//
// AO計算の本体は custom_insert.hlsl の lilShadowExCalcSSAO() に実装。
// _CameraDepthTexture が有効な場合のみ動作する(VRChatではシャドウ付き
// Directional Light が存在するワールドで有効になる)。無効時は素通し。
//
// コンタクトシャドウ (Screen Space Shadows) も同様に深度テクスチャを再利用する。
// ref: https://panoskarabelas.com/posts/screen_space_shadows/
//
// MatCap追加レイヤー (最大3) : fd.uvMat + 共有サンプラーで追加のMatCapを合成する。
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
    float  _CustomSSAOQuality; \
    float  _CustomExtraNormalEnabled; \
    float  _CustomExtraNormalStrengthA; \
    float  _CustomExtraNormalStrengthB; \
    float4 _CustomExtraNormal1stScale; \
    float4 _CustomExtraNormal2ndScale; \
    float  _CustomExtraNormal1stMaskChannel; \
    float  _CustomExtraNormal2ndMaskChannel; \
    float4 _CustomRim2ndColor; \
    float  _CustomRim2ndEnabled; \
    float  _CustomRim2ndMode; \
    float  _CustomRim2ndPower; \
    float  _CustomRim2ndBorder; \
    float  _CustomRim2ndBlur; \
    float  _CustomRim2ndEnableLighting; \
    float  _CustomRim2ndShadowMask; \
    float  _CustomRim2ndDepthWidth; \
    float  _CustomRim2ndDepthThreshold; \
    float4 _CustomSpecColor; \
    float  _CustomSpecEnabled; \
    float  _CustomSpecSmoothness; \
    float  _CustomSpecStrength; \
    float  _CustomSpecBlendMode; \
    float  _CustomSpecEnableLighting; \
    float  _CustomSpecShadowMask; \
    float  _CustomSpecMaskChannel; \
    float4 _CustomContactShadowColor; \
    float  _CustomContactShadowEnabled; \
    float  _CustomContactShadowLength; \
    float  _CustomContactShadowThickness; \
    float  _CustomContactShadowBias; \
    float  _CustomContactShadowBlur; \
    float  _CustomContactShadowBlurStrength; \
    float  _CustomContactShadowQuality; \
    float  _CustomContactShadowDither; \
    float  _CustomContactShadowMaskChannel; \
    float4 _CustomRimShadeColor; \
    float  _CustomRimShadeEnabled; \
    float  _CustomRimShadeBorder; \
    float  _CustomRimShadeBlur; \
    float  _CustomRimShadeFresnelPower; \
    float  _CustomRimShadeMaskChannel; \
    float4 _CustomMatCapLayer1Color; \
    float  _CustomMatCapLayer1Enabled; \
    float  _CustomMatCapLayer1BlendMode; \
    float  _CustomMatCapLayer1EnableLighting; \
    float  _CustomMatCapLayer1ShadowMask; \
    float  _CustomMatCapLayer1MaskChannel; \
    float4 _CustomMatCapLayer2Color; \
    float  _CustomMatCapLayer2Enabled; \
    float  _CustomMatCapLayer2BlendMode; \
    float  _CustomMatCapLayer2EnableLighting; \
    float  _CustomMatCapLayer2ShadowMask; \
    float  _CustomMatCapLayer2MaskChannel; \
    float4 _CustomMatCapLayer3Color; \
    float  _CustomMatCapLayer3Enabled; \
    float  _CustomMatCapLayer3BlendMode; \
    float  _CustomMatCapLayer3EnableLighting; \
    float  _CustomMatCapLayer3ShadowMask; \
    float  _CustomMatCapLayer3MaskChannel;

// Custom textures
// (_CameraDepthTexture は lilToon 側 (lil_common_input.hlsl) で宣言済みのため追加宣言しない)
// 追加ノーマルマップ 2枚 (_CustomExtraNormal1stTex / _CustomExtraNormal2ndTex) は
// 専用サンプラーを宣言せず lilToon 共有サンプラー (sampler_linear_repeat) を再利用する。
// DX11 のサンプラースロット(16)を消費しないため。
// _CustomFXMask / _CustomFXMask2 は複数の質感FXが共有するRGBAマスク2枚
// (計8チャンネル)。こちらも共有サンプラー (sampler_linear_repeat) を再利用する。
// サンプリングは BEFORE_EMISSION_1ST の先頭 (lilShadowExSampleFXMasks) で
// 実際に使われるマスクだけを各1回行い、static 変数へキャッシュして
// 全FX (リムシェード/MatCapレイヤー/コンタクトシャドウ/追加スペキュラ) が使い回す。
// MatCap追加レイヤーのテクスチャも同様に共有サンプラーを再利用する
// (テクスチャスロットは消費するがサンプラースロットは増えない)。
#define LIL_CUSTOM_TEXTURES \
    TEXTURE2D(_CustomExtraNormal1stTex); \
    TEXTURE2D(_CustomExtraNormal2ndTex); \
    TEXTURE2D(_CustomFXMask); \
    TEXTURE2D(_CustomFXMask2); \
    TEXTURE2D(_CustomMatCapLayer1Tex); \
    TEXTURE2D(_CustomMatCapLayer2Tex); \
    TEXTURE2D(_CustomMatCapLayer3Tex);

// Add vertex shader output
// SSAOの中心点計算にワールド座標を使うため強制的に v2f へ含める
#define LIL_V2F_FORCE_POSITION_WS

// 追加ノーマルマップ (個別の接線空間ノーマルマップ2枚) を lilToon のノーマル処理内で合成する。
// lilToon は BEFORE_NORMAL_2ND の直後に normalmap を fd.N へ変換 (world) し、
// fd.ln / fd.uvMat / fd.reflectionN / fd.matcapN 等の派生値もまとめて再計算する。
// そのため接線空間の normalmap にディテールを積むことで、追加処理を書かずとも
// ライティング・MatCap・リム・反射すべてに反映される。
// 1st/2nd はそれぞれ専用テクスチャを別UV(タイリング)でサンプルし、lilToon 本体と
// 同じ lilUnpackNormalScale (DXT5nm対応) + lilBlendNormal で合成する。
// 各法線の Strength には共有FXマスクの選択chを掛けて範囲制御する。マスクは
// このフックが BEFORE_EMISSION_1ST のキャッシュより前に展開されるため
// lilShadowExSampleNormalMasks() で必要な分だけ直接サンプルする (最大2サンプル)。
// ※ この注入は lilToon のノーマルマップ機能が有効なとき (LIL_FEATURE_NORMAL) に動作する。
#define BEFORE_NORMAL_2ND \
    if (_CustomExtraNormalEnabled > 0.5) \
    { \
        float exMask1, exMask2; \
        lilShadowExSampleNormalMasks(fd.uvMain, _CustomExtraNormal1stMaskChannel, _CustomExtraNormal2ndMaskChannel, exMask1, exMask2); \
        float4 exTex1 = LIL_SAMPLE_2D(_CustomExtraNormal1stTex, sampler_linear_repeat, fd.uv0 * _CustomExtraNormal1stScale.xy); \
        float4 exTex2 = LIL_SAMPLE_2D(_CustomExtraNormal2ndTex, sampler_linear_repeat, fd.uv0 * _CustomExtraNormal2ndScale.xy); \
        normalmap = lilBlendNormal(normalmap, lilUnpackNormalScale(exTex1, _CustomExtraNormalStrengthA * exMask1)); \
        normalmap = lilBlendNormal(normalmap, lilUnpackNormalScale(exTex2, _CustomExtraNormalStrengthB * exMask2)); \
    }

// リムライト2nd (フレネル型 / 深度輪郭型) を合成する。
// 注入点は BEFORE_BLEND_EMISSION。ここは full/lite 両パスに存在し、かつ
// #ifndef LIL_PASS_FORWARDADD の内側 (ベースパス限定) なので、リアルタイム追加
// ライトのパスで定数リムが二重加算されるのを防げる (lilToon本体もadd時は定数リムを無効化)。
//   Mode 0 = フレネル型 : abs(dot(N,V)) から輪郭を出す。追加サンプル無し。
//   Mode 1 = 深度輪郭型 : _CameraDepthTexture を再利用しシルエット境界を検出。
//            LIL_ENABLED_DEPTH_TEX が有効なワールドでのみ動作 (無効時は素通し)。
// _CustomRim2ndEnableLighting でライト色乗算 (0=定数色 / 1=ライト追従) を補間。
//
// 追加スペキュラ (質感系) も同じ注入点に相乗り。ベースパス限定なので追加ライトで
// 二重加算されず、full/lite 両パスに存在するため Lite でも効く。
//   スタイライズド Blinn-Phong を fd.N/fd.V/fd.L から算出し、共有FXマスク
//   (_CustomFXMask) の任意chで強度をマスク、lilBlendColor でブレンド。
//   マスク値は BEFORE_EMISSION_1ST (このフックより前に展開される) でキャッシュ
//   済みの lilShadowExFXMask を参照する (追加サンプル無し)。
#define BEFORE_BLEND_EMISSION \
    if (_CustomRim2ndEnabled > 0.5) \
    { \
        float rim2 = 0.0; \
        if (_CustomRim2ndMode < 0.5) \
        { \
            float nvabs = abs(dot(fd.N, fd.V)); \
            rim2 = pow(saturate(1.0 - nvabs), max(_CustomRim2ndPower, 0.01)); \
            rim2 = lilTooningScale(_AAStrength, rim2, _CustomRim2ndBorder, _CustomRim2ndBlur); \
        } \
        else if (LIL_ENABLED_DEPTH_TEX) \
        { \
            float centerEyeDepth = -mul(LIL_MATRIX_V, float4(fd.positionWS, 1.0)).z; \
            rim2 = lilShadowExDepthContour(fd.positionCS.xy, centerEyeDepth, _CustomRim2ndDepthWidth, _CustomRim2ndDepthThreshold); \
        } \
        rim2 = lerp(rim2, rim2 * fd.shadowmix, _CustomRim2ndShadowMask); \
        float3 rim2Col = lerp(_CustomRim2ndColor.rgb, _CustomRim2ndColor.rgb * fd.lightColor, _CustomRim2ndEnableLighting); \
        fd.col.rgb += rim2Col * (rim2 * _CustomRim2ndColor.a); \
    } \
    if (_CustomSpecEnabled > 0.5) \
    { \
        float spec = lilShadowExSpecular(fd.N, fd.V, fd.L, fd.ln, _CustomSpecSmoothness); \
        float specMask = lilShadowExSelectMaskCh(_CustomSpecMaskChannel); \
        float specAmt = spec * _CustomSpecStrength * specMask * _CustomSpecColor.a; \
        specAmt = lerp(specAmt, specAmt * fd.shadowmix, _CustomSpecShadowMask); \
        float3 specCol = lerp(_CustomSpecColor.rgb, _CustomSpecColor.rgb * fd.lightColor, _CustomSpecEnableLighting); \
        fd.col.rgb = lilBlendColor(fd.col.rgb, specCol, saturate(specAmt), (uint)_CustomSpecBlendMode); \
    }

// Inserting a process into pixel shader
// エミッション加算の直前に適用することで、発光部分を暗くせずにAOをかける。
// LIL_ENABLED_DEPTH_TEX: 深度テクスチャが無いワールドでは自動的に無効化される。
//
// コンタクトシャドウ (Screen Space Shadows) も同じ注入点に相乗り。
// ref: https://panoskarabelas.com/posts/screen_space_shadows/
// ここは full/lite 両パスに存在し #ifndef LIL_PASS_FORWARDADD の内側 (ベースパス限定)
// なので、追加ライトパスでの二重適用・コスト増が無い。
// 遮蔽係数で専用の影色を乗算合成し、fd.shadowmix にも書き込むことで
// 後段のリム2nd/追加スペキュラの Shadow Mask とも連動する。
//
// MatCap追加レイヤー (最大3) も同じ注入点の先頭に相乗り。
// lilToon の MatCap フック (BEFORE_MATCAP 等) は FORWARDADD でも走るため使わず、
// ベースパス限定のここへ注入する (カメラ依存のMatCapを追加ライトで二重加算しない)。
// UV は lilToon が計算済みの fd.uvMat を再利用 (追加計算ゼロ、liteでは頂点補間値)。
// SSAO/コンタクトシャドウより前に合成することで、レイヤーの上からも遮蔽が暗く乗る。
//
// 本ブロック先頭では共有FXマスク2枚 (_CustomFXMask / _CustomFXMask2) のうち
// 実際に使われる方だけを lilShadowExSampleFXMasks() で各1回サンプルしキャッシュする。
// 以降の全FX (後段の BEFORE_BLEND_EMISSION の追加スペキュラ含む) はこの値を
// 参照するため、有効なFXの数によらずマスクのサンプルは最大2回 (使用マスク数分)。
// 全FX無効時は条件が定数falseになりロック時にサンプルごとストリップされる。
//
// リムシェード (乗算リム陰) はマスクサンプルの直後 (合成の最初) に適用する。
// 計算は lilToon 本体の Rim Shade と同等 (fd.headV 基準の逆フレネル +
// lilTooningScale + 乗算合成)。本体の Rim Shade フックは lite パスに存在しない
// ため、ここで自前実装することで lite でも動作する。陰の上に MatCap レイヤーや
// リム2nd が乗る順序も lilToon 本体 (rimshade -> matcap -> rim) と揃う。
#define BEFORE_EMISSION_1ST \
    lilShadowExSampleFXMasks(fd.uvMain); \
    if (_CustomRimShadeEnabled > 0.5) \
    { \
        float rsNvabs = abs(dot(fd.N, fd.headV)); \
        float rsRim = pow(saturate(1.0 - rsNvabs), max(_CustomRimShadeFresnelPower, 0.01)); \
        rsRim = lilTooningScale(_AAStrength, rsRim, _CustomRimShadeBorder, _CustomRimShadeBlur); \
        float rsMask = lilShadowExSelectMaskCh(_CustomRimShadeMaskChannel); \
        rsRim = saturate(rsRim * _CustomRimShadeColor.a * rsMask); \
        fd.col.rgb = lerp(fd.col.rgb, fd.col.rgb * _CustomRimShadeColor.rgb, rsRim); \
    } \
    if (_CustomMatCapLayer1Enabled > 0.5) \
    { \
        float4 mc1 = LIL_SAMPLE_2D(_CustomMatCapLayer1Tex, sampler_linear_repeat, fd.uvMat) * _CustomMatCapLayer1Color; \
        fd.col.rgb = lilShadowExMatCapLayer(fd.col.rgb, mc1, fd.lightColor, fd.shadowmix, lilShadowExSelectMaskCh(_CustomMatCapLayer1MaskChannel), _CustomMatCapLayer1BlendMode, _CustomMatCapLayer1EnableLighting, _CustomMatCapLayer1ShadowMask); \
    } \
    if (_CustomMatCapLayer2Enabled > 0.5) \
    { \
        float4 mc2 = LIL_SAMPLE_2D(_CustomMatCapLayer2Tex, sampler_linear_repeat, fd.uvMat) * _CustomMatCapLayer2Color; \
        fd.col.rgb = lilShadowExMatCapLayer(fd.col.rgb, mc2, fd.lightColor, fd.shadowmix, lilShadowExSelectMaskCh(_CustomMatCapLayer2MaskChannel), _CustomMatCapLayer2BlendMode, _CustomMatCapLayer2EnableLighting, _CustomMatCapLayer2ShadowMask); \
    } \
    if (_CustomMatCapLayer3Enabled > 0.5) \
    { \
        float4 mc3 = LIL_SAMPLE_2D(_CustomMatCapLayer3Tex, sampler_linear_repeat, fd.uvMat) * _CustomMatCapLayer3Color; \
        fd.col.rgb = lilShadowExMatCapLayer(fd.col.rgb, mc3, fd.lightColor, fd.shadowmix, lilShadowExSelectMaskCh(_CustomMatCapLayer3MaskChannel), _CustomMatCapLayer3BlendMode, _CustomMatCapLayer3EnableLighting, _CustomMatCapLayer3ShadowMask); \
    } \
    if (_CustomSSAOEnabled > 0.5 && LIL_ENABLED_DEPTH_TEX) \
    { \
        float aoRate = lilShadowExCalcSSAO(fd.positionWS, fd.positionCS); \
        float aoFactor = saturate(pow(saturate(aoRate), _CustomSSAOPower) * _CustomSSAOStrength); \
        fd.col.rgb = lerp(fd.col.rgb, _CustomSSAOColor.rgb, aoFactor * _CustomSSAOColor.a); \
    } \
    if (_CustomContactShadowEnabled > 0.5 && LIL_ENABLED_DEPTH_TEX) \
    { \
        float csFactor = lilShadowExContactShadow(fd.positionWS, fd.positionCS, fd.L); \
        float csMask = lilShadowExSelectMaskCh(_CustomContactShadowMaskChannel); \
        csFactor = saturate(csFactor * _CustomContactShadowColor.a * csMask); \
        fd.col.rgb = lerp(fd.col.rgb, fd.col.rgb * _CustomContactShadowColor.rgb, csFactor); \
        fd.shadowmix *= 1.0 - csFactor; \
    }
