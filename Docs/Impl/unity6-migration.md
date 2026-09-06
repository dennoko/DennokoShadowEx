# ShadowEx — Unity 6 移行調査

- 調査日: 2026-09-06
- 現行: Unity 2022.3.22f1 / Built-in RP
- 目標: Unity 6 (6000.0 LTS) / **BiRP 維持**
- 共通調査: [`unity6-migration-overview.md`](unity6-migration-overview.md)

## 判定

⛔ **外部依存待ち** — 自コード側に Unity 6 非対応の API は **0 件**。
ただし **lilToon の型を直接参照**しているため、lilToon が Unity 6 に対応するまで
コンパイルすらできない。先行検証は不可能。

## 構成

| 項目 | 内容 |
|---|---|
| 規模 | C# 4 ファイル / 約 1,471 行 ＋ シェーダ 4 ファイル |
| asmdef | `dennokoworks.ShadowEx.Editor`（→ **`lilToon.Editor`**） |
| エントリ | `MenuItem("Assets/dennokoworks/ShadowEx/Convert material to custom shader")`<br>`MenuItem("Tools/dennokoworks/ShadowEx/FX Mask Packer")` |
| UI | lilToon インスペクタ拡張（IMGUI） |
| 外部依存 | **lilToon**（asmdef 直接参照） |

DennokoEx と同系統の lilToon 拡張ツール。DennokoEx より依存が少なく
（VRC Light Volumes / AudioLink / NDMF に非依存）、lilToon のみに依存する。

## 検出事項

### 1. lilToon への asmdef 直接参照（⛔ 最大の外部依存）

`Editor/ShadowEx.Editor.asmdef`

```json
"references": [ "lilToon.Editor" ]
```

- **lilToon のアセンブリを直接参照**しているため、lilToon が Unity 6 に対応するまで
  そもそもコンパイルできない。リフレクション回避の余地がない設計。
- lilToon が Unity 6 対応版を出せばそのまま動く見込みだが、
  lilToon はメジャーバージョン間でインスペクタ構造を変えることがあるため、
  **Unity 6 対応版 lilToon で API 面の再確認が必要**。

**対応**: lilToon の Unity 6 対応版リリースを待つ。事前作業は不可。

### 2. シェーダの lilToon 拡張フック（⛔ 外部依存）

`Shader/custom.hlsl`, `Shader/custom_insert.hlsl`

```
//   #include lil_pipeline_*.hlsl
//   #include lil_common.hlsl
//   #include lil_pass_*.hlsl      (this is where the BEFORE_* hook macros are expanded)
```

- lilToon のシェーダパイプラインに `BEFORE_*` フックマクロで割り込む設計。
- **lilToon 側のフックマクロ名や include 順序が変わると破綻する**。
- Unity 6 の BiRP 自体には破壊的変更がないため、
  **リスクは Unity 6 ではなく lilToon のバージョン差異に由来する**。

`Shader/custom_insert.hlsl` のコメント（`:59`）

```
//   Unity によって各 HLSLPROGRAM の「先頭」へ挿入される。一方 LIL_PASS_FORWARDADD は
```

Unity のシェーダインクルード挿入位置に関する前提が書かれている。
この挙動は Unity 6 でも変わらないが、**フェーズ 3 の検証時に実際のコンパイル結果で確認**する。

### 3. `ShadowExMaskPacker` と `TextureImporter`（✅ 影響なし）

`Editor/ShadowExMaskPacker.cs:229,251`

```csharp
var importer = string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path) as TextureImporter;
var importer = AssetImporter.GetAtPath(path) as TextureImporter;
```

- `AssetImporter.GetAtPath` / `TextureImporter` は Unity 6 で **変更なし**。
- Unity 6 で Obsolete になっている `TextureImporter.textureFormat` は **未使用**。
- `string.IsNullOrEmpty(path)` によるガードもあり、防御的に書かれている。
- **修正不要。**

### 4. `EditorGUIUtility.IconContent` / 組み込み GUIStyle（🔍 軽微な視覚検証）

`Editor/ShadowExInspector.cs:964`

```csharp
if(GUI.Button(position, EditorGUIUtility.IconContent("_Popup"), middleButton))
```

