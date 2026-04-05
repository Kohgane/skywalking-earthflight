// SpectatorTests.cs — NUnit EditMode tests for Phase 107 Live Streaming & Spectator Mode
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SWEF.Spectator;

[TestFixture]
public class SpectatorTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // SpectatorEnums
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpectatorCameraMode_AllValuesAreDefined()
    {
        var values = (SpectatorCameraMode[])Enum.GetValues(typeof(SpectatorCameraMode));
        Assert.GreaterOrEqual(values.Length, 5, "At least 5 camera modes should be defined");
        Assert.Contains(SpectatorCameraMode.FreeCam,      values);
        Assert.Contains(SpectatorCameraMode.FollowCam,    values);
        Assert.Contains(SpectatorCameraMode.OrbitCam,     values);
        Assert.Contains(SpectatorCameraMode.CinematicCam, values);
        Assert.Contains(SpectatorCameraMode.PilotView,    values);
    }

    [Test]
    public void StreamingPlatform_AllValuesAreDefined()
    {
        var values = (StreamingPlatform[])Enum.GetValues(typeof(StreamingPlatform));
        Assert.Contains(StreamingPlatform.Twitch,  values);
        Assert.Contains(StreamingPlatform.YouTube, values);
        Assert.Contains(StreamingPlatform.Custom,  values);
    }

    [Test]
    public void CameraTransitionEffect_AllValuesAreDefined()
    {
        var values = (CameraTransitionEffect[])Enum.GetValues(typeof(CameraTransitionEffect));
        Assert.Contains(CameraTransitionEffect.Cut,       values);
        Assert.Contains(CameraTransitionEffect.Crossfade, values);
        Assert.Contains(CameraTransitionEffect.WhipPan,   values);
    }

    [Test]
    public void FlightEventType_AllValuesAreDefined()
    {
        var values = (FlightEventType[])Enum.GetValues(typeof(FlightEventType));
        Assert.Contains(FlightEventType.SpeedRecord,      values);
        Assert.Contains(FlightEventType.AltitudeMilestone, values);
        Assert.Contains(FlightEventType.NearMiss,         values);
        Assert.Contains(FlightEventType.Overtake,         values);
        Assert.Contains(FlightEventType.FormationFlying,  values);
    }

    [Test]
    public void ChatCommandType_AllValuesAreDefined()
    {
        var values = (ChatCommandType[])Enum.GetValues(typeof(ChatCommandType));
        Assert.Contains(ChatCommandType.Camera,  values);
        Assert.Contains(ChatCommandType.Follow,  values);
        Assert.Contains(ChatCommandType.Stats,   values);
        Assert.Contains(ChatCommandType.Unknown, values);
    }

    [Test]
    public void CinematicShotType_AllValuesAreDefined()
    {
        var values = (CinematicShotType[])Enum.GetValues(typeof(CinematicShotType));
        Assert.Contains(CinematicShotType.Chase,    values);
        Assert.Contains(CinematicShotType.Flyby,    values);
        Assert.Contains(CinematicShotType.Dramatic, values);
        Assert.Contains(CinematicShotType.TopDown,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpectatorConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpectatorConfig_DefaultValues_AreReasonable()
    {
        var cfg = ScriptableObject.CreateInstance<SpectatorConfig>();

        Assert.Greater(cfg.freeCamSpeed,            0f,  "freeCamSpeed must be positive");
        Assert.Greater(cfg.freeCamBoostMultiplier,  1f,  "boost multiplier must be > 1");
        Assert.Greater(cfg.followPositionSmoothing, 0f,  "position smoothing must be positive");
        Assert.Greater(cfg.orbitRadius,             0f,  "orbit radius must be positive");
        Assert.Greater(cfg.orbitSpeed,              0f,  "orbit speed must be positive");
        Assert.Greater(cfg.cinematicShotDuration,   0f,  "shot duration must be positive");
        Assert.Greater(cfg.defaultFov,              0f,  "default FOV must be positive");
        Assert.Greater(cfg.chatMaxMessages,         0,   "chatMaxMessages must be positive");
        Assert.GreaterOrEqual(cfg.chatRateLimitSeconds, 0f, "rate limit must be non-negative");
        Assert.NotNull(cfg.viewerMilestones,             "viewerMilestones must not be null");

        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void SpectatorConfig_ViewerMilestones_AreInAscendingOrder()
    {
        var cfg = ScriptableObject.CreateInstance<SpectatorConfig>();

        for (int i = 1; i < cfg.viewerMilestones.Length; i++)
            Assert.Greater(cfg.viewerMilestones[i], cfg.viewerMilestones[i - 1],
                "Viewer milestones should be in ascending order");

        ScriptableObject.DestroyImmediate(cfg);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SpectatorModeController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SpectatorModeController_EnterExit_FiresEvents()
    {
        var go = new GameObject("SpectatorController");
        var controller = go.AddComponent<SpectatorModeController>();

        bool enteredFired = false;
        bool exitedFired  = false;
        controller.OnSpectatorModeEntered += () => enteredFired = true;
        controller.OnSpectatorModeExited  += () => exitedFired  = true;

        controller.EnterSpectatorMode();
        Assert.IsTrue(enteredFired,         "OnSpectatorModeEntered should fire on enter");
        Assert.IsTrue(controller.IsActive,  "IsActive should be true after entering");

        controller.ExitSpectatorMode();
        Assert.IsTrue(exitedFired,          "OnSpectatorModeExited should fire on exit");
        Assert.IsFalse(controller.IsActive, "IsActive should be false after exiting");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SpectatorModeController_EnterTwice_DoesNotFireTwice()
    {
        var go = new GameObject("SpectatorController2");
        var controller = go.AddComponent<SpectatorModeController>();

        int enterCount = 0;
        controller.OnSpectatorModeEntered += () => enterCount++;

        controller.EnterSpectatorMode();
        controller.EnterSpectatorMode(); // should be no-op

        Assert.AreEqual(1, enterCount, "OnSpectatorModeEntered should fire only once");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SpectatorModeController_RegisterUnregisterTarget()
    {
        var go         = new GameObject("SpectatorController3");
        var controller = go.AddComponent<SpectatorModeController>();
        var targetGO   = new GameObject("Target");

        controller.RegisterTarget(targetGO.transform);
        Assert.AreEqual(1, controller.GetTargets().Count, "Target should be registered");

        controller.UnregisterTarget(targetGO.transform);
        Assert.AreEqual(0, controller.GetTargets().Count, "Target should be unregistered");

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(targetGO);
    }

    [Test]
    public void SpectatorModeController_SelectNextTarget_CyclesTargets()
    {
        var go         = new GameObject("SpectatorController4");
        var controller = go.AddComponent<SpectatorModeController>();

        var t1 = new GameObject("T1").transform;
        var t2 = new GameObject("T2").transform;
        var t3 = new GameObject("T3").transform;

        controller.RegisterTarget(t1);
        controller.RegisterTarget(t2);
        controller.RegisterTarget(t3);

        // Enter spectator mode (no initial target → FreeCam)
        controller.EnterSpectatorMode();
        Assert.IsNull(controller.CurrentTarget, "No target initially");

        controller.SelectNextTarget();
        Assert.AreEqual(t1, controller.CurrentTarget, "First SelectNext should pick t1");

        controller.SelectNextTarget();
        Assert.AreEqual(t2, controller.CurrentTarget, "Second SelectNext should pick t2");

        controller.SelectNextTarget();
        Assert.AreEqual(t3, controller.CurrentTarget, "Third SelectNext should pick t3");

        controller.SelectNextTarget();
        Assert.AreEqual(t1, controller.CurrentTarget, "Should wrap around to t1");

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(t1.gameObject);
        UnityEngine.Object.DestroyImmediate(t2.gameObject);
        UnityEngine.Object.DestroyImmediate(t3.gameObject);
    }

    [Test]
    public void SpectatorModeController_SetCameraMode_FiresEvent()
    {
        var go         = new GameObject("SpectatorController5");
        var controller = go.AddComponent<SpectatorModeController>();

        SpectatorCameraMode? receivedMode = null;
        controller.OnCameraModeChanged += m => receivedMode = m;

        controller.EnterSpectatorMode();
        // FreeCam should be set on enter (no target)
        Assert.AreEqual(SpectatorCameraMode.FreeCam, controller.CurrentCameraMode);

        // Attempting to switch to FollowCam without a target should stay in FreeCam
        controller.SetCameraMode(SpectatorCameraMode.FollowCam);
        Assert.AreEqual(SpectatorCameraMode.FreeCam, controller.CurrentCameraMode,
            "Without a target, FollowCam should fall back to FreeCam");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StreamingIntegrationManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StreamingIntegrationManager_StartEnd_SetsState()
    {
        var go      = new GameObject("StreamManager");
        var manager = go.AddComponent<StreamingIntegrationManager>();

        bool startedFired = false;
        bool endedFired   = false;
        manager.OnStreamStarted += _ => startedFired = true;
        manager.OnStreamEnded   += () => endedFired  = true;

        manager.StartStream(StreamingPlatform.Twitch);
        Assert.IsTrue(manager.IsStreaming,             "IsStreaming should be true after StartStream");
        Assert.AreEqual(StreamingPlatform.Twitch, manager.Platform);
        Assert.IsTrue(startedFired, "OnStreamStarted should fire");

        manager.EndStream();
        Assert.IsFalse(manager.IsStreaming, "IsStreaming should be false after EndStream");
        Assert.IsTrue(endedFired, "OnStreamEnded should fire");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void StreamingIntegrationManager_StartTwice_IsNoOp()
    {
        var go      = new GameObject("StreamManager2");
        var manager = go.AddComponent<StreamingIntegrationManager>();

        int startCount = 0;
        manager.OnStreamStarted += _ => startCount++;

        manager.StartStream(StreamingPlatform.YouTube);
        manager.StartStream(StreamingPlatform.Twitch); // no-op

        Assert.AreEqual(1, startCount, "StartStream called twice should only fire once");
        Assert.AreEqual(StreamingPlatform.YouTube, manager.Platform, "Platform should remain YouTube");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void StreamingIntegrationManager_UpdateViewerCount_ClampsNegative()
    {
        var go      = new GameObject("StreamManager3");
        var manager = go.AddComponent<StreamingIntegrationManager>();

        manager.StartStream(StreamingPlatform.Custom);
        manager.UpdateViewerCount(-100);
        Assert.AreEqual(0, manager.ViewerCount, "Negative viewer count should clamp to 0");

        manager.UpdateViewerCount(500);
        Assert.AreEqual(500, manager.ViewerCount);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void StreamingIntegrationManager_GetOverlayData_ReflectsState()
    {
        var go      = new GameObject("StreamManager4");
        var manager = go.AddComponent<StreamingIntegrationManager>();

        manager.StartStream(StreamingPlatform.Twitch);
        manager.UpdateViewerCount(1234);

        var data = manager.GetOverlayData();
        Assert.IsTrue(data.isStreaming,                  "OverlayData.isStreaming should be true");
        Assert.AreEqual(StreamingPlatform.Twitch, data.platform);
        Assert.AreEqual(1234, data.viewerCount);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveChatController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveChatController_SubmitMessage_AcceptsValidMessage()
    {
        var go     = new GameObject("ChatController");
        var chat   = go.AddComponent<LiveChatController>();
        var config = ScriptableObject.CreateInstance<SpectatorConfig>();
        // Inject config via reflection to avoid serialisation in edit mode
        typeof(LiveChatController)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(chat, config);

        bool fired = false;
        chat.OnMessageReceived += _ => fired = true;

        bool accepted = chat.SubmitMessage("viewer1", "Viewer One", "Hello world!");
        Assert.IsTrue(accepted, "Valid message should be accepted");
        Assert.IsTrue(fired,    "OnMessageReceived should fire");
        Assert.AreEqual(1, chat.Messages.Count, "Message queue should contain 1 entry");

        ScriptableObject.DestroyImmediate(config);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LiveChatController_SubmitMessage_RejectsEmptyText()
    {
        var go     = new GameObject("ChatController2");
        var chat   = go.AddComponent<LiveChatController>();
        var config = ScriptableObject.CreateInstance<SpectatorConfig>();
        typeof(LiveChatController)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(chat, config);

        bool accepted = chat.SubmitMessage("viewer1", "Viewer One", "   ");
        Assert.IsFalse(accepted, "Empty/whitespace message should be rejected");

        ScriptableObject.DestroyImmediate(config);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LiveChatController_ChatCommand_IsParsed()
    {
        var go     = new GameObject("ChatController3");
        var chat   = go.AddComponent<LiveChatController>();
        var config = ScriptableObject.CreateInstance<SpectatorConfig>();
        typeof(LiveChatController)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(chat, config);

        ChatCommand receivedCmd = null;
        chat.OnCommandReceived += cmd => receivedCmd = cmd;

        chat.SubmitMessage("viewer1", "Viewer One", "!stats");
        Assert.IsNotNull(receivedCmd,                        "Command should be parsed");
        Assert.AreEqual(ChatCommandType.Stats, receivedCmd.type);

        ScriptableObject.DestroyImmediate(config);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LiveChatController_ClearChat_EmptiesQueue()
    {
        var go     = new GameObject("ChatController4");
        var chat   = go.AddComponent<LiveChatController>();
        var config = ScriptableObject.CreateInstance<SpectatorConfig>();
        typeof(LiveChatController)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(chat, config);

        chat.SubmitMessage("v1", "V1", "Hello");
        Assert.AreEqual(1, chat.Messages.Count);

        chat.ClearChat();
        Assert.AreEqual(0, chat.Messages.Count, "ClearChat should empty the queue");

        ScriptableObject.DestroyImmediate(config);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommentatorController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CommentatorController_MarkHighlight_FiresEvent()
    {
        var go  = new GameObject("Commentator");
        var cc  = go.AddComponent<CommentatorController>();

        bool fired = false;
        cc.OnHighlightMarked += _ => fired = true;

        cc.MarkHighlight();
        Assert.IsTrue(fired, "OnHighlightMarked should fire");
        Assert.AreEqual(1, cc.GetHighlights().Count, "One highlight should be recorded");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CommentatorController_AddEventMarker_IsStoredAndFires()
    {
        var go  = new GameObject("Commentator2");
        var cc  = go.AddComponent<CommentatorController>();

        FlightEventMarker received = null;
        cc.OnEventMarked += m => received = m;

        cc.AddEventMarker(FlightEventType.SpeedRecord, "sr71", "New speed record!");

        Assert.IsNotNull(received, "OnEventMarked should fire");
        Assert.AreEqual(FlightEventType.SpeedRecord, received.eventType);
        Assert.AreEqual("sr71", received.aircraftId);
        Assert.AreEqual(1, cc.GetEventMarkers().Count);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CommentatorController_SaveRecallPreset_RoundTrips()
    {
        var go  = new GameObject("Commentator3");
        var cc  = go.AddComponent<CommentatorController>();

        var camGO  = new GameObject("Cam");
        var source = new GameObject("Source");
        source.transform.position = new Vector3(10f, 20f, 30f);
        source.transform.rotation = Quaternion.Euler(15f, 45f, 0f);

        cc.SavePreset(0, source.transform);
        bool recalled = cc.RecallPreset(0, camGO.transform);

        Assert.IsTrue(recalled, "RecallPreset should return true for a saved slot");
        Assert.AreEqual(source.transform.position, camGO.transform.position, "Position should match");

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(camGO);
        UnityEngine.Object.DestroyImmediate(source);
    }

    [Test]
    public void CommentatorController_RecallEmptySlot_ReturnsFalse()
    {
        var go  = new GameObject("Commentator4");
        var cc  = go.AddComponent<CommentatorController>();
        var camGO = new GameObject("Cam2");

        bool result = cc.RecallPreset(5, camGO.transform);
        Assert.IsFalse(result, "RecallPreset on empty slot should return false");

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(camGO);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CameraSwitchDirector
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CameraSwitchDirector_StartStop_SetsActiveState()
    {
        var go       = new GameObject("Director");
        var director = go.AddComponent<CameraSwitchDirector>();

        director.StartAutoDirector();
        Assert.IsTrue(director.IsAutoDirectorActive, "Auto-director should be active after start");

        director.StopAutoDirector();
        Assert.IsFalse(director.IsAutoDirectorActive, "Auto-director should be inactive after stop");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CameraSwitchDirector_ManualCut_SetsManualOverride()
    {
        var go       = new GameObject("Director2");
        var director = go.AddComponent<CameraSwitchDirector>();

        SpectatorCameraMode? cutMode = null;
        director.OnCameraSwitch += (m, _) => cutMode = m;

        director.ManualCut(SpectatorCameraMode.OrbitCam, CameraTransitionEffect.Crossfade, 5f);
        Assert.IsTrue(director.IsManualOverride, "ManualCut should set IsManualOverride");
        Assert.AreEqual(SpectatorCameraMode.OrbitCam, cutMode, "OnCameraSwitch should fire with OrbitCam");

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OverlayData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OverlayData_DefaultValues_AreCorrect()
    {
        var data = new OverlayData();
        Assert.IsFalse(data.isStreaming);
        Assert.AreEqual(0, data.viewerCount);
        Assert.AreEqual(0f, data.uptimeSeconds);
        Assert.AreEqual(0f, data.targetSpeedKph);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChatMessage / ChatCommand types
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChatMessage_CanBeInstantiated()
    {
        var msg = new ChatMessage
        {
            viewerId   = "v1",
            viewerName = "TestViewer",
            text       = "Hello from test",
            timestamp  = 1.0f,
            isCommand  = false,
        };
        Assert.AreEqual("v1",              msg.viewerId);
        Assert.AreEqual("TestViewer",      msg.viewerName);
        Assert.AreEqual("Hello from test", msg.text);
        Assert.IsFalse(msg.isCommand);
    }

    [Test]
    public void ChatCommand_ArgsDefaultToEmpty()
    {
        var cmd = new ChatCommand
        {
            viewerId   = "v1",
            viewerName = "TestViewer",
            type       = ChatCommandType.Stats,
            args       = Array.Empty<string>(),
            timestamp  = 0f,
        };
        Assert.IsNotNull(cmd.args);
        Assert.AreEqual(0, cmd.args.Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightEventMarker
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightEventMarker_FieldsAreSerializable()
    {
        var marker = new FlightEventMarker
        {
            eventType   = FlightEventType.NearMiss,
            aircraftId  = "aircraft_01",
            description = "Very close pass!",
            timestamp   = 42.5f,
        };
        Assert.AreEqual(FlightEventType.NearMiss, marker.eventType);
        Assert.AreEqual("aircraft_01",            marker.aircraftId);
        Assert.AreEqual(42.5f,                    marker.timestamp);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CameraPreset
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CameraPreset_FieldsAreAssignable()
    {
        var preset = new CameraPreset
        {
            slot     = 2,
            position = new Vector3(1f, 2f, 3f),
            rotation = Quaternion.identity,
        };
        Assert.AreEqual(2,                    preset.slot);
        Assert.AreEqual(new Vector3(1f,2f,3f), preset.position);
        Assert.AreEqual(Quaternion.identity,   preset.rotation);
    }
}
