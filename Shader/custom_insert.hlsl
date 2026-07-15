//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : Angle-based SSAO (UE4 style)
// ref: https://takumifukasawa.hatenablog.com/entry/unity-ssao-custom-post-process
// ref: https://github.com/takumifukasawa/UnitySSAOBuiltinPipeline (SSAOAngleBased.shader)
//
// アルゴリズム概要:
//   1. フラグメントのビュー空間位置を中心点とする
//   2. 6方向 × 対称2点 (A/B) = 12回の深度サンプリングを1セットとする
//   3. 各サンプル点のビュー空間位置を深度から復元
//   4. 「中心→サンプル点」の方向と「中心→カメラ」の方向の内積 (角度) を平均して遮蔽度とする
//      (遮蔽物が手前にあるほどサンプル方向がカメラ側へ傾き、内積が大きくなる)
//
// 平滑化 (ポストプロセスのブラーパスの代替):
//   - Quality: サンプリングパターンを 60°/K ずつ回転させながらK回実行して平均。
//     角度方向の隙間が埋まりバンディングが消える (インライン回転スーパーサンプリング)
//   - Dither: Interleaved Gradient Noise でピクセルごとにパターンを回転。
//     ノイズが高周波かつ均一に分散するため、目の空間積分で滑らかに見える
//----------------------------------------------------------------------------------------------------------------------

// サンプリングパターン: 参照実装 (SSAOAngleBased.cs) と同様に
// 回転角は 0..2π を6分割した各区間内、距離は 0.1..1.0 を6分割した各区間内から
// 選んだ値を定数として焼き込み (元実装はCPU側で乱数生成して配列で渡している)
static const float LIL_SHADOWEX_SSAO_ROTATIONS[6] = {0.401, 1.532, 2.401, 3.665, 4.510, 5.788};
static const float LIL_SHADOWEX_SSAO_DISTANCES[6] = {0.187, 0.331, 0.399, 0.542, 0.712, 0.874};

// ビュー空間位置が投影されるピクセルの深度 (linear eye depth) と投影先UVを取得する。
// 画面外・カメラ背後・深度未書き込み (far plane) の場合は false を返す。
bool lilShadowExSampleEyeDepthUV(float3 positionVS, out float eyeDepth, out float2 uv)
{
    eyeDepth = 0.0;
    uv = 0.0;

    float4 positionCS = mul(LIL_MATRIX_P, float4(positionVS, 1.0));
    if (positionCS.w < 0.0001) return false;

    // lilToonの提供する変換関数を利用して、プラットフォームごとの差異やレンダーテクスチャ反転を解決する
    float4 positionSS = lilTransformCStoSS(positionCS);
    uv = positionSS.xy / positionSS.w;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return false;

    float2 texel = uv * LIL_SCREENPARAMS.xy;
    float rawDepth = LIL_GET_DEPTH_TEX_CS(texel).r;
    #if defined(UNITY_REVERSED_Z)
        if (rawDepth <= 0.0) return false; // 深度未書き込み
    #else
        if (rawDepth >= 1.0) return false;
    #endif

    // ミラー (oblique projection) も考慮した linear eye depth 変換
    eyeDepth = LIL_TO_LINEARDEPTH(rawDepth, texel);
    return true;
}

// 従来シグネチャ (SSAO用)。UV不要な呼び出し側はこちらを使う。
bool lilShadowExSampleEyeDepth(float3 positionVS, out float eyeDepth)
{
    float2 uvUnused;
    return lilShadowExSampleEyeDepthUV(positionVS, eyeDepth, uvUnused);
}

// offsetVS のピクセルを通る視線レイ上で、深度 eyeDepth にある点を復元する
float3 lilShadowExReconstructVS(float3 offsetVS, float eyeDepth)
{
    return offsetVS * (eyeDepth / max(-offsetVS.z, 0.0001));
}

