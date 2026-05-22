## Lighting Planner Package フォーマット

`ArtNet/Lighting Planner/Export Show Package` は、Lighting Planner アプリ向けに JSON ファイル一式を出力する。

出力されるファイルは以下の 3 つ。

- `manifest.json`
- `rig.json`
- `intent.json`

このフォーマットの目的は、Unity シーン内の灯体構成と演出の前提条件を、Lighting Planner アプリが解釈しやすい形で共有することにある。

> [!NOTE]
> 実行時の制御データは Art-Net で送受信する想定であり、この JSON は事前共有用の静的データである。

### 設計意図

Lighting Planner Package は、以下の責務分離を前提としている。

- ArtNet-Unity
  - リグ情報の正本
  - DMX patch 情報の管理
  - 灯体の意味情報の保持
  - Art-Net 受信による再生
- Lighting Planner
  - 灯体構成の読込
  - 演出案の生成
  - タイムラインや Look の設計
  - Art-Net 送信

このため、共有データは次の 2 層に分離している。

- `fixtureTypes`
  - 同じチャンネル構成を持つ灯体タイプ定義
- `fixtures`
  - シーン内の個体情報

これにより、同型灯体のチャンネル構成を各個体に重複保持せずに済む。

## manifest.json

`manifest.json` は package 全体の入口であり、関連ファイルへの参照と識別情報を持つ。

### フィールド

| Name            | Type   | Description                         |
|-----------------|--------|-------------------------------------|
| `schemaVersion` | string | JSON schema のバージョン            |
| `packageId`     | string | package の識別子                    |
| `title`         | string | package の表示名                    |
| `createdAt`     | string | 出力時刻。ISO 8601 形式             |
| `rigFile`       | string | rig 定義 JSON のファイル名          |
| `intentFile`    | string | intent 定義 JSON のファイル名       |
| `rigId`         | string | この package が参照する rig の識別子 |

### 例

```json
{
  "schemaVersion": "1.0.0",
  "packageId": "main_stage",
  "title": "Main Stage",
  "createdAt": "2026-04-11T10:00:00.0000000+09:00",
  "rigFile": "rig.json",
  "intentFile": "intent.json",
  "rigId": "main_stage"
}
```

## rig.json

`rig.json` は Lighting Planner が灯体構成を理解するための中心ファイルである。

### トップレベル構造

| Name               | Type     | Description                                 |
|--------------------|----------|---------------------------------------------|
| `schemaVersion`    | string   | JSON schema のバージョン                    |
| `rigId`            | string   | rig の識別子                                |
| `coordinateSystem` | object   | 座標系の定義                                |
| `addressing`       | object   | Art-Net / DMX addressing の基準             |
| `fixtureTypes`     | object[] | 灯体タイプ定義                              |
| `fixtures`         | object[] | シーン内の灯体個体定義                      |

### coordinateSystem

現在の exporter は Unity 座標系をそのまま出力する。

| Name         | Type   | Value   |
|--------------|--------|---------|
| `space`      | string | `unity` |
| `unit`       | string | `meter` |
| `handedness` | string | `left`  |

### addressing

現在の exporter は以下の基準を出力する。

| Name                    | Type | Value |
|-------------------------|------|-------|
| `universeBase`          | int  | `0`   |
| `addressBase`           | int  | `0`   |
| `maxChannelsPerUniverse`| int  | `512` |

> [!NOTE]
> `startAddress` と各 `channels.offset` は 0-based である。
> 絶対チャンネルは `startAddress + offset` で求める。

### fixtureTypes

`fixtureTypes` は、同一のチャンネル構成を持つ灯体の共通定義である。

#### フィールド

| Name               | Type     | Description                                  |
|--------------------|----------|----------------------------------------------|
| `fixtureTypeId`    | string   | 灯体タイプの識別子                           |
| `displayName`      | string   | 表示名。通常は `profileName`                 |
| `channelFootprint` | int      | 使用チャンネル数                             |
| `capabilities`     | string[] | タイプから推定された機能一覧                 |
| `channels`         | object[] | チャンネル定義                               |

#### channels フィールド

