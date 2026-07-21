# ShadowEx パラメーター使用ガイド (Japanese)

ShadowEx は、`lilToon` シェーダーを拡張し、スクリーンスペースの影（SSAO / コンタクトシャドウ）、マルチレイヤーMatCap、追加ノーマルマップ、スタイライズドスペキュラ等の高品質な表現機能を追加するカスタムシェーダー拡張です。

---

## 目次

1. [Shared FX Mask (共有FXマスク)](#1-shared-fx-mask-共有fxマスク)
2. [SSAO (Screen Space Ambient Occlusion)](#2-ssao-screen-space-ambient-occlusion)
3. [Contact Shadow (コンタクトシャドウ / 接地影)](#3-contact-shadow-コンタクトシャドウ--接地影)
4. [Extra Normals (拡張ノーマルマップ)](#4-extra-normals-拡張ノーマルマップ)
5. [Rim Light 2nd (第2リムライト)](#5-rim-light-2nd-第2リムライト)
6. [Rim Shade (リムシェード)](#6-rim-shade-リムシェード)
7. [Additional Specular (追加スペキュラ)](#7-additional-specular-追加スペキュラ)
8. [MatCap Layers (MatCap レイヤー 1〜3)](#8-matcap-layers-matcap-レイヤー-13)
9. [FX Mask Packer (テクスチャパックツール)](#9-fx-mask-packer-テクスチャパックツール)

---

## 1. Shared FX Mask (共有FXマスク)

複数の質感エフェクト（リムシェード / MatCapレイヤー / コンタクトシャドウ / 追加スペキュラ / 拡張ノーマル）で共有する **RGBA 2枚分（計8チャンネル）** のマスクテクスチャシステムです。

* **プロパティ**:
  * `_CustomFXMask` : **FX Mask 1 (RGBA)**
  * `_CustomFXMask2` : **FX Mask 2 (RGBA)**
* **特徴**:
  * 各エフェクトの `Mask Channel` で `Mask1 R/G/B/A` 〜 `Mask2 R/G/B/A` の計8チャンネルから適用領域を選択します。
  * 実際に有効化されたエフェクトが参照するマスクのみサンプリングされるため、エフェクト数が増えてもテクスチャサンプル負荷は最小限（最大ピクセル当たり2サンプル）に抑えられます。
  * **推奨設定**: インポート設定で `sRGB (Color Texture)` を **オフ (Linear)** に設定してください。

---

## 2. SSAO (Screen Space Ambient Occlusion)

カメラの深度バッファ（`_CameraDepthTexture`）を利用し、オブジェクト間の凹みや接触部分にリアルタイムな環境光遮蔽（暗がり）を落とします。

> ⚠️ **動作条件**: VRChat等の環境で `_CameraDepthTexture` が有効な場合（影付きDirectional Lightが存在するワールドなど）にのみ描画されます。
ワールドに関わらず描画するためには、unitypackageインポート後、Assets/dennokoworks/ShadowEx/ にある LightForDepth をアバター内に配置してください。

* **主なパラメーター**:
  * **SSAOを有効化 (`_CustomSSAOEnabled`)**: SSAO機能のON/OFF。
  * **遮蔽色 (`_CustomSSAOColor`)**: 落ちる影の色。デフォルトは黒。
  * **強度 (`_CustomSSAOStrength`)** [範囲: 0 - 8]: AOの暗さの強度。
  * **ぼかし強度 (Power) (`_CustomSSAOPower`)** [範囲: 0.1 - 4]: コントラストとグラデーションの滑らかさ。1未満でソフトな陰影になります。
  * **サンプル長 (m) (`_CustomSSAOSampleLength`)** [範囲: 0.001 - 1]: 遮蔽を判定する探索半径（メートル単位）。
  * **最小距離 (m) / 最大距離 (m) (`_CustomSSAOMinDistance`, `_CustomSSAOMaxDistance`)**: 遮蔽判定を行う距離の範囲。
  * **深度バイアス (m) (`_CustomSSAOBias`)**: 自己遮蔽（チラつき）を防ぐための深度オフセット。
  * **フェード距離 (m) (`_CustomSSAOFadeDistance`)**: カメラからの距離に応じてAOを徐々にフェードアウトさせる距離。
  * **ぼかし (`_CustomSSAOBlur`)**: ノイズやバンディングを打ち消す軽量ぼかし。
  * **品質 (`_CustomSSAOQuality`)** [1 - 4]: 12サンプルのサンプリング品質倍率。
  * **ディザリング (IGN) (`_CustomSSAODither`)**: 画面空間インターリーブド・グラディエント・ノイズによるジッター処理の有無。

---

## 3. Contact Shadow (コンタクトシャドウ / 接地影)

深度バッファをライト方向へレイマーチングし、足元や着衣の隙間などにくっきりとした近距離の接地影を生成します。

> ⚠️ **動作条件**: SSAOと同様に影付きDirectional Lightのあるワールドでのみ動作します。

* **主なパラメーター**:
  * **コンタクトシャドウを有効化 (`_CustomContactShadowEnabled`)**: ON/OFF。
  * **影色 (乗算) (`_CustomContactShadowColor`)**: 接地影の色。メインカラーに乗算されます。
  * **レイ長 (m) (`_CustomContactShadowLength`)** [範囲: 0.005 - 0.5]: 影を検索するレイの長さ（数cm〜数十cmの短距離向け）。
  * **厚み (m) (`_CustomContactShadowThickness`)**: 遮蔽物とみなすレイの厚み。
  * **深度バイアス (m) (`_CustomContactShadowBias`)**: チラつき防止用オフセット。
  * **ぼかし (`_CustomContactShadowBlur`)**: 影の境界の柔らかさ。
  * **ぼかし強度 (`_CustomContactShadowBlurStrength`)**: 距離や深度に応じたソフトシャドウの強調倍率。
  * **品質 (`_CustomContactShadowQuality`)** [1 - 4]: 8ステップのレイマーチ精度。
  * **マスクチャンネル (`_CustomContactShadowMaskChannel`)**: 影の描画範囲を限定する Shared FX Mask のチャンネル。

---

## 4. Extra Normals (拡張ノーマルマップ)

lilToon 本体のノーマルマップとは別に、**独立した2枚のノーマルマップ（1st / 2nd）** を重ね合わせてブレンドします。

* **主なパラメーター**:
  * **拡張ノーマルを有効化 (`_CustomExtraNormalEnabled`)**: ON/OFF。
  * **ノーマルマップ 1st / 2nd (`_CustomExtraNormal1stTex`, `_CustomExtraNormal2ndTex`)**: 法線マップテクスチャ（インポート設定は `Normal map` に指定）。
  * **強度 (`_CustomExtraNormalStrengthA`, `_CustomExtraNormalStrengthB`)** [範囲: 0 - 10]: 法線の凹凸強度。
  * **タイリング (`_CustomExtraNormal1stScale`, `_CustomExtraNormal2ndScale`)**: それぞれ独立して設定できる UV スケール（Tiling）。
  * **マスクチャンネル (`_CustomExtraNormal1stMaskChannel`, `_CustomExtraNormal2ndMaskChannel`)**: 強度を制御する Shared FX Mask チャンネル。

---

## 5. Rim Light 2nd (第2リムライト)

エミッション段階で合成される追加のリムライトです。従来のフレネル方式に加え、シルエット輪郭を検出する **深度コンターモード** を搭載しています。

* **モード選択 (`_CustomRim2ndMode`)**:
  * **0: フレネル (Fresnel)**: 視線角度と法線の角度から輪郭光を算出（追加サンプルなし）。
  * **1: 深度コンター (Depth Contour)**: 深度バッファの差分から輪郭線を検出し、均一幅の輪郭リムを生成。
* **共通パラメーター**:
  * **リム色 (HDR) (`_CustomRim2ndColor`)**: リムライトの発光色。
  * **メインカラー強度 (`_CustomRim2ndMainStrength`)**: メインテクスチャの色をリムに乗せる割合。
  * **ライティング有効 (`_CustomRim2ndEnableLighting`)**: 0で一定発光、1でワールドのライト色・明るさに追従。
  * **シャドウマスク (`_CustomRim2ndShadowMask`)**: 影になっている領域でリムライトを衰退させる強度。
* **フレネルモード専用**:
  * **シャープさ (Power) (`_CustomRim2ndPower`)** / **境界 (`_CustomRim2ndBorder`)** / **ぼかし (`_CustomRim2ndBlur`)**
* **深度コンターモード専用**:
  * **幅 (px) (`_CustomRim2ndDepthWidth`)**: 輪郭線の幅（ピクセル）。
  * **しきい値 (m) (`_CustomRim2ndDepthThreshold`)**: エッジ検出の深度差しきい値。

---

## 6. Rim Shade (リムシェード)

視線と法線の角度から、輪郭部分を乗算色で暗く落とすシンプルなリムシェードです。

* **特徴**:
  * VRでの両目差を抑制するため頭部方向を考慮した計算を行っています。
  * Lite シェーダーでも動作し、ベースパスのエミッション直前に適用されるためエミッションが暗くなりません。
* **主なパラメーター**:
  * **シェード色 (乗算) (`_CustomRimShadeColor`)**: 暗く落とす乗算カラー。
  * **境界 (`_CustomRimShadeBorder`)**: 影が入り始める位置。
  * **ぼかし (`_CustomRimShadeBlur`)**: 境界のぼかし具合。
  * **フレネル強度 (`_CustomRimShadeFresnelPower`)**: 減衰のシャープさ。
  * **マスクチャンネル (`_CustomRimShadeMaskChannel`)**: 適用範囲を制御する Shared FX Mask チャンネル。

---

## 7. Additional Specular (追加スペキュラ)

lilToon のリアルタイプ反射と同等の物理ベース（GGX NDF + Smith-GGX 可視性関数 + Schlick Fresnel）スペキュラーハイライトを追加します。

* **主なパラメーター**:
  * **スペキュラ色 (HDR) (`_CustomSpecColor`)**: ハイライトの色。
  * **メインカラー強度 (`_CustomSpecMainStrength`)**: メインテクスチャの色（albedo）をスペキュラー色に乗せる割合。リムライトと同等の色決定方式です。
  * **スムーズネス (`_CustomSpecSmoothness`)**: 0で広範囲のボケた光、1で鋭い絞られたハイライト。
  * **金属度 (`_CustomSpecMetallic`)**: 金属度。1に近づくほど Albedo 色の反射率が反映されます。
  * **反射率 (F0) (`_CustomSpecReflectance`)**: 非金属時のフレネル基本反射率 (F0)。標準は 0.04。
  * **ノーマル強度 (`_CustomSpecNormalStrength`)**: ノーマルマップがスペキュラー計算に与える影響度。
  * **GSAA強度 (`_CustomSpecGSAAStrength`)**: 幾何学的スペキュラーアンチエイリアシングの適用強度。
  * **強度 (`_CustomSpecStrength`)**: ハイライトの明るさ倍率。
  * **合成モード (`_CustomSpecBlendMode`)**:
    * `通常 (Normal)` / `加算 (Add)` / `スクリーン (Screen)` / `乗算 (Multiply)`
  * **ライティング有効 (`_CustomSpecEnableLighting`)**: 1でメインライト色に追従。
  * **複数ライトからの光沢を有効化 (`_CustomSpecApplyMultiLight`)**: ON(1)の時、Point Light や Spot Light などのリアルタイム追加ライトからもそれぞれの光方向に合わせた物理スペキュラーツヤを生成します。
  * **環境反射を有効化 (`_CustomSpecApplyReflection`)**: ON(1)の時、ワールドのリフレクションプローブ/環境キューブマップから環境光の映り込みをサンプルしスペキュラーに合成します。
  * **シャドウマスク (`_CustomSpecShadowMask`)**: 影部でのハイライトの減衰強度。
  * **マスクチャンネル (`_CustomSpecMaskChannel`)**: 適用領域を制限する Shared FX Mask チャンネルの選択。

---

## 8. MatCap Layers (MatCap レイヤー 1〜3)

lilToon 本体の MatCap (1st/2nd) に加え、**最大3レイヤー** の MatCap テクスチャを追加できます。

* **特徴**:
  * lilToon の MatCap UV（`fd.uvMat`）とサンプラーを共有し、非常に軽量です。
  * `加算` や `スクリーン` 用には黒背景の MatCap、`乗算` 用には白背景の MatCap を使用してください。
* **レイヤーごとのパラメーター**:
  * **有効化 / MatCap テクスチャ / カラー (HDR)**
  * **合成モード**: 通常 / 加算 / スクリーン / 乗算
  * **ライティング有効**: ライト色に追従するかどうか。
  * **シャドウマスク**: 影領域での MatCap の強度減衰。
  * **マスクチャンネル**: Shared FX Mask チャンネルの指定。

---

## 9. FX Mask Packer (テクスチャパックツール)

Unity エディタ上メニューの `Tools > dennokoworks > ShadowEx > FX Mask Packer` から利用できるツールです。

* **機能**:
  * 1〜4枚のグレースケール/カラー画像を、1枚の RGBA テクスチャ（PNG）へパッキング生成します。
  * 入力画像の解像度が異なる場合は、自動的に最大解像度にバイリニア拡大されて合成されます。
  * **合成先 (Base Texture)** を指定すると、既存のパックテクスチャの下地を維持したまま特定チャンネルのみ差し替えることが可能です。
