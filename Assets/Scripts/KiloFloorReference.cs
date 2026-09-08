using System;
using System.Collections.Generic;
using System.Threading;
using LitJson;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.XR;

namespace Robot
{
    // Reports the runtime coordinate reference, not a measured room surface or
    // physical floor accuracy. All exported poses remain native PICO coordinates.
    public sealed class KiloFloorReference : MonoBehaviour
    {
        private static KiloFloorReference _instance;
        private static readonly string OriginId = Guid.NewGuid().ToString("D");
        private readonly List<XRInputSubsystem> _inputs = new List<XRInputSubsystem>();
        private readonly HashSet<XRInputSubsystem> _subscribed = new HashSet<XRInputSubsystem>();
        private static long _generation;
        private string _lastOrigin;
        private bool _paused;
        private bool _running;
        private bool _requested;
        private float _nextRequestTime;

        public readonly struct Capture
        {
            public readonly long Generation;
            public readonly string ActualOrigin;
            public readonly string Reason;

            public Capture(long generation, string actualOrigin, string reason)
            {
                Generation = generation;
                ActualOrigin = actualOrigin;
                Reason = reason;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var owner = new GameObject("KILO floor reference");
            DontDestroyOnLoad(owner);
            _instance = owner.AddComponent<KiloFloorReference>();
            PXR_Plugin.System.RecenterSuccess += _instance.OriginChanged;
        }

        private void OriginChanged()
        {
            Interlocked.Increment(ref _generation);
        }

        private void OriginChanged(XRInputSubsystem input)
        {
            OriginChanged();
        }

        private void OnApplicationFocus(bool focused)
        {
            OriginChanged();
            if (focused)
            {
                _requested = false;
                _nextRequestTime = 0;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _paused = paused;
            OriginChanged();
            if (!paused)
            {
                _requested = false;
                _nextRequestTime = 0;
            }
        }

        private void OnDestroy()
        {
            OriginChanged();
            PXR_Plugin.System.RecenterSuccess -= OriginChanged;
            foreach (var input in _subscribed)
                input.trackingOriginUpdated -= OriginChanged;
            if (_instance == this)
                _instance = null;
        }

        private void Prepare()
        {
            _inputs.Clear();
            SubsystemManager.GetInstances(_inputs);
            bool running = false;
            foreach (var input in _inputs)
            {
                if (_subscribed.Add(input))
                {
                    input.trackingOriginUpdated += OriginChanged;
                    OriginChanged();
                    _requested = false;
                }
                running |= input.running;
            }

            if (running != _running)
            {
                _running = running;
                _requested = false;
                OriginChanged();
            }

            if (!running || _paused || !Application.isFocused || _requested ||
                Time.realtimeSinceStartup < _nextRequestTime)
                return;

            _nextRequestTime = Time.realtimeSinceStartup + 1;
            foreach (var input in _inputs)
                if (input.running &&
                    (input.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0)
                    input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);

#if UNITY_ANDROID && !UNITY_EDITOR
            // The public PXR_System wrapper discards this native return code.
            // Readback in Read() is still required even if this request succeeds.
            try
            {
                _requested = PXR_Plugin.Pxr_SetTrackingOrigin(PxrTrackingOrigin.Floor) == 0;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
#endif
        }

        private Capture Read()
        {
            string actual = "unknown";
            string reason = "native_origin_unavailable";
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var mode = (PxrTrackingOrigin)(-1);
                int result = PXR_Plugin.Pxr_GetTrackingOrigin(ref mode);
                if (result == 0)
                {
                    actual = mode == PxrTrackingOrigin.Floor ? "floor" :
                        mode == PxrTrackingOrigin.Eye ? "eye" : "unknown";
                    reason = actual == "floor" ? "" : "origin_not_floor";
                }
                // Do not clear the native recenter notification: PXR_Manager owns it.
                // Checking it here also covers a recenter before its Unity callback.
                if (PXR_Plugin.System.UPxr_GetHomeKey())
                    reason = "recenter_pending";
            }
            catch (DllNotFoundException)
            {
                actual = "unknown";
                reason = "native_origin_unavailable";
            }
            catch (EntryPointNotFoundException)
            {
                actual = "unknown";
                reason = "native_origin_unavailable";
            }
#endif
            if (_lastOrigin != actual)
            {
                _lastOrigin = actual;
                OriginChanged();
            }
            if (_paused || !Application.isFocused)
                reason = "application_inactive";
            else if (!_running)
                reason = "tracking_subsystem_stopped";
            return new Capture(Interlocked.Read(ref _generation), actual, reason);
        }

        public static Capture BeginCapture()
        {
            if (_instance == null)
                return new Capture(Interlocked.Read(ref _generation), "unknown", "client_not_initialized");
            _instance.Prepare();
            return _instance.Read();
        }

        public static JsonData EndCapture(Capture before, bool headTracked)
        {
            var after = _instance == null
                ? new Capture(Interlocked.Read(ref _generation), "unknown", "client_not_initialized") : _instance.Read();
            string reason = before.Reason.Length != 0 ? before.Reason : after.Reason;
            if (before.Generation != after.Generation || before.ActualOrigin != after.ActualOrigin)
                reason = "origin_changed_during_capture";
            else if (reason.Length == 0 && !headTracked)
                reason = "head_position_unavailable";

            bool available = reason.Length == 0 && after.ActualOrigin == "floor";
            var metadata = new JsonData();
            metadata["schemaVersion"] = 1;
            metadata["originId"] = OriginId;
            metadata["generation"] = after.Generation;
            metadata["requestedOrigin"] = "floor";
            metadata["actualOrigin"] = after.ActualOrigin;
            metadata["status"] = available ? "runtime_floor" : "unavailable";
            metadata["reason"] = reason;
            metadata["poseFrame"] = "pico_tracking";
            metadata["plane"] = null;
            if (available)
            {
                var normal = new JsonData();
                normal.Add(0.0);
                normal.Add(1.0);
                normal.Add(0.0);
                var plane = new JsonData();
                plane["normal"] = normal;
                plane["offsetM"] = 0.0;
                metadata["plane"] = plane;
            }
            return metadata;
        }
    }
}
