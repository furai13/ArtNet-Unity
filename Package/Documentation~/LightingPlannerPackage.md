## Lighting Planner Package Format

`ArtNet/Lighting Planner/Export Show Package` exports a JSON package for the Lighting Planner application.

The exporter writes the following files:

- `manifest.json`
- `rig.json`
- `intent.json`

`timeline.json` is intentionally not part of the exported package contract at this time.
If planner-side tools need timeline or cue data, that data should remain a planner-owned sidecar file until a stable interchange format is defined.

The purpose of this package is to share fixture layout, DMX patch data, and planning context in a format that is easy for the planner application to consume.

> [!NOTE]
> Runtime playback data is expected to be transmitted over Art-Net. These JSON files are static interchange data for planning and preprocessing.

### Design Intent

The package assumes a clear split of responsibilities.

- ArtNet-Unity
  - Source of truth for rig data
  - DMX patch management
  - Fixture semantic metadata
  - Art-Net receiving and playback
- Lighting Planner
  - Rig ingestion
  - Show planning
  - Look and timeline generation
  - Art-Net sending

To support this split, the rig data is divided into:

- `fixtureTypes`
  - Shared channel layout definitions
- `fixtures`
  - Per-instance placement, patch, and semantics

This avoids duplicating channel definitions for every fixture instance.

## timeline.json Position

`timeline.json` should remain a planner-specific sidecar, not a formal export target, for the current exporter design.

Reasoning based on the current implementation:

- `manifest.json` only references `rig.json` and `intent.json`
- the exporter generates static rig data plus an intent template, but no timeline authoring data
- validation samples and the fixed-folder validation flow assert only the three exported files
- planner-side timeline generation is explicitly part of the planner responsibility split

Promoting `timeline.json` to a first-class exported file now would create a contract without a stable source of truth in ArtNet-Unity.
That would force downstream consumers to distinguish between a real timeline, an empty placeholder, and planner-private metadata, which is not modeled today.

## Sidecar Compatibility Policy

Planner-specific sidecar files are allowed to live next to the exported package, but they are outside the ArtNet-Unity package contract unless referenced by `manifest.json`.

Current policy:

- package consumers must treat `manifest.json`, `rig.json`, and `intent.json` as the only required files
- unreferenced extra files such as `timeline.json` must be treated as optional and ignored by generic importers
- planner-specific sidecars may evolve independently without requiring an ArtNet-Unity schema bump

If `timeline.json` is standardized later, add it additively:

- introduce a new manifest field such as `timelineFile`
- keep existing packages valid when that field is absent
- treat the absence of `timelineFile` as "no standardized timeline included"
- avoid changing the meaning of existing `rig.json` or `intent.json` fields to backfill timeline semantics

## manifest.json

`manifest.json` is the package entry point and contains references to the related files.

### Fields

| Name            | Type   | Description                      |
|-----------------|--------|----------------------------------|
| `schemaVersion` | string | JSON schema version              |
| `packageId`     | string | Package identifier               |
| `title`         | string | Display title                    |
| `createdAt`     | string | Export timestamp in ISO 8601     |
| `rigFile`       | string | Rig definition file name         |
| `intentFile`    | string | Intent definition file name      |
| `rigId`         | string | Referenced rig identifier        |

## rig.json

`rig.json` is the main file used by the planner to understand the rig.

### Top-level structure

| Name               | Type     | Description                          |
|--------------------|----------|--------------------------------------|
| `schemaVersion`    | string   | JSON schema version                  |
| `rigId`            | string   | Rig identifier                       |
| `coordinateSystem` | object   | Coordinate system definition         |
| `addressing`       | object   | Art-Net / DMX addressing baseline    |
| `fixtureTypes`     | object[] | Shared fixture type definitions      |
| `fixtures`         | object[] | Scene fixture instances              |

### coordinateSystem

The current exporter emits Unity world-space coordinates.

| Name         | Type   | Value   |
|--------------|--------|---------|
| `space`      | string | `unity` |
| `unit`       | string | `meter` |
| `handedness` | string | `left`  |

### addressing

The current exporter emits the following addressing rules.

| Name                     | Type | Value |
|--------------------------|------|-------|
| `universeBase`           | int  | `0`   |
| `addressBase`            | int  | `0`   |
| `maxChannelsPerUniverse` | int  | `512` |

> [!NOTE]
> `startAddress` and channel `offset` are 0-based.
> Absolute channel index is `startAddress + offset`.

### fixtureTypes

`fixtureTypes` defines the shared channel layout for fixtures with the same structure.

#### Fields

