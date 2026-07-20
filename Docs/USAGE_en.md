# ShadowEx Parameter Usage Guide (English)

ShadowEx is a custom shader extension for `lilToon` that enhances rendering capabilities by adding screen-space shadows (SSAO / Contact Shadows), multi-layer MatCaps, extra normal maps, stylized specular, and more.

---

## Table of Contents

1. [Shared FX Mask](#1-shared-fx-mask)
2. [SSAO (Screen Space Ambient Occlusion)](#2-ssao-screen-space-ambient-occlusion)
3. [Contact Shadow](#3-contact-shadow)
4. [Extra Normals](#4-extra-normals)
5. [Rim Light 2nd](#5-rim-light-2nd)
6. [Rim Shade](#6-rim-shade)
7. [Additional Specular](#7-additional-specular)
8. [MatCap Layers (Layers 1-3)](#8-matcap-layers-layers-1-3)
9. [FX Mask Packer (Editor Tool)](#9-fx-mask-packer-editor-tool)

---

## 1. Shared FX Mask

A shared mask texture system using **2 RGBA textures (8 channels in total)** across multiple FX features (Rim Shade, MatCap Layers, Contact Shadow, Additional Specular, Extra Normals).

* **Properties**:
  * `_CustomFXMask` : **FX Mask 1 (RGBA)**
  * `_CustomFXMask2` : **FX Mask 2 (RGBA)**
* **Features**:
  * Select the mask channel to use per feature (`Mask1 R/G/B/A` to `Mask2 R/G/B/A`) using the `Mask Channel` popup.
  * Only masks referenced by active features are sampled, keeping texture sampling overhead minimal (maximum 2 texture samples per pixel regardless of active FX count).
  * **Recommended Import Setting**: Set `sRGB (Color Texture)` to **OFF (Linear)** in texture import settings.

---

## 2. SSAO (Screen Space Ambient Occlusion)

Calculates real-time ambient occlusion shadows in crevices and contact areas using the camera depth buffer (`_CameraDepthTexture`).

> ⚠️ **Requirement**: Functions in environments like VRChat where `_CameraDepthTexture` is enabled (e.g., worlds with shadowed Directional Light).
To ensure SSAO renders regardless of the world, place `LightForDepth` (found in `Assets/dennokoworks/ShadowEx/` after importing the unitypackage) under your avatar.

* **Key Parameters**:
  * **Enable SSAO (`_CustomSSAOEnabled`)**: Toggles SSAO ON/OFF.
  * **Occlusion Color (`_CustomSSAOColor`)**: Color of the ambient occlusion shadow (default: Black).
  * **Strength (`_CustomSSAOStrength`)** [Range: 0 - 8]: Intensity of occlusion.
  * **Power (`_CustomSSAOPower`)** [Range: 0.1 - 4]: Controls contrast and softness of gradation. Values below 1 soften the shadow gradient.
  * **Sample Length (m) (`_CustomSSAOSampleLength`)** [Range: 0.001 - 1]: Search radius for occlusion in meters.
  * **Min Distance (m) / Max Distance (m) (`_CustomSSAOMinDistance`, `_CustomSSAOMaxDistance`)**: Distance range for occlusion evaluation.
  * **Depth Bias (m) (`_CustomSSAOBias`)**: Depth offset to prevent self-occlusion artifacts/flickering.
  * **Fade Distance (m) (`_CustomSSAOFadeDistance`)**: Distance at which AO gradually fades out.
  * **Blur (`_CustomSSAOBlur`)**: Lightweight blur to smooth out noise and banding.
  * **Quality (`_CustomSSAOQuality`)** [1 - 4]: Sampling quality multiplier for 12-sample kernel.
  * **Dither (IGN) (`_CustomSSAODither`)**: Toggles Interleaved Gradient Noise dithering jitter.

---

## 3. Contact Shadow

Raymarches the camera depth buffer towards the main light direction to cast crisp, short-range contact shadows near feet, clothing folds, etc.

> ⚠️ **Requirement**: Requires a world with a shadowed Directional Light to function.

* **Key Parameters**:
  * **Enable Contact Shadow (`_CustomContactShadowEnabled`)**: Toggles Contact Shadows ON/OFF.
  * **Shadow Color (Multiply) (`_CustomContactShadowColor`)**: Multiplicative shadow color.
  * **Ray Length (m) (`_CustomContactShadowLength`)** [Range: 0.005 - 0.5]: Maximum raymarch length (intended for short distances of a few cm).
  * **Thickness (m) (`_CustomContactShadowThickness`)**: Ray thickness considered as an occluder.
  * **Depth Bias (m) (`_CustomContactShadowBias`)**: Offset to prevent self-shadowing artifacts.
  * **Blur (`_CustomContactShadowBlur`)**: Softness of shadow edges.
  * **Blur Strength (`_CustomContactShadowBlurStrength`)**: Scaling multiplier for distance/depth-based soft shadow softening.
  * **Quality (`_CustomContactShadowQuality`)** [1 - 4]: Step count quality multiplier (8 steps).
  * **Mask Channel (`_CustomContactShadowMaskChannel`)**: Selects the channel from Shared FX Mask to limit shadow placement.

---

## 4. Extra Normals

Blends **2 additional independent tangent-space normal maps (1st / 2nd)** on top of lilToon's main normal map.

* **Key Parameters**:
  * **Enable Extra Normals (`_CustomExtraNormalEnabled`)**: Toggles Extra Normals ON/OFF.
  * **Normal Map 1st / 2nd (`_CustomExtraNormal1stTex`, `_CustomExtraNormal2ndTex`)**: Normal map textures (set Texture Type to `Normal map`).
  * **Strength (`_CustomExtraNormalStrengthA`, `_CustomExtraNormalStrengthB`)** [Range: 0 - 10]: Normal bump intensity scale.
  * **Tiling (`_CustomExtraNormal1stScale`, `_CustomExtraNormal2ndScale`)**: Independent UV tiling scales for 1st and 2nd layers.
  * **Mask Channel (`_CustomExtraNormal1stMaskChannel`, `_CustomExtraNormal2ndMaskChannel`)**: Mask channel from Shared FX Mask to scale bump strength.

---

## 5. Rim Light 2nd

An additional rim light layer applied during the base pass emission stage. Features both traditional Fresnel mode and silhouette-detecting **Depth Contour mode**.

* **Mode Selection (`_CustomRim2ndMode`)**:
  * **0: Fresnel**: Standard view-angle rim highlight (zero extra samples).
  * **1: Depth Contour**: Detects silhouette edges from the depth buffer for uniform outline rim lighting.
* **Common Parameters**:
  * **Rim Color (HDR) (`_CustomRim2ndColor`)**: Emission color for the rim light.
  * **Main Color Strength (`_CustomRim2ndMainStrength`)**: Blend ratio of main texture color into rim light color.
  * **Enable Lighting (`_CustomRim2ndEnableLighting`)**: 0 = Constant color, 1 = Follows world light color & brightness.
  * **Shadow Mask (`_CustomRim2ndShadowMask`)**: Attenuates rim light intensity in shadowed regions.
* **Fresnel Mode Parameters**:
  * **Power (`_CustomRim2ndPower`)** / **Border (`_CustomRim2ndBorder`)** / **Blur (`_CustomRim2ndBlur`)**
* **Depth Contour Mode Parameters**:
  * **Width (px) (`_CustomRim2ndDepthWidth`)**: Line width of contour rim in pixels.
  * **Threshold (m) (`_CustomRim2ndDepthThreshold`)**: Depth difference threshold for silhouette detection.

---

## 6. Rim Shade

A simple multiplicative rim shade that darkens silhouette borders based on view and normal angles.

* **Features**:
  * Head-vector based calculation reduces VR eye parity differences.
  * Supported in Lite shaders. Applied prior to emission in the base pass so emission brightness is preserved.
* **Key Parameters**:
  * **Shade Color (Multiply) (`_CustomRimShadeColor`)**: Multiplicative darkening color.
  * **Border (`_CustomRimShadeBorder`)**: Start position of border shading.
  * **Blur (`_CustomRimShadeBlur`)**: Softness of the shade edge.
  * **Fresnel Power (`_CustomRimShadeFresnelPower`)**: Falloff power exponent.
  * **Mask Channel (`_CustomRimShadeMaskChannel`)**: Shared FX Mask channel for area control.

---

## 7. Additional Specular

Adds stylized Blinn-Phong highlights relative to the main light direction for hair, skin, or fabric shine.

* **Key Parameters**:
  * **Specular Color (HDR) (`_CustomSpecColor`)**: Highlight color.
  * **Smoothness (`_CustomSpecSmoothness`)**: Controls highlight sharpness (0 = wide soft glow, 1 = sharp highlight).
  * **Strength (`_CustomSpecStrength`)**: Specular intensity scale.
  * **Blend Mode (`_CustomSpecBlendMode`)**:
    * `Normal` / `Add` / `Screen` / `Multiply`
  * **Enable Lighting (`_CustomSpecEnableLighting`)**: Follows main light color when enabled.
  * **Shadow Mask (`_CustomSpecShadowMask`)**: Suppresses specular highlights in shadow areas.
  * **Mask Channel (`_CustomSpecMaskChannel`)**: Shared FX Mask channel for specular range.

---

## 8. MatCap Layers (Layers 1-3)

Applies **up to 3 additional MatCap layers** on top of lilToon's primary MatCaps (1st/2nd).

* **Features**:
  * Reuses lilToon MatCap UVs (`fd.uvMat`) and texture samplers for low performance overhead.
  * Use black-background MatCaps for `Add` / `Screen`, and white-background MatCaps for `Multiply`.
* **Per-Layer Parameters**:
  * **Enable / MatCap Texture / Color (HDR)**
  * **Blend Mode**: Normal / Add / Screen / Multiply
  * **Enable Lighting**: Toggles light color influence.
  * **Shadow Mask**: Attenuates MatCap brightness in shadow regions.
  * **Mask Channel**: Shared FX Mask channel selector.

---

## 9. FX Mask Packer (Editor Tool)

An editor tool accessible via Unity menu: `Tools > dennokoworks > ShadowEx > FX Mask Packer`.

* **Features**:
  * Packs 1 to 4 grayscale/color mask images into a single RGBA PNG texture.
  * Automatically rescales input images to the maximum resolution using bilinear filtering if dimensions differ.
  * **Base Texture (Optional)**: Specifies an existing packed texture to overwrite only selected channels while preserving unassigned channels.