// Interleaved Gradient Noise (ディザ回転用)
// ピクセル間で位相が規則的に分散するため、sinハッシュよりノイズが均一で滑らかに見える
// ref: Jimenez 2014, "Next Generation Post Processing in Call of Duty: Advanced Warfare"
float lilShadowExIGN(float2 positionCS)
{
    return frac(52.9829189 * frac(dot(positionCS, float2(0.06711056, 0.00583715))));
}

// 1サンプル点分の遮蔽寄与を計算する (寄与なしで0)
float lilShadowExSampleOcclusion(float3 offsetPos, float3 centerVS, float centerEyeDepth, float3 surfaceToCameraDir, float minDist)
{
    float eyeDepth;
    if (!lilShadowExSampleEyeDepth(offsetPos, eyeDepth)) return 0.0;

    // ほぼ同一深度 (同一平面) のサンプルはAOに寄与させない
    if (abs(centerEyeDepth - eyeDepth) < _CustomSSAOBias) return 0.0;

    float3 samplePos = lilShadowExReconstructVS(offsetPos, eyeDepth);
    float dist = distance(samplePos, centerVS);
    if (dist < minDist || dist > _CustomSSAOMaxDistance) return 0.0;

    // 中心→サンプル点の方向がカメラ方向へ傾くほど遮蔽されていると判定
    float d = dot((samplePos - centerVS) / dist, surfaceToCameraDir);
    // 遠いサンプルほど寄与を減衰させてソフトな見た目にする
    float falloff = 1.0 - saturate(dist / _CustomSSAOMaxDistance);
    return max(0.0, d) * falloff;
}

//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : 追加ノーマルマップ (1枚のRGBAに2枚分をパック)
//
//   RG = 1枚目の接線空間法線XY, BA = 2枚目の接線空間法線XY。Zはシェーダーで復元する。
//   1サンプルで2枚分の法線を扱えるためテクスチャ枚数を削減できる。
//
//   ※ パックテクスチャは Unity の「Normal map」ではなく「Default (sRGBオフ/Linear)」で
//      インポートすること。Normal map インポートは DXT5nm の AG スウィズルを行うため、
//      RG/BA パッキングが壊れる。
//----------------------------------------------------------------------------------------------------------------------

// 0..1 の2ch から接線空間法線を復元する。strength で XY を増幅する (UDN的に Z が潰れる)。
float3 lilShadowExDecodeNormalCh(float2 ch, float strength)
{
    float2 xy = (ch * 2.0 - 1.0) * strength;
    float z = sqrt(saturate(1.0 - dot(xy, xy)));
    return float3(xy, z);
}

// UDNブレンド: ベース法線 (lilToonの1st等) の Z を保ちつつ、ディテール法線の XY を積む。
// 加算のみで正規化1回と軽量。強い凹凸でも破綻しにくい。
float3 lilShadowExBlendNormalUDN(float3 baseN, float3 detailN)
{
    return normalize(float3(baseN.xy + detailN.xy, baseN.z));
}

//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : 深度輪郭リムライト用ヘルパー
//
//   現在ピクセルのサーフェス深度と、上下左右 widthPixels ピクセル先のシーン深度を比較し、
//   近傍が奥にある (= シルエット境界) ほど大きい輪郭係数 (0..1) を返す。
//   _CameraDepthTexture を再利用するため追加サンプラーは消費しない。
//----------------------------------------------------------------------------------------------------------------------
float lilShadowExDepthContour(float2 pixelCoord, float centerEyeDepth, float widthPixels, float threshold)
{
    float edge = 0.0;
    float2 offs[4] = { float2(widthPixels, 0.0), float2(-widthPixels, 0.0), float2(0.0, widthPixels), float2(0.0, -widthPixels) };
    [unroll]
    for (uint i = 0; i < 4; i++)
    {
        float2 texel = pixelCoord + offs[i];
        float rawDepth = LIL_GET_DEPTH_TEX_CS(texel).r;
        float neighborDepth = LIL_TO_LINEARDEPTH(rawDepth, texel);
        // 近傍が奥 = シルエット境界。差が threshold を超えた分を滑らかに係数化。
        edge = max(edge, smoothstep(threshold, threshold * 2.0, neighborDepth - centerEyeDepth));
    }
    return edge;
}

