# Lighting Planner Export Validation Scene

Open `LightingPlannerExportValidation.unity` and run `ArtNet/Lighting Planner/Export Show Package`.

Committed sample JSON lives in `Package/Documentation~/ValidationSamples/lighting_planner_export_validation/`.

## Reproduction

1. Open `Dev/` as the Unity project.
2. Open the sample scene at `Samples/ArtNet-Unity/0.2.0/Receive Art-Net packet/Scenes/LightingPlannerExportValidation.unity`.
3. Run `ArtNet/Lighting Planner/Export Validation Scene (Fixed Folder)`.
4. Confirm that `manifest.json`, `rig.json`, and `intent.json` were written to `Package/Documentation~/ValidationSamples/lighting_planner_export_validation/`.

`timeline.json` is not expected here.
The validation flow treats it as planner-owned sidecar data rather than part of the exported package contract.

The fixed-folder export menu uses `LightingPlannerExportValidationBatch.ExportValidationScene` and is intended for refreshing the committed sample in-place.

Check these points in the exported files:

- `manifest.json`
  - `schemaVersion` is `1.0.0`
  - `rigFile` is `rig.json`
  - `intentFile` is `intent.json`
  - `packageId` and `rigId` are derived from the scene name
- `rig.json`
  - contains a fixture type from `profileName: Validation LED Wash RGB Strobe`
  - that fixture type has `channelFootprint: 5`
  - its channels include:
    - `0: dimmer`
    - `1: color.red`
    - `2: color.green`
    - `3: color.blue`
    - `4: strobe`
  - contains fixtures named `Rear Wash L 01` and `Front Key R 01`
  - their semantic roles are split:
    - `Rear Wash L 01` -> `BackLight`, `Wash`
    - `Front Key R 01` -> `KeyLight`, `FrontLight`
- `intent.json`
  - `schemaVersion` is `1.0.0`
  - `rigId` matches `manifest.json` / `rig.json`
  - default template values are present (`bpm: 120`, empty `sections`)

Additional fixtures may appear in the export if the sample scene already contains other `DmxFixture` objects. For validation, the two fixtures above are the required checkpoints.