- `_Popup` は Unity の**組み込みアイコン名**。Unity 6 でも存在するが、
  Editor スキン刷新に伴い見た目が変わる可能性がある。
- 存在しない名前を指定しても例外にはならず、空のアイコンになるだけ。
- **対応**: Unity 6 上でボタンが正しく描画されるか目視確認する。

> 同じパターンが `DennokoEx/Editor/DennokoExInspector.cs:265` にもある。

### 5. バージョン定義（✅ 影響なし）

`Editor/ShadowExInspector.cs:913,970`

```csharp
#if UNITY_2019_3_OR_NEWER
```

- Unity 6 でも **true**。新しい側の分岐が採用され、旧分岐が死にコードになるだけ。
- **動作は変わらない。修正不要。**

> 移行の機会に、サポート下限が 2022.3 になるなら
> `#if UNITY_2019_3_OR_NEWER` の分岐自体を削除して簡素化してもよい（必須ではない）。

## 非該当の確認

| 確認項目 | 結果 |
|---|---|
| `Object.FindObjectsOfType` / `FindObjectOfType` | **なし** |
| UnityEditor 内部 API へのリフレクション | **なし** |
| `GraphicsFormat.DepthAuto` / `ShadowAuto` / `VideoAuto` | **なし** |
| UI Toolkit（`ExecuteDefaultAction` / `UxmlFactory` 等） | **なし**（IMGUI のみ） |
| IMGUI テーマ（共有 `EditorStyles` 書き換え） | **なし** |
| Compute シェーダ | **なし** |
| `CustomEditorForRenderPipelineAttribute`（Obsolete） | **なし** |
| `TextureImporter.textureFormat`（Obsolete） | **なし** |
| `Lightmapping` / 物理 API | **なし** |
| VRChat SDK / NDMF 直接参照 | **なし**（lilToon のみ） |

## 移行手順

### フェーズ 1（Unity 2022.3.22f1 のまま実施可）

- [ ] 検証用の基準データを作る: 代表的なマテリアルで FX Mask Packer を実行し、
      2022.3 で出力したマスクテクスチャを保存しておく（Unity 6 での比較に使う）
- [ ] （任意）`#if UNITY_2019_3_OR_NEWER` の旧分岐を削除して簡素化

### フェーズ 3（外部依存の Unity 6 対応後）

1. **lilToon** の Unity 6 対応版を導入（これが無いとコンパイル不可）
2. VRChat SDK（Unity 6 対応済み）を導入（アップロード検証用）
3. 下記チェックリストで動作確認

> **先行検証は不可能**。asmdef が `lilToon.Editor` を直接参照しているため、
> lilToon が Unity 6 に対応するまでコンパイルすら通らない。
> 共通調査のフェーズ 2（先行検証）の対象外。
>
> DennokoEx と同じ制約だが、ShadowEx のほうが依存が少ない
> （VRC Light Volumes / AudioLink / NDMF に非依存）ため、
> **lilToon さえ対応すれば検証を開始できる**。DennokoEx より先に検証できる。

## 検証チェックリスト（Unity 6 + lilToon 対応版）

- [ ] コンパイルエラー・警告が 0 件
- [ ] asmdef の `lilToon.Editor` 参照が解決される
- [ ] `ShadowExInspector` が lilToon マテリアルのインスペクタとして表示される
- [ ] `_Popup` アイコンボタンが正しく描画される
- [ ] `Assets/dennokoworks/ShadowEx/Convert material to custom shader` が動作する
- [ ] 変換後のマテリアルが正しいシェーダを参照している
- [ ] **`Shader/custom.hlsl` / `custom_insert.hlsl` がコンパイルエラーなく解決される**
- [ ] lilToon の `BEFORE_*` フックマクロが正しく展開されている
- [ ] シェーダの見た目（影の表現）が 2022.3 と一致する
- [ ] `Tools/dennokoworks/ShadowEx/FX Mask Packer` が動作する
- [ ] **出力されたマスクテクスチャが フェーズ 1 で保存した基準データと一致する**
- [ ] マスクテクスチャのインポート設定が正しく適用される（`TextureImporter` 経路）
- [ ] アバターのアップロードが成功する