| Name         | Type   | Description                                                |
|--------------|--------|------------------------------------------------------------|
| `offset`     | int    | 灯体先頭からの相対チャンネル                               |
| `functionId` | string | 機械可読な機能識別子                                       |
| `label`      | string | Unity 側 descriptor 名                                     |
| `resolution` | string | `8bit`, `16bit_coarse`, `16bit_fine` のいずれか            |
| `pairKey`    | string | coarse/fine を束ねるキー。`pan`, `tilt`, `value` など      |

#### functionId の意図

`functionId` は Lighting Planner 側でチャンネル意味を判定するための正規化キーである。  
代表例:

- `dimmer`
- `strobe`
- `pan`
- `tilt`
- `beam.angle`
- `color.red`
- `color.green`
- `color.blue`
- `color.white`
- `color.amber`
- `color.uv`
- `color.cyan`
- `color.magenta`
- `color.yellow`
- `color.wheel`
- `gobo`
- `gobo.rotation`
- `iris`
- `frost`
- `prism`
- `speed`
- `control`
- `value`
- `unknown.*`

`DirectValueModule` は、専用 Unity モジュールを持たない fixture 固有チャンネルに対して Planner 向けの `functionId` を export できる。
`iris`, `frost`, `prism`, `speed` や独自の `value` チャンネルのような 0..1 正規化制御に使う。
未設定の場合は後方互換のため `value` として export される。

#### fixtureTypeId の生成方針

`fixtureTypeId` は `profileName` と footprint をベースに exporter が自動生成する。  
同じ表示名で異なるチャンネル構成が存在する場合は、衝突回避のために hash suffix を付与する。

これは次の前提を置くためである。

- 同じ `fixtureTypeId` は同じチャンネル構成を持つ
- 同じ `fixtureTypeId` は同じ機能マップを持つ

### fixtures

`fixtures` はシーン内の各灯体個体を表す。

#### フィールド

| Name            | Type   | Description                                  |
|-----------------|--------|----------------------------------------------|
| `fixtureId`     | string | 灯体個体の識別子。hierarchy path から生成    |
| `fixtureTypeId` | string | 参照する灯体タイプ ID                        |
| `name`          | string | Unity 上の GameObject 名                     |
| `patch`         | object | Universe / StartAddress                      |
| `transform`     | object | Position / Rotation                          |
| `semantics`     | object | Lighting Planner 向けの意味情報              |

#### patch

| Name           | Type | Description        |
|----------------|------|--------------------|
| `universe`     | int  | 0-based universe   |
| `startAddress` | int  | 0-based start addr |

#### transform

| Name            | Type      | Description                  |
|-----------------|-----------|------------------------------|
| `position`      | float[3]  | Unity world position         |
| `rotationEuler` | float[3]  | Unity world rotation (Euler) |

#### semantics

`FixtureSemanticBinding` が付いている場合に、その内容を出力する。  
役割やタグが未設定でも JSON 自体は出力される。

| Name             | Type     | Description                                    |
|------------------|----------|------------------------------------------------|
| `primaryRole`    | string   | `roles` の先頭要素。未設定なら空文字           |
| `secondaryRoles` | string[] | `roles` の 2 要素目以降                        |
| `roles`          | string[] | 役割一覧                                       |
| `capabilities`   | string[] | semantic layer 上の capability                 |
| `tags`           | string[] | 補助タグ                                       |
| `exclusionTags`  | string[] | 除外タグ                                       |
| `properties`     | object[] | key-value property                             |
| `priority`       | float    | 0 から 1 の優先度                              |

### rig.json 例

```json
{
  "schemaVersion": "1.0.0",
  "rigId": "main_stage",
  "coordinateSystem": {
    "space": "unity",
    "unit": "meter",
    "handedness": "left"
  },
  "addressing": {
    "universeBase": 0,
    "addressBase": 0,
    "maxChannelsPerUniverse": 512
  },
  "fixtureTypes": [
    {
      "fixtureTypeId": "led_wash_7ch",
      "displayName": "LED Wash",
      "channelFootprint": 7,
      "capabilities": ["Dimmer", "Color", "Strobe"],
      "channels": [
        {
          "offset": 0,
          "functionId": "dimmer",
          "label": "Dimmer",
          "resolution": "8bit",
          "pairKey": ""
        },
        {
          "offset": 1,
          "functionId": "color.red",
          "label": "Red",
          "resolution": "8bit",
          "pairKey": ""
        }
      ]
    }
  ],
  "fixtures": [
    {
      "fixtureId": "stage_rear_wash_l_01",
      "fixtureTypeId": "led_wash_7ch",
      "name": "Rear Wash L 01",
      "patch": {
        "universe": 0,
        "startAddress": 0
      },
      "transform": {
        "position": [-4.0, 5.5, 8.0],
        "rotationEuler": [0.0, 180.0, 0.0]
      },
      "semantics": {
        "primaryRole": "BackLight",
        "secondaryRoles": ["Wash"],
        "roles": ["BackLight", "Wash"],
        "capabilities": ["Color", "Dimmer"],
        "tags": ["silhouette"],
        "exclusionTags": ["no_audience"],
        "properties": [
          {
            "key": "stageZone",
            "value": "UpStageLeft"
          }
        ],
        "priority": 0.8
      }
    }
  ]
}
```