| Name               | Type     | Description                           |
|--------------------|----------|---------------------------------------|
| `fixtureTypeId`    | string   | Fixture type identifier               |
| `displayName`      | string   | Display name, usually `profileName`   |
| `channelFootprint` | int      | Used channel count                    |
| `capabilities`     | string[] | Capabilities inferred from channels   |
| `channels`         | object[] | Channel layout definition             |

#### channels fields

| Name         | Type   | Description                                       |
|--------------|--------|---------------------------------------------------|
| `offset`     | int    | Relative channel from fixture start               |
| `functionId` | string | Machine-readable function identifier              |
| `label`      | string | Original Unity descriptor name                    |
| `resolution` | string | `8bit`, `16bit_coarse`, or `16bit_fine`          |
| `pairKey`    | string | Key for grouping coarse/fine pairs                |

#### functionId intent

`functionId` is the normalized key the planner uses to understand channel meaning.  
Typical values include:

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
- `color.wheel`
- `gobo.select`
- `gobo.rotate`
- `control`
- `value`
- `unknown.*`

#### fixtureTypeId generation

The exporter generates `fixtureTypeId` from `profileName` and footprint.  
If the same display name is used by multiple channel layouts, the exporter appends a hash suffix to avoid collisions.

This supports the following assumptions.

- The same `fixtureTypeId` always means the same channel layout
- The same `fixtureTypeId` always means the same functional map

### fixtures

`fixtures` describes each fixture instance in the scene.

#### Fields

| Name            | Type   | Description                              |
|-----------------|--------|------------------------------------------|
| `fixtureId`     | string | Fixture instance identifier              |
| `fixtureTypeId` | string | Referenced fixture type ID               |
| `name`          | string | Unity GameObject name                    |
| `patch`         | object | Universe and start address               |
| `transform`     | object | Position and rotation                    |
| `semantics`     | object | Planner-facing semantic metadata         |

#### semantics

When `FixtureSemanticBinding` is present, its resolved data is exported here.

| Name             | Type     | Description                              |
|------------------|----------|------------------------------------------|
| `primaryRole`    | string   | First item in `roles`                    |
| `secondaryRoles` | string[] | Remaining role items                     |
| `roles`          | string[] | Full role list                           |
| `capabilities`   | string[] | Semantic-layer capabilities              |
| `tags`           | string[] | Additional tags                          |
| `exclusionTags`  | string[] | Exclusion tags                           |
| `properties`     | object[] | Key-value properties                     |
| `priority`       | float    | Priority value in range 0 to 1           |

## intent.json

`intent.json` is exported as a template file for planner-side show conditions.

### Top-level structure

| Name              | Type     | Description                          |
|-------------------|----------|--------------------------------------|
| `schemaVersion`   | string   | JSON schema version                  |
| `rigId`           | string   | Referenced rig ID                    |
| `showId`          | string   | Show identifier                      |
| `title`           | string   | Show title                           |
| `bpm`             | float    | BPM                                  |
| `durationSeconds` | float    | Duration in seconds                  |
| `globalIntent`    | object   | Global planning guidance             |
| `sections`        | object[] | Per-section intent data              |

### globalIntent

| Name            | Type     | Description                    |
|-----------------|----------|--------------------------------|
| `styleKeywords` | string[] | Style hints                    |
| `paletteHints`  | string[] | Palette hints                  |
| `constraints`   | object   | Planning constraints           |

### constraints

| Name                    | Type  | Description                    |
|-------------------------|-------|--------------------------------|
| `avoidAudienceHit`      | bool  | Avoid audience lighting        |
| `preserveFaceVisibility`| bool  | Keep faces visible             |
| `limitStrobe`           | bool  | Limit strobe usage             |
| `maxBrightness`         | float | Global brightness ceiling      |
| `maxMotionDensity`      | float | Motion density ceiling         |

## Planner-side usage

The intended workflow is:

1. Read `manifest.json`
2. Load `rig.json` and build a `fixtureTypes` dictionary
3. Read `fixtures` for placement, patch, and semantic metadata
4. Edit or generate `intent.json`
5. Build channel maps from `fixtureType.channels`
6. Convert planned looks and cues into Art-Net frames

The planner should treat `fixtureType.channels` as the source of truth for channel meaning, rather than inferring meaning from fixture instances alone.

## Compatibility

- The format is versioned by `schemaVersion`
- Future changes should prefer additive fields
- Changing the meaning of an existing `fixtureTypeId` should be avoided
- Additional planner-owned sidecar files are non-contract unless explicitly referenced by `manifest.json`
