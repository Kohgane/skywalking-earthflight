// XRFlightTests.cs — NUnit EditMode tests for Phase 112: VR/XR Flight Experience
// Covers: enums, config, data models, platform adapters, gesture recognition,
//         comfort system, cockpit interactions, analytics, recenter controller.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.XR;

[TestFixture]
public class XRFlightTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // XRPlatform enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRPlatform_AllValuesAreDefined()
    {
        var values = (XRPlatform[])Enum.GetValues(typeof(XRPlatform));
        Assert.GreaterOrEqual(values.Length, 4, "At least 4 platforms required");
        Assert.Contains(XRPlatform.Generic,        values);
        Assert.Contains(XRPlatform.MetaQuest,      values);
        Assert.Contains(XRPlatform.AppleVisionPro, values);
        Assert.Contains(XRPlatform.SteamVR,        values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRComfortLevel enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRComfortLevel_AllValuesAreDefined()
    {
        var values = (XRComfortLevel[])Enum.GetValues(typeof(XRComfortLevel));
        Assert.Contains(XRComfortLevel.Off,    values);
        Assert.Contains(XRComfortLevel.Low,    values);
        Assert.Contains(XRComfortLevel.Medium, values);
        Assert.Contains(XRComfortLevel.High,   values);
        Assert.Contains(XRComfortLevel.Custom, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRHandedness enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRHandedness_HasRightAndLeft()
    {
        var values = (XRHandedness[])Enum.GetValues(typeof(XRHandedness));
        Assert.Contains(XRHandedness.Right, values);
        Assert.Contains(XRHandedness.Left,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRLocomotionType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRLocomotionType_AllValuesAreDefined()
    {
        var values = (XRLocomotionType[])Enum.GetValues(typeof(XRLocomotionType));
        Assert.Contains(XRLocomotionType.Continuous,    values);
        Assert.Contains(XRLocomotionType.Teleport,      values);
        Assert.Contains(XRLocomotionType.SnapTeleport,  values);
        Assert.Contains(XRLocomotionType.Seated,        values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRSessionState enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRSessionState_AllValuesAreDefined()
    {
        var values = (XRSessionState[])Enum.GetValues(typeof(XRSessionState));
        Assert.Contains(XRSessionState.Uninitialized, values);
        Assert.Contains(XRSessionState.Initializing,  values);
        Assert.Contains(XRSessionState.Running,        values);
        Assert.Contains(XRSessionState.Suspended,      values);
        Assert.Contains(XRSessionState.Stopped,        values);
        Assert.Contains(XRSessionState.Error,          values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRFlightPhase enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRFlightPhase_AllValuesAreDefined()
    {
        var values = (VRFlightPhase[])Enum.GetValues(typeof(VRFlightPhase));
        Assert.Contains(VRFlightPhase.Preflight, values);
        Assert.Contains(VRFlightPhase.Takeoff,   values);
        Assert.Contains(VRFlightPhase.Cruise,    values);
        Assert.Contains(VRFlightPhase.Landing,   values);
        Assert.Contains(VRFlightPhase.Debrief,   values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRGestureType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRGestureType_AllValuesAreDefined()
    {
        var values = (XRGestureType[])Enum.GetValues(typeof(XRGestureType));
        Assert.Contains(XRGestureType.None,      values);
        Assert.Contains(XRGestureType.Pinch,     values);
        Assert.Contains(XRGestureType.Grab,      values);
        Assert.Contains(XRGestureType.Point,     values);
        Assert.Contains(XRGestureType.OpenPalm,  values);
        Assert.Contains(XRGestureType.ThumbsUp,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CockpitInteractionType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CockpitInteractionType_AllValuesAreDefined()
    {
        var values = (CockpitInteractionType[])Enum.GetValues(typeof(CockpitInteractionType));
        Assert.Contains(CockpitInteractionType.Grab,  values);
        Assert.Contains(CockpitInteractionType.Push,  values);
        Assert.Contains(CockpitInteractionType.Pull,  values);
        Assert.Contains(CockpitInteractionType.Twist, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRPhotoFormat enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRPhotoFormat_AllValuesAreDefined()
    {
        var values = (VRPhotoFormat[])Enum.GetValues(typeof(VRPhotoFormat));
        Assert.Contains(VRPhotoFormat.Flat,             values);
        Assert.Contains(VRPhotoFormat.Panorama360,      values);
        Assert.Contains(VRPhotoFormat.StereoSideBySide, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRTrackingMode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRTrackingMode_HasSeatedAndStanding()
    {
        var values = (VRTrackingMode[])Enum.GetValues(typeof(VRTrackingMode));
        Assert.Contains(VRTrackingMode.Seated,   values);
        Assert.Contains(VRTrackingMode.Standing, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRFlightConfig (ScriptableObject)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRFlightConfig_DefaultValues_AreReasonable()
    {
        var cfg = ScriptableObject.CreateInstance<XRFlightConfig>();

        Assert.AreEqual(XRComfortLevel.Medium,      cfg.defaultComfortLevel);
        Assert.Greater(cfg.maxVignetteIntensity,    0f,  "vignette intensity > 0");
        Assert.LessOrEqual(cfg.maxVignetteIntensity, 1f, "vignette intensity <= 1");
        Assert.Greater(cfg.snapTurnAngle,           0f,  "snap turn angle > 0");
        Assert.Greater(cfg.renderScale,             0f,  "render scale > 0");
        Assert.Greater(cfg.pinchThreshold,          0f,  "pinch threshold > 0");
        Assert.Greater(cfg.grabThreshold,           0f,  "grab threshold > 0");
        Assert.Greater(cfg.maxTeleportDistance,     0f,  "max teleport distance > 0");
        Assert.Greater(cfg.defaultIpd,              0f,  "IPD > 0");
        Assert.Greater(cfg.hudProjectionDistance,   0f,  "HUD projection distance > 0");

        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void XRFlightConfig_DefaultComfortLevel_IsMedium()
    {
        var cfg = ScriptableObject.CreateInstance<XRFlightConfig>();
        Assert.AreEqual(XRComfortLevel.Medium, cfg.defaultComfortLevel);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void XRFlightConfig_DefaultLocomotionType_IsSeated()
    {
        var cfg = ScriptableObject.CreateInstance<XRFlightConfig>();
        Assert.AreEqual(XRLocomotionType.Seated, cfg.defaultLocomotionType);
        ScriptableObject.DestroyImmediate(cfg);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRHandState data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRHandState_DefaultValues_AreCorrect()
    {
        var state = new XRHandState { Hand = XRHandedness.Right };
        Assert.AreEqual(XRHandedness.Right,   state.Hand);
        Assert.AreEqual(XRGestureType.None,   state.ActiveGesture);
        Assert.IsFalse(state.IsTracked);
        Assert.AreEqual(0f, state.PinchStrength);
        Assert.AreEqual(0f, state.GrabStrength);
    }

    [Test]
    public void XRHandState_FingerTips_HasFiveElements_WhenInitialised()
    {
        var state = new XRHandState { FingerTips = new Vector3[5] };
        Assert.AreEqual(5, state.FingerTips.Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRHandCalibrationData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRHandCalibrationData_DefaultValues_AreReasonable()
    {
        var data = new XRHandCalibrationData();
        Assert.AreEqual(XRHandedness.Right, data.DominantHand);
        Assert.Greater(data.PalmWidthMetres,    0f);
        Assert.Greater(data.FingerLengthMetres, 0f);
        Assert.AreEqual(1f, data.GestureSensitivity);
        Assert.IsFalse(data.IsCalibrated);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRAnalyticsEvent
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRAnalyticsEvent_CanBeCreatedAndPopulated()
    {
        var evt = new XRAnalyticsEvent
        {
            SessionId              = "test-session-1",
            Platform               = XRPlatform.MetaQuest,
            ComfortLevel           = XRComfortLevel.High,
            SessionDurationSeconds = 120f,
            GesturesRecognised     = 42,
            Timestamp              = DateTime.UtcNow.ToString("o")
        };

        Assert.AreEqual("test-session-1",       evt.SessionId);
        Assert.AreEqual(XRPlatform.MetaQuest,   evt.Platform);
        Assert.AreEqual(XRComfortLevel.High,    evt.ComfortLevel);
        Assert.AreEqual(120f,                   evt.SessionDurationSeconds);
        Assert.AreEqual(42,                     evt.GesturesRecognised);
        Assert.IsNotNull(evt.Timestamp);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenericXRAdapter
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GenericXRAdapter_Platform_IsGeneric()
    {
        var adapter = new GenericXRAdapter();
        Assert.AreEqual(XRPlatform.Generic, adapter.Platform);
    }

    [Test]
    public void GenericXRAdapter_Initialise_SetsIsActive()
    {
        var adapter = new GenericXRAdapter();
        Assert.IsFalse(adapter.IsActive);
        adapter.Initialise(null);
        Assert.IsTrue(adapter.IsActive);
    }

    [Test]
    public void GenericXRAdapter_Shutdown_ClearsIsActive()
    {
        var adapter = new GenericXRAdapter();
        adapter.Initialise(null);
        adapter.Shutdown();
        Assert.IsFalse(adapter.IsActive);
    }

    [Test]
    public void GenericXRAdapter_GetHandStates_ReturnNonNull()
    {
        var adapter = new GenericXRAdapter();
        adapter.Initialise(null);

        Assert.IsNotNull(adapter.GetLeftHandState());
        Assert.IsNotNull(adapter.GetRightHandState());
    }

    [Test]
    public void GenericXRAdapter_LeftHand_HandednessIsLeft()
    {
        var adapter = new GenericXRAdapter();
        adapter.Initialise(null);
        Assert.AreEqual(XRHandedness.Left, adapter.GetLeftHandState().Hand);
    }

    [Test]
    public void GenericXRAdapter_RightHand_HandednessIsRight()
    {
        var adapter = new GenericXRAdapter();
        adapter.Initialise(null);
        Assert.AreEqual(XRHandedness.Right, adapter.GetRightHandState().Hand);
    }

    [Test]
    public void GenericXRAdapter_RecenterView_DoesNotThrow()
    {
        var adapter = new GenericXRAdapter();
        adapter.Initialise(null);
        Assert.DoesNotThrow(() => adapter.RecenterView());
    }

    [Test]
    public void GenericXRAdapter_Tick_DoesNotThrow()
    {
        var adapter = new GenericXRAdapter();
        adapter.Initialise(null);
        Assert.DoesNotThrow(() => adapter.Tick(0.016f));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IXRPlatformAdapter interface contract
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void IXRPlatformAdapter_GenericAdapter_ImplementsInterface()
    {
        IXRPlatformAdapter adapter = new GenericXRAdapter();
        Assert.IsNotNull(adapter);
        Assert.AreEqual(XRPlatform.Generic, adapter.Platform);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRCockpitLayout (ScriptableObject)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRCockpitLayout_DefaultAircraftType_IsGeneralAviation()
    {
        var layout = ScriptableObject.CreateInstance<VRCockpitLayout>();
        Assert.AreEqual(CockpitAircraftType.GeneralAviation, layout.aircraftType);
        ScriptableObject.DestroyImmediate(layout);
    }

    [Test]
    public void CockpitAircraftType_AllValuesAreDefined()
    {
        var values = (CockpitAircraftType[])Enum.GetValues(typeof(CockpitAircraftType));
        Assert.Contains(CockpitAircraftType.GeneralAviation,   values);
        Assert.Contains(CockpitAircraftType.CommercialAirliner, values);
        Assert.Contains(CockpitAircraftType.FighterJet,        values);
        Assert.Contains(CockpitAircraftType.Helicopter,        values);
        Assert.Contains(CockpitAircraftType.Spacecraft,        values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRCockpitInteraction — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRCockpitInteraction_DefaultNormalizedValue_IsZero()
    {
        var go = new GameObject("CockpitControl");
        var interaction = go.AddComponent<VRCockpitInteraction>();
        Assert.AreEqual(0f, interaction.NormalizedValue);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCockpitInteraction_SetNormalizedValue_ClampsToZeroOne()
    {
        var go = new GameObject("CockpitControl");
        var interaction = go.AddComponent<VRCockpitInteraction>();

        interaction.SetNormalizedValue(1.5f);
        Assert.AreEqual(1f, interaction.NormalizedValue, 0.001f);

        interaction.SetNormalizedValue(-0.5f);
        Assert.AreEqual(0f, interaction.NormalizedValue, 0.001f);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCockpitInteraction_SetNormalizedValue_FiresOnValueChanged()
    {
        var go = new GameObject("CockpitControl");
        var interaction = go.AddComponent<VRCockpitInteraction>();

        bool fired = false;
        interaction.OnValueChanged += (id, val) => fired = true;
        interaction.SetNormalizedValue(0.5f);

        Assert.IsTrue(fired);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCockpitInteraction_BeginGrab_SetsIsGrabbed()
    {
        var go = new GameObject("CockpitControl");
        var interaction = go.AddComponent<VRCockpitInteraction>();
        interaction.BeginGrab(Vector3.zero);
        Assert.IsTrue(interaction.IsGrabbed);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCockpitInteraction_EndGrab_ClearsIsGrabbed()
    {
        var go = new GameObject("CockpitControl");
        var interaction = go.AddComponent<VRCockpitInteraction>();
        interaction.BeginGrab(Vector3.zero);
        interaction.EndGrab();
        Assert.IsFalse(interaction.IsGrabbed);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCockpitInteraction_IsWithinGrabRange_ReturnsTrueWhenClose()
    {
        var go = new GameObject("CockpitControl");
        go.transform.position = Vector3.zero;
        var interaction = go.AddComponent<VRCockpitInteraction>();
        Assert.IsTrue(interaction.IsWithinGrabRange(new Vector3(0.01f, 0f, 0f)));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCockpitInteraction_IsWithinGrabRange_ReturnsFalseWhenFar()
    {
        var go = new GameObject("CockpitControl");
        go.transform.position = Vector3.zero;
        var interaction = go.AddComponent<VRCockpitInteraction>();
        Assert.IsFalse(interaction.IsWithinGrabRange(new Vector3(10f, 0f, 0f)));
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRComfortSystem — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRComfortSystem_DefaultComfortLevel_IsMedium()
    {
        var go = new GameObject("ComfortSystem");
        var comfort = go.AddComponent<VRComfortSystem>();
        Assert.AreEqual(XRComfortLevel.Medium, comfort.ComfortLevel);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRComfortSystem_SetComfortLevel_UpdatesLevel()
    {
        var go = new GameObject("ComfortSystem");
        var comfort = go.AddComponent<VRComfortSystem>();
        comfort.SetComfortLevel(XRComfortLevel.High);
        Assert.AreEqual(XRComfortLevel.High, comfort.ComfortLevel);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRComfortSystem_SetComfortLevel_Off_DisablesVignette()
    {
        var go = new GameObject("ComfortSystem");
        var comfort = go.AddComponent<VRComfortSystem>();
        comfort.SetComfortLevel(XRComfortLevel.Off);
        comfort.UpdateVignetteForRotationSpeed(200f);
        Assert.AreEqual(0f, comfort.CurrentVignetteIntensity, 0.001f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRComfortSystem_SnapTurn_FiresEvent()
    {
        var go = new GameObject("ComfortSystem");
        var comfort = go.AddComponent<VRComfortSystem>();
        comfort.SetComfortLevel(XRComfortLevel.High); // enables snap turning

        float snapAngle = 0f;
        comfort.OnSnapTurn += angle => snapAngle = angle;
        comfort.SnapTurn(true);

        Assert.Greater(snapAngle, 0f);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HandTrackingCalibration — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HandTrackingCalibration_DefaultCalibration_IsNotCalibrated()
    {
        var go  = new GameObject("Calibration");
        var cal = go.AddComponent<HandTrackingCalibration>();
        Assert.IsFalse(cal.IsCalibrated);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandTrackingCalibration_SetDominantHand_UpdatesCalibrationData()
    {
        var go  = new GameObject("Calibration");
        var cal = go.AddComponent<HandTrackingCalibration>();
        cal.SetDominantHand(XRHandedness.Left);
        Assert.AreEqual(XRHandedness.Left, cal.CalibrationData.DominantHand);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandTrackingCalibration_SetGestureSensitivity_ClampsValue()
    {
        var go  = new GameObject("Calibration");
        var cal = go.AddComponent<HandTrackingCalibration>();
        cal.SetGestureSensitivity(5f);
        Assert.LessOrEqual(cal.CalibrationData.GestureSensitivity, 2f);
        cal.SetGestureSensitivity(-1f);
        Assert.GreaterOrEqual(cal.CalibrationData.GestureSensitivity, 0.5f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandTrackingCalibration_CalibrateFromHandState_MarksCalibrated()
    {
        var go  = new GameObject("Calibration");
        var cal = go.AddComponent<HandTrackingCalibration>();

        var state = new XRHandState
        {
            IsTracked    = true,
            PalmPosition = Vector3.zero,
            FingerTips   = new Vector3[5]
            {
                new Vector3(-0.04f, 0f, 0f), // thumb
                new Vector3(-0.02f, 0.07f, 0f), // index
                new Vector3(0f,     0.08f, 0f),  // middle
                new Vector3(0.02f,  0.07f, 0f),  // ring
                new Vector3(0.04f,  0.06f, 0f)   // pinky
            }
        };
        cal.CalibrateFromHandState(state);
        Assert.IsTrue(cal.IsCalibrated);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandTrackingCalibration_ResetCalibration_ClearsIsCalibrated()
    {
        var go  = new GameObject("Calibration");
        var cal = go.AddComponent<HandTrackingCalibration>();

        var state = new XRHandState
        {
            IsTracked  = true,
            FingerTips = new Vector3[5]
        };
        cal.CalibrateFromHandState(state);
        cal.ResetCalibration();
        Assert.IsFalse(cal.IsCalibrated);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HandGestureRecognizer — ClassifyGesture
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HandGestureRecognizer_ClassifyGesture_ReturnsNone_WhenNotTracked()
    {
        var go  = new GameObject("GestureRecognizer");
        var rec = go.AddComponent<HandGestureRecognizer>();
        var state = new XRHandState { IsTracked = false };
        Assert.AreEqual(XRGestureType.None, rec.ClassifyGesture(state));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandGestureRecognizer_ClassifyGesture_ReturnsGrab_WhenGrabStrengthHigh()
    {
        var go  = new GameObject("GestureRecognizer");
        var rec = go.AddComponent<HandGestureRecognizer>();
        var state = new XRHandState { IsTracked = true, GrabStrength = 0.9f };
        Assert.AreEqual(XRGestureType.Grab, rec.ClassifyGesture(state));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandGestureRecognizer_ClassifyGesture_ReturnsPinch_WhenPinchStrengthHigh()
    {
        var go  = new GameObject("GestureRecognizer");
        var rec = go.AddComponent<HandGestureRecognizer>();
        var state = new XRHandState { IsTracked = true, PinchStrength = 0.85f, GrabStrength = 0f };
        Assert.AreEqual(XRGestureType.Pinch, rec.ClassifyGesture(state));
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRRecenterController — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRRecenterController_DefaultTrackingMode_IsSeated()
    {
        var go  = new GameObject("Recenter");
        var rec = go.AddComponent<VRRecenterController>();
        Assert.AreEqual(VRTrackingMode.Seated, rec.TrackingMode);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRRecenterController_SetTrackingMode_UpdatesMode()
    {
        var go  = new GameObject("Recenter");
        var rec = go.AddComponent<VRRecenterController>();
        rec.SetTrackingMode(VRTrackingMode.Standing);
        Assert.AreEqual(VRTrackingMode.Standing, rec.TrackingMode);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRRecenterController_ToggleTrackingMode_SwitchesMode()
    {
        var go  = new GameObject("Recenter");
        var rec = go.AddComponent<VRRecenterController>();
        Assert.AreEqual(VRTrackingMode.Seated, rec.TrackingMode);
        rec.ToggleTrackingMode();
        Assert.AreEqual(VRTrackingMode.Standing, rec.TrackingMode);
        rec.ToggleTrackingMode();
        Assert.AreEqual(VRTrackingMode.Seated, rec.TrackingMode);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRRecenterController_OnRecentered_EventFires()
    {
        var go  = new GameObject("Recenter");
        var rec = go.AddComponent<VRRecenterController>();
        bool fired = false;
        rec.OnRecentered += () => fired = true;
        rec.Recenter();
        Assert.IsTrue(fired);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRCameraRig — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRCameraRig_SetIPD_ClampsToValidRange()
    {
        var go  = new GameObject("CameraRig");
        var rig = go.AddComponent<VRCameraRig>();
        rig.SetIPD(0.02f); // below minimum
        Assert.GreaterOrEqual(rig.IPD, 0.05f);
        rig.SetIPD(0.12f); // above maximum
        Assert.LessOrEqual(rig.IPD, 0.08f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRCameraRig_SetFOV_ClampsToValidRange()
    {
        var go  = new GameObject("CameraRig");
        var rig = go.AddComponent<VRCameraRig>();
        rig.SetFOV(30f); // below minimum
        Assert.GreaterOrEqual(rig.FOV, 60f);
        rig.SetFOV(200f); // above maximum
        Assert.LessOrEqual(rig.FOV, 120f);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XRAnalytics (static)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void XRAnalytics_BeginSession_SetsSessionId()
    {
        XRAnalytics.BeginSession(XRPlatform.Generic);
        Assert.IsNotNull(XRAnalytics.SessionId);
        Assert.IsNotEmpty(XRAnalytics.SessionId);
    }

    [Test]
    public void XRAnalytics_TrackGesture_IncreasesCount()
    {
        XRAnalytics.BeginSession(XRPlatform.Generic);
        int before = XRAnalytics.SessionGestureCount;
        XRAnalytics.TrackGesture(XRHandedness.Right, XRGestureType.Pinch);
        Assert.AreEqual(before + 1, XRAnalytics.SessionGestureCount);
    }

    [Test]
    public void XRAnalytics_EndSession_ReturnsValidEvent()
    {
        XRAnalytics.BeginSession(XRPlatform.MetaQuest);
        XRAnalyticsEvent evt = XRAnalytics.EndSession(XRPlatform.MetaQuest, XRComfortLevel.High);
        Assert.IsNotNull(evt);
        Assert.IsNotEmpty(evt.SessionId);
        Assert.AreEqual(XRPlatform.MetaQuest, evt.Platform);
        Assert.AreEqual(XRComfortLevel.High,  evt.ComfortLevel);
        Assert.GreaterOrEqual(evt.SessionDurationSeconds, 0f);
    }

    [Test]
    public void XRAnalytics_TrackComfortLevelChange_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => XRAnalytics.TrackComfortLevelChange(XRComfortLevel.Medium));
    }

    [Test]
    public void XRAnalytics_TrackTeleport_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => XRAnalytics.TrackTeleport(Vector3.up));
    }

    [Test]
    public void XRAnalytics_TrackPhotoCaptured_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => XRAnalytics.TrackPhotoCaptured(VRPhotoFormat.Panorama360));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRPhotoMode — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRPhotoMode_DefaultState_IsNotActive()
    {
        var go   = new GameObject("PhotoMode");
        var mode = go.AddComponent<VRPhotoMode>();
        Assert.IsFalse(mode.IsActive);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRPhotoMode_SetActive_ChangesState()
    {
        var go   = new GameObject("PhotoMode");
        var mode = go.AddComponent<VRPhotoMode>();
        mode.SetPhotoModeActive(true);
        Assert.IsTrue(mode.IsActive);
        mode.SetPhotoModeActive(false);
        Assert.IsFalse(mode.IsActive);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRPhotoMode_CapturePhoto_IncrementsCaptureCount()
    {
        var go   = new GameObject("PhotoMode");
        var mode = go.AddComponent<VRPhotoMode>();
        mode.SetPhotoModeActive(true);
        int before = mode.CaptureCount;
        mode.CapturePhoto(VRPhotoFormat.Flat);
        Assert.AreEqual(before + 1, mode.CaptureCount);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRPhotoMode_CapturePhoto_WhenInactive_DoesNotIncrement()
    {
        var go   = new GameObject("PhotoMode");
        var mode = go.AddComponent<VRPhotoMode>();
        int before = mode.CaptureCount;
        mode.CapturePhoto(VRPhotoFormat.Flat); // inactive
        Assert.AreEqual(before, mode.CaptureCount);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRInstrumentPanel — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRInstrumentPanel_UpdateInstruments_StoresValues()
    {
        var go    = new GameObject("InstrumentPanel");
        var panel = go.AddComponent<VRInstrumentPanel>();
        panel.UpdateInstruments(5000f, 250f, 180f, 3f, -10f);

        Assert.AreEqual(5000f, panel.AltitudeFt,    0.01f);
        Assert.AreEqual(250f,  panel.AirspeedKnots, 0.01f);
        Assert.AreEqual(180f,  panel.HeadingDeg,    0.01f);
        Assert.AreEqual(3f,    panel.PitchDeg,      0.01f);
        Assert.AreEqual(-10f,  panel.BankDeg,       0.01f);

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRSpatialAudio — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRSpatialAudio_SetEngineThrottleLevel_ClampsValue()
    {
        var go    = new GameObject("SpatialAudio");
        var audio = go.AddComponent<VRSpatialAudio>();
        audio.SetEngineThrottleLevel(1.5f);
        Assert.AreEqual(1f, audio.EngineThrottleLevel, 0.001f);
        audio.SetEngineThrottleLevel(-0.2f);
        Assert.AreEqual(0f, audio.EngineThrottleLevel, 0.001f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRSpatialAudio_SetWindSpeed_ClampsValue()
    {
        var go    = new GameObject("SpatialAudio");
        var audio = go.AddComponent<VRSpatialAudio>();
        audio.SetWindSpeed(2f);
        Assert.AreEqual(1f, audio.WindSpeedFactor, 0.001f);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRWeatherEffects — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRWeatherEffects_SetTurbulence_ClampsValue()
    {
        var go      = new GameObject("WeatherEffects");
        var weather = go.AddComponent<VRWeatherEffects>();
        weather.SetTurbulence(1.5f);
        Assert.AreEqual(1f, weather.TurbulenceIntensity, 0.001f);
        weather.SetTurbulence(-0.5f);
        Assert.AreEqual(0f, weather.TurbulenceIntensity, 0.001f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRWeatherEffects_SetRain_ClampsValue()
    {
        var go      = new GameObject("WeatherEffects");
        var weather = go.AddComponent<VRWeatherEffects>();
        weather.SetRain(2f);
        Assert.AreEqual(1f, weather.RainIntensity, 0.001f);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRUI — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRUI_SetVisible_UpdatesIsVisible()
    {
        var go = new GameObject("VRUI");
        var ui = go.AddComponent<VRUI>();
        ui.SetVisible(true);
        Assert.IsTrue(ui.IsVisible);
        ui.SetVisible(false);
        Assert.IsFalse(ui.IsVisible);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRUI_SetFollowMode_UpdatesMode()
    {
        var go = new GameObject("VRUI");
        var ui = go.AddComponent<VRUI>();
        ui.SetFollowMode(VRUIFollowMode.Fixed);
        // No exception = pass.
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRMenuController — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRMenuController_AddAndClearItems_Works()
    {
        var go   = new GameObject("MenuController");
        var menu = go.AddComponent<VRMenuController>();

        menu.AddMenuItem(new VRRadialMenuItem { Label = "Recenter" });
        menu.AddMenuItem(new VRRadialMenuItem { Label = "Settings" });

        Assert.IsFalse(menu.IsOpen);  // not open yet

        menu.ClearMenuItems();
        // No exception = pass.
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRMenuController_OpenMenu_SetsIsOpen()
    {
        var go   = new GameObject("MenuController");
        var menu = go.AddComponent<VRMenuController>();
        menu.OpenMenu(Vector3.zero);
        Assert.IsTrue(menu.IsOpen);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRMenuController_CloseMenu_ClearsIsOpen()
    {
        var go   = new GameObject("MenuController");
        var menu = go.AddComponent<VRMenuController>();
        menu.OpenMenu(Vector3.zero);
        menu.CloseMenu();
        Assert.IsFalse(menu.IsOpen);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRMenuController_SelectItem_FiresOnItemSelected()
    {
        var go   = new GameObject("MenuController");
        var menu = go.AddComponent<VRMenuController>();
        menu.AddMenuItem(new VRRadialMenuItem { Label = "Recenter" });
        menu.OpenMenu(Vector3.zero);

        string selected = null;
        menu.OnItemSelected += label => selected = label;
        menu.SelectItem(0);

        Assert.AreEqual("Recenter", selected);
        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VRFlightExperience — MonoBehaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VRFlightExperience_DefaultPhase_IsPreflight()
    {
        var go  = new GameObject("FlightExperience");
        var exp = go.AddComponent<VRFlightExperience>();
        Assert.AreEqual(VRFlightPhase.Preflight, exp.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRFlightExperience_TransitionToPhase_ChangesCurrentPhase()
    {
        var go  = new GameObject("FlightExperience");
        var exp = go.AddComponent<VRFlightExperience>();
        exp.TransitionToPhase(VRFlightPhase.Cruise);
        Assert.AreEqual(VRFlightPhase.Cruise, exp.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRFlightExperience_AdvancePhase_MovesThroughSequence()
    {
        var go  = new GameObject("FlightExperience");
        var exp = go.AddComponent<VRFlightExperience>();
        exp.TransitionToPhase(VRFlightPhase.Preflight);
        exp.AdvancePhase();
        Assert.AreEqual(VRFlightPhase.Takeoff, exp.CurrentPhase);
        exp.AdvancePhase();
        Assert.AreEqual(VRFlightPhase.Cruise, exp.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VRFlightExperience_OnPhaseChanged_EventFires()
    {
        var go  = new GameObject("FlightExperience");
        var exp = go.AddComponent<VRFlightExperience>();

        VRFlightPhase fired = VRFlightPhase.Preflight;
        exp.OnPhaseChanged += p => fired = p;
        exp.TransitionToPhase(VRFlightPhase.Landing);
        Assert.AreEqual(VRFlightPhase.Landing, fired);

        Object.DestroyImmediate(go);
    }
}