//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : コンタクトシャドウ (Screen Space Shadows)
// ref: https://panoskarabelas.com/posts/screen_space_shadows/
//
//   フラグメントのビュー空間位置からライト方向へレイマーチし、各ステップで
//   レイ位置を投影した先のシーン深度と比較。レイがシーン表面より奥
//   (bias < delta < thickness) に潜ったら遮蔽 = 影と判定する。
//   シャドウマップでは拾えない近距離の接地影・パーツ間の細かい影を補完する。
//   _CameraDepthTexture を再利用するため追加サンプラーは消費しない。
//
//   スクリーンスペースの制約: 画面内に映っている遮蔽物しか影を落とせないため、
//   レイ長は短め (数cm〜) の近距離コンタクト影用途に留めるのが前提。
//
//   境界のぼかし (_CustomContactShadowBlur):
//   アバターシェーダーは単一パス完結でポストブラーが使えないため、レイマーチ内で
//   解析的にソフト化する (追加サンプルコスト無し)。
//     1. 遮蔽窓の平滑化 : bias..thickness の binary 判定を smoothstep 化し、
//        レイの潜り込みが浅い (= 影の境界付近の) ピクセルほど薄い影にする
//     2. ペナンブラ近似 : 遮蔽物が接地点から遠いほど影を弱める (実影の半影と同傾向)
//   さらに IGN ディザがソフト値と混ざることでステップ起因のバンディングも均される。
//----------------------------------------------------------------------------------------------------------------------
float lilShadowExContactShadow(float3 positionWS, float4 positionCS, float3 L)
{
    // 平行投影では視線レイによる深度比較が成り立たないためスキップ
    if (!lilIsPerspective()) return 0.0;

    float3 centerVS = mul(LIL_MATRIX_V, float4(positionWS, 1.0)).xyz;
    float3 lightDirVS = normalize(mul((float3x3)LIL_MATRIX_V, L));

    uint steps = (uint)clamp(_CustomContactShadowQuality + 0.5, 1.0, 4.0) * 8u; // 8..32
    float stepLen = _CustomContactShadowLength / (float)steps;

    // レイ開始位置を IGN でディザしてステップ間のバンディングをノイズ化 (任意)
    float jitter = _CustomContactShadowDither > 0.5 ? lilShadowExIGN(positionCS.xy) : 0.5;
    float3 rayPos = centerVS + lightDirVS * (stepLen * jitter);

    // 遮蔽窓 (bias..thickness) の両端のソフト幅。Blur=0 で従来の binary 判定と一致する。
    float blur = saturate(_CustomContactShadowBlur);
    float riseEnd = _CustomContactShadowBias + max(_CustomContactShadowThickness * blur, 1e-4);
    float fallStart = _CustomContactShadowThickness * (1.0 - blur);

    float occlusion = 0.0;

    for (uint i = 0; i < steps; i++)
    {
        rayPos += lightDirVS * stepLen;

        float sceneDepth;
        float2 uv;
        if (!lilShadowExSampleEyeDepthUV(rayPos, sceneDepth, uv)) continue;

        // レイがシーン表面より奥に潜っている量 (eye depth 差)
        float delta = (-rayPos.z) - sceneDepth;

        // 1. 遮蔽窓の平滑化: 潜り込みが浅いほど薄く、thickness を超えるほど薄く
        float soft = smoothstep(_CustomContactShadowBias, riseEnd, delta)
                   * (1.0 - smoothstep(fallStart, _CustomContactShadowThickness, delta));

        // 2. ペナンブラ近似: 遮蔽物までの距離 (レイ進行度) に応じて影を弱める
        float rayT = (float)(i + 1) / (float)steps;
        float distFade = 1.0 - rayT * blur;

        // スクリーン端フェード: 画面外の遮蔽情報が無いことによる影の切れ目を目立たなくする
        float2 edge = 1.0 - abs(uv * 2.0 - 1.0);
        float edgeFade = smoothstep(0.0, 0.2, min(edge.x, edge.y));

        occlusion = max(occlusion, soft * distFade * edgeFade);
        if (occlusion >= 0.999) break; // これ以上濃くならないため打ち切り
    }
    return occlusion;
}

