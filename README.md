# KILO XR Floor · PICO client

This is the **PICO headset client for [KILO](https://github.com/TToTMooN/KILO)**,
based on XRoboToolkit PICO **v1.1.1**. It adds explicit floor/origin metadata and
a separately installable Android app for floor-aware teleoperation.

**[Download APK](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/releases/download/v1.1.1-kilo-floor.1/KILO-XR-Floor.apk)**
· **[Release and checksums](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/releases/tag/v1.1.1-kilo-floor.1)**
· **[Setup, build and floor contract](KILO-FLOOR.md)**
· **[Remaining client work](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/issues/2)**

## Start here

1. Install **KILO XR Floor** from the release above. It is a development-signed
   APK, package `com.kilo.xrobotoolkit.floor`, and can coexist with the stock app.
2. **Calibrate the physical floor in PICO system Play Boundary / Adjust Floor
   Level setup.** Place a controller on the actual floor and confirm the displayed
   grid. Menu names vary by OS; Home recenter does not replace this step.
3. Follow [KILO's VR startup guide](https://github.com/TToTMooN/KILO/blob/codex/luna-rt/docs/online/vr-first-run.md)
   for PC Service, sender and desktop simulation. Choose the actual head/chest
   mounting and keep motion held while checking a known controller height.

The app supplies headset/controller poses and controls. KILO owns robot mapping,
calibration, motion enable, solvers and the simulation workbench. The PC Service
and native transport remain upstream. Quest support is separate work.

## Which source should I use?

- **`kilo-pico`** is the maintained KILO source branch. Start new KILO changes here.
- **`v1.1.1-kilo-floor.1`** pins the first KILO development release. Its assets
  retain the exact APK, build-time source snapshot and source patch.
- **`v1.1.1`** is the original upstream baseline. [Compare the KILO changes](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/compare/v1.1.1...kilo-pico).
- **`main`** retains separate pre-existing client work; it is not the KILO release
  line. The initial **`codex/floor-reference`** and **`codex/pico-v1.1.1`** branches
  are historical references for [PR #1](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/pull/1).

```bash
git clone --branch kilo-pico \
  https://github.com/TToTMooN/XRoboToolkit-Unity-Client.git
```

For an exact released checkout, select the release tag instead. Published tags
and APK assets are retained; new builds receive new versions rather than moving
an existing tag. Use short-lived feature branches and PRs targeting `kilo-pico`
for future work.

## What changed from upstream?

- Native Floor mode is requested and read back. `floorReference` reports actual
  origin, availability, a floor plane and origin identity/generation.
- Observed origin, recenter and lifecycle changes invalidate the consumer's old
  reference. Native head/controller coordinates are preserved.
- Controller prediction uses the SDK's millisecond time unit.
- A separate Android build entry point pins **Unity 2022.3.62f3**, IL2CPP/ARM64,
  and the KILO package identity. [Build instructions](KILO-FLOOR.md#build-and-install-alongside-stock).

Relevant source: [floor/origin metadata](Assets/Scripts/KiloFloorReference.cs),
[pose capture](Assets/Scripts/TrackingData.cs),
[Android build entry point](Assets/Editor/KiloFloorBuild.cs), and
[build launcher](build-kilo-floor.sh). The Unity scene is `Assets/Main.unity`.

## Qualification status

The first APK was built, installed and used in a physical PICO-to-KILO simulation
check. Native Floor metadata, scale-1 raw-to-mapped height agreement and recenter
invalidation with deliberate KILO recalibration were observed. After PICO system
floor setup, the operator reported corrected-looking height; precise physical
floor accuracy and controller contact offsets were not measured.

**Per-controller tracking validity remains unfinished.** Floor availability and
fresh messages do not establish complete tracking qualification or robot safety.
See [the scoped validation record](KILO-FLOOR.md#focused-physical-check-8-september-2026)
and [client issue #2](https://github.com/TToTMooN/XRoboToolkit-Unity-Client/issues/2).
Rest-pose design, gradual acquisition before faster tracking and cost tuning
remain [KILO follow-ups](https://github.com/TToTMooN/KILO/issues/59).

## Upstream reference

Upstream: [XR-Robotics/XRoboToolkit-Unity-Client](https://github.com/XR-Robotics/XRoboToolkit-Unity-Client),
PICO v1.1.1 at `9f775b535d781618bd2bb7ef8d6c414c0531387c`.
The [original client README](Docs/UPSTREAM-README.md) is retained for its UI and
feature reference; use the KILO setup/build instructions linked above for this
branch.
