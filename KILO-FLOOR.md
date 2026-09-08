# KILO floor-reference PICO client

The maintained source branch is **`kilo-pico`**. Start from the
[repository overview](README.md) for the APK, release tag and upstream comparison.

Based on XRoboToolkit PICO client v1.1.1, commit
`9f775b535d781618bd2bb7ef8d6c414c0531387c`. This development fork requests
the PICO runtime's floor origin and reports the resulting reference alongside
the existing native poses. It does not estimate a room mesh, infer chest height,
or certify the physical accuracy of the runtime floor.

## Build and install alongside stock

Use **Unity 2022.3.62f3** with an activated editor license and its **Android
Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules. The
upstream 2022.3.16f1 pin is retained in Git history; the new pin includes Unity's
[Android security fix](https://unity.com/security/sept-2025-01).

**First APK built successfully on 8 September 2026**, using Ubuntu 24.04 x86-64,
IL2CPP ARM64, SDK API 31 (minimum 30), build-tools 34.0.0 and NDK 23.1.7779620.
The package is `com.kilo.xrobotoolkit.floor`, version `1.1.1-kilo-floor.1` (code 1),
34,347,991 bytes, with a verified APK v2 development signature. SHA-256:
`7161fed1e706ccdf26e0fcfc2a1419dbe2977bf9e21033c9f9887526917fd72d`.
This APK was installed and tested on a PICO headset. Floor metadata and
recenter/origin notifications were observed; after correcting the PICO system
floor, the operator reported the height looked correct. Exact physical height and complete
controller tracking remain unqualified; see the scoped results below. Package
restoration may require internet.

The [development prerelease](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/releases/tag/v1.1.1-kilo-floor.1)
contains the tested APK, original build provenance, retained source patch and
checksums. The built source snapshot is
`494dc4f0227949b4826a69ca65e2928f3fa63554`; later setup-documentation changes
do not rebuild or change this APK.

```bash
./build-kilo-floor.sh /path/to/2022.3.62f3/Editor/Unity
# After a successful build, and with the headset connected through adb:
adb install -r Builds/KILO-XR-Floor.apk
```

Use Unity Hub to install the editor and Android modules listed above, activate
the editor license, and open this checkout once to complete package restoration.
The script accepts the supplied editor path and writes its log alongside the
APK. Enable USB debugging and authorize the connected computer on the headset
before using `adb install`. Per-controller tracking evidence and remaining
headset qualification stay open in
[issue #2](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/issues/2).

The app appears as **KILO XR Floor**, package **com.kilo.xrobotoolkit.floor**.
Stock **com.xrobotoolkit.client** remains installed. Run only the chosen client
when connecting to PC Service. The batch entry point uses a local Android
development signing key rather than upstream's private keystore; keep that
key to update this package without uninstalling it. Build output and logs are
ignored by git. This adds no Android permissions beyond the upstream manifest.

## Set the physical floor before teleop

**Required: calibrate the floor in PICO system setup.** Selecting Floor in this
app only selects the runtime reference; it cannot correct an incorrectly placed
system floor. Home-button recenter is not a physical floor-height measurement.

1. Keep robot motion held and temporarily wear the headset on your head to see
   setup. Open system **Play Boundary / Boundary** and choose **Adjust Floor
   Level**, or recreate the boundary to reach its floor step. Labels vary by
   PICO OS version.
2. Look down, gently place a controller on the actual floor and follow the
   confirmation prompt. Check that the displayed floor grid matches the room
   floor. Prefer this physical check to accepting an incorrect automatic height.
   [PICO floor-setting guidance](https://business.picoxr.com/us/doc/mpd3avqz).
3. Finish boundary setup, restore the intended head/chest placement, and open
   **KILO XR Floor**. Enable Head, Controller and Send; disable **Switch w/ A
   Button**. Keep controls released and verify fresh input after returning.
4. Check the incoming native controller Y coordinate against one known height
   while motion remains held. A controller housing touching the floor does not
   place its internal tracked origin exactly at zero; precise contact needs the
   measured offset. Receiving applications must invalidate their calibration
   after an origin change and require deliberate recalibration before resuming.

A chest-mounted headset reports its tracked device pose; it is not automatically
a chest landmark or tool frame.

## First import in the Linux editor

Open `Assets/Main.unity` from the Project panel after import. An empty
`Untitled` scene is not the headset client scene. The batch build already
selects `Assets/Main.unity` through `EditorBuildSettings`.

The initial build-script metadata contained a 34-character GUID, causing Unity
to ignore `Assets/Editor/KiloFloorBuild.cs`. Its GUID is now corrected to 32
hexadecimal characters. After updating, return focus to Unity or use
**Assets → Refresh**, then clear historical Console entries. Deleting the
project or its Library directory is not necessary for this metadata correction.

The bundled spatial-audio sample mixers can log missing **Pico Ambisonic
Renderer** / **Pico Audio Router** effects when imported on Linux. Their native
plugins ship for Android, Windows and macOS; this SDK has no Linux editor audio
binary. Android ARM64 libraries and Android-enabled import settings are present,
and the application scene does not reference these sample mixers. Keep the
Android plugins unchanged. These messages do not establish an Android build
failure; APK compilation and headset behavior still require qualification.

## Tracking JSON extension

Each existing tracking JSON object additionally contains:

```json
{
  "floorReference": {
    "schemaVersion": 1,
    "originId": "6ce7822b-746e-45d3-94d5-1eea0c23b33c",
    "generation": 4,
    "requestedOrigin": "floor",
    "actualOrigin": "floor",
    "status": "runtime_floor",
    "reason": "",
    "poseFrame": "pico_tracking",
    "plane": {"normal": [0.0, 1.0, 0.0], "offsetM": 0.0}
  }
}
```

`originId` is a UUID generated for the application lifecycle. `generation`
increases for observed origin/recenter, focus and pause changes. Consumers must
invalidate their previous registration when either identifier changes; the
counter is not a pose timestamp. The plane equation is
`normal · point + offsetM = 0`, with points in metres in the **same native
PICO tracking coordinates** as the transmitted Head and Controller poses.
No scene-rig transform or fixed camera-height offset is added to these poses.

`actualOrigin` is `floor`, `eye` or `unknown`, using successful native PICO
readback, not the requested Unity setting. `status: runtime_floor` requires
floor readback, a running tracking subsystem, focused/unpaused application, a reported 6DoF Head sample and
stable observed origin across the full capture. This is **runtime reference
availability**, not independent physical floor validation. There is no SDK
floor-accuracy measurement being reported. The existing controller-validity
and producer-timestamp limitations are unchanged.

When these conditions fail, `status` is `unavailable`, `plane` is `null`, and
`reason` explains the failure: `native_origin_unavailable`, `origin_not_floor`,
`recenter_pending`, `application_inactive`, `origin_changed_during_capture`,
`head_position_unavailable`, `tracking_subsystem_stopped` or
`client_not_initialized`. A stage result is
reported as `unknown`; this fork deliberately requests and recognizes Floor.

The capture is bracketed by native mode readback and an observed event
generation. Unity origin updates, PICO recenter callbacks and pending native
Home notifications fence reference changes; focus/pause changes invalidate
registration conservatively. These checks cannot prove sensor-acquisition
atomicity or detect an unreported runtime relocation. A consumer must hold
through unavailable or changed reference metadata and require recalibration.

## Changes and qualification

- `Assets/Scripts/KiloFloorReference.cs` requests Floor, observes lifecycle and
  origin changes, reads the actual native mode and constructs metadata.
- `Assets/Scripts/TrackingData.cs` brackets the complete existing pose capture.
  Controller prediction receives milliseconds as documented by `PXR_Input`;
  only the existing wire `predictTime` field is converted to microseconds.
  The upstream path incorrectly reused that multiplied value for prediction.
- The XR Origin prefab requests Floor with no artificial eye-height offset.
- `Assets/Editor/KiloFloorBuild.cs` and the build script create the separate APK.

### Focused physical check, 8 September 2026

- The installed APK sent fresh native head/controller messages with
  `actualOrigin=floor`, `status=runtime_floor`, and native floor Y=0.
- With controller housings on the physical floor, the initial incoming heights
  were about 0.550 m and 0.555 m. The physical-floor correspondence failed
  before system floor setup was corrected.
- After PICO system floor/boundary calibration, the operator reported that the
  height looked correct. No synchronized measured post-setup ground sample was
  collected, so this is operator confirmation rather than a quantified accuracy
  claim. Housing-to-tracked-origin offsets were not measured.
- One operator-reported Home recenter generated origin notifications. This
  checks that the client reports the reference change; receiving applications
  remain responsible for invalidating their previous registration.

Per-controller validity and native return/status evidence remain unfinished in
[issue #2](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/issues/2).
Fresh messages and floor availability do not establish controller tracking
qualification, acquisition timestamps, physical contact accuracy or robot safety.

Known client bug: after an active runtime changes Floor to Eye/Stage, the client
reports unavailable metadata but does not retry Floor until a focus/pause or
subsystem transition. The [Floor-mode retry review finding](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/pull/1#pullrequestreview-5147800494)
remains unresolved in the current APK and is tracked in issue #2.