## intent.json

`intent.json` は Lighting Planner 側で演出条件を入力するためのテンプレートである。  
現時点では exporter が初期値入りの空テンプレートを生成する。

### トップレベル構造

| Name             | Type     | Description                                 |
|------------------|----------|---------------------------------------------|
| `schemaVersion`  | string   | JSON schema のバージョン                    |
| `rigId`          | string   | 参照する rig ID                             |
| `showId`         | string   | show の識別子                               |
| `title`          | string   | show 名                                     |
| `bpm`            | float    | BPM                                         |
| `durationSeconds`| float    | 曲長                                         |
| `globalIntent`   | object   | 全体方針                                    |
| `sections`       | object[] | セクションごとの意図                        |

### globalIntent

| Name            | Type     | Description                    |
|-----------------|----------|--------------------------------|
| `styleKeywords` | string[] | スタイルキーワード             |
| `paletteHints`  | string[] | 色のヒント                     |
| `constraints`   | object   | 制約条件                       |

### constraints

| Name                   | Type  | Description                       |
|------------------------|-------|-----------------------------------|
| `avoidAudienceHit`     | bool  | 客席照射を避ける                  |
| `preserveFaceVisibility`| bool | 顔見せを優先する                  |
| `limitStrobe`          | bool  | ストロボ制限                      |
| `maxBrightness`        | float | 全体輝度上限                      |
| `maxMotionDensity`     | float | 動きの密度上限                    |

### sections

| Name        | Type     | Description                 |
|-------------|----------|-----------------------------|
| `sectionId` | string   | セクション識別子            |
| `label`     | string   | 表示名                      |
| `startTime` | float    | 開始秒                      |
| `endTime`   | float    | 終了秒                      |
| `energy`    | float    | エネルギー感                |
| `density`   | float    | 演出密度                    |
| `keywords`  | string[] | 補助キーワード              |

### intent.json 例

```json
{
  "schemaVersion": "1.0.0",
  "rigId": "main_stage",
  "showId": "show_001",
  "title": "New Show",
  "bpm": 120.0,
  "durationSeconds": 0.0,
  "globalIntent": {
    "styleKeywords": ["dramatic"],
    "paletteHints": ["#1C3D8F", "#D9A441"],
    "constraints": {
      "avoidAudienceHit": true,
      "preserveFaceVisibility": true,
      "limitStrobe": false,
      "maxBrightness": 1.0,
      "maxMotionDensity": 1.0
    }
  },
  "sections": [
    {
      "sectionId": "intro",
      "label": "Intro",
      "startTime": 0.0,
      "endTime": 18.0,
      "energy": 0.2,
      "density": 0.2,
      "keywords": ["dark", "build"]
    }
  ]
}
```

## Lighting Planner 側での利用指針

Lighting Planner 側では、以下の順序で使うことを想定している。

1. `manifest.json` を読み、関連ファイルを解決する
2. `rig.json` を読み、`fixtureTypes` の辞書を構築する
3. `fixtures` から個体配置と semantic 情報を読む
4. `intent.json` を編集または生成し、show 条件を定義する
5. `fixtureType.channels` を参照して Art-Net 送信用の channel map を構築する

重要なのは、Lighting Planner が `fixtures` だけで実チャンネルを解釈しないことにある。  
チャンネル意味は `fixtureTypes.channels` を正本として扱う。

## 互換性について

- このフォーマットは `schemaVersion` を持つ
- 将来の拡張では field の追加を優先し、既存 field の意味変更は避ける
- `fixtureTypeId` の意味は後方互換性に直結するため、同一 ID に対するチャンネル構成変更は避ける