//----------------------------------------------------------------------------------------------------------------------
// ShadowEx : 共有FXマスク & 追加スペキュラ用ヘルパー
//
//   複数の質感FXが1枚のRGBAマスクを共有し、各FXが _CustomXxxMaskChannel で
//   使用チャンネル(0=R/1=G/2=B/3=A)を選ぶ。テクスチャ枚数とサンプル数を削減する。
//----------------------------------------------------------------------------------------------------------------------

// RGBA から1チャンネルを選択して返す (0=R/1=G/2=B/3=A)
float lilShadowExSelectCh(float4 packed, float channel)
{
    if (channel < 0.5) return packed.r;
    if (channel < 1.5) return packed.g;
    if (channel < 2.5) return packed.b;
    return packed.a;
}

// スタイライズド Blinn-Phong スペキュラ量 (0..) を返す。
//   smoothness(0..1) をハイライトの鋭さ (指数) に変換し、N・L>0 の受光面のみに出す。
float lilShadowExSpecular(float3 N, float3 V, float3 L, float ndl, float smoothness)
{
    float3 halfDir = normalize(L + V);
    float nh = saturate(dot(N, halfDir));
    float specPow = exp2(saturate(smoothness) * 10.0 + 1.0); // 2..2048
    return pow(nh, specPow) * saturate(ndl);
}

// アングルベースAO本体。遮蔽度 (0..1) を返す。
float lilShadowExCalcSSAO(float3 positionWS, float4 positionCS)
{
    // 平行投影では視線レイによる復元が成り立たないためスキップ
    if (!lilIsPerspective()) return 0.0;

    float3 centerVS = mul(LIL_MATRIX_V, float4(positionWS, 1.0)).xyz;
    float centerEyeDepth = -centerVS.z;
    float3 surfaceToCameraDir = -normalize(centerVS);

    // ピクセルごとにサンプリングパターンを回転してバンディングをノイズ化 (任意)
    float ditherRad = _CustomSSAODither > 0.5 ? lilShadowExIGN(positionCS.xy) * (2.0 * LIL_PI) : 0.0;

    float minDist = max(_CustomSSAOMinDistance, 0.0001);
    uint iterations = (uint)clamp(_CustomSSAOQuality + 0.5, 1.0, 4.0);
    // パターンは約60°周期なので、反復ごとに 60°/K ずつ回転させて角度の隙間を埋める
    float iterationStep = (LIL_PI / 3.0) / (float)iterations;

    float occludedAcc = 0.0;

    for (uint k = 0; k < iterations; k++)
    {
        float baseRad = ditherRad + iterationStep * (float)k;

        for (uint i = 0; i < 6; i++)
        {
            float rad = LIL_SHADOWEX_SSAO_ROTATIONS[i] + baseRad;
            // 反復ごとに距離の割り当てもローテーションし、半径方向の隙間も埋める
            float offsetLen = LIL_SHADOWEX_SSAO_DISTANCES[(i + k) % 6] * _CustomSSAOSampleLength;
            float2 dir;
            sincos(rad, dir.y, dir.x);

            // ビュー空間XY平面上の対称2点
            float3 offsetA = float3(dir * offsetLen, 0.0);
            occludedAcc += lilShadowExSampleOcclusion(centerVS + offsetA, centerVS, centerEyeDepth, surfaceToCameraDir, minDist);
            occludedAcc += lilShadowExSampleOcclusion(centerVS - offsetA, centerVS, centerEyeDepth, surfaceToCameraDir, minDist);
        }
    }

    // (6方向 × 対称2点 × 反復数) の平均を返す
    return occludedAcc / (12.0 * (float)iterations);
}
