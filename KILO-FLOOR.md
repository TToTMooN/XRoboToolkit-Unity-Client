# KILO floor-reference PICO client

Based on XRoboToolkit PICO client v1.1.1, commit
`9f775b535d781618bd2bb7ef8d6c414c0531387c`. This development fork requests
the PICO runtime's floor origin and reports the resulting reference alongside
the existing native poses. It does not estimate a room mesh, infer chest height,
or certify the physical accuracy of the runtime floor.

## Build and install alongside stock

Use **Unity 2022.3.16f1** with an activated editor license and its **Android Build
Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules. Let this pinned
Unity installation supply the native toolchain; the project targets Android
API 31, minimum API 30, with IL2CPP ARM64. Package restore may require internet.
An installed APK cannot be produced from this source without the Unity editor.

```bash
./build-kilo-floor.sh /path/to/2022.3.16f1/Editor/Unity
# After a successful build, and with the headset connected through adb:
adb install -r Builds/KILO-XR-Floor.apk
```

The app appears as **KILO XR Floor**, package **com.kilo.xrobotoolkit.floor**.
Stock **com.xrobotoolkit.client** remains installed. Run only the chosen client
when connecting to PC Service. The batch entry point uses a local Android
development signing key rather than upstream's private keystore; keep that
key to update this package without uninstalling it. Build output and logs are
ignored by git. This adds no Android permissions beyond the upstream manifest.

Configure the headset's floor/boundary through its system setup before use.
In the app, enable Head, Controller and Send; disable **Switch w/ A Button**
when using KILO's controller bindings. A chest-mounted headset reports the
tracked device pose: it is not automatically a chest landmark or tool frame.
Check a known physical height before relying on floor-aware mapping.

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

Building does not qualify behavior on a headset. Acceptance must include the
actual installed package, Floor readback, a known-height comparison, headset
mounting, Home recenter, focus/pause and resumed tracking. Verify each origin
change reaches KILO and inhibits the old calibration before accepting new
motion. Room geometry and controller-to-contact/tool calibration remain
separate work.
