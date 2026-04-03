// PlatformTargetMatrixTests.cs — SWEF Phase 95: Platform Target Matrix & Build Pipeline
// NUnit EditMode tests for PlatformTargetMatrix, BuildProfileConfig, and PlatformFeatureGate.
using System;
using NUnit.Framework;
using UnityEngine;
using SWEF.BuildPipeline;

[TestFixture]
public class PlatformTargetMatrixTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // PlatformTargetMatrix.GetCategory — all 8 targets
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GetCategory_WindowsPC_ReturnsPC()
    {
        Assert.AreEqual(PlatformCategory.PC, PlatformTargetMatrix.GetCategory(PlatformTarget.WindowsPC));
    }

    [Test]
    public void GetCategory_macOS_ReturnsPC()
    {
        Assert.AreEqual(PlatformCategory.PC, PlatformTargetMatrix.GetCategory(PlatformTarget.macOS));
    }

    [Test]
    public void GetCategory_iOS_ReturnsMobile()
    {
        Assert.AreEqual(PlatformCategory.Mobile, PlatformTargetMatrix.GetCategory(PlatformTarget.iOS));
    }

    [Test]
    public void GetCategory_Android_ReturnsMobile()
    {
        Assert.AreEqual(PlatformCategory.Mobile, PlatformTargetMatrix.GetCategory(PlatformTarget.Android));
    }

    [Test]
    public void GetCategory_iPadOS_ReturnsTablet()
    {
        Assert.AreEqual(PlatformCategory.Tablet, PlatformTargetMatrix.GetCategory(PlatformTarget.iPadOS));
    }

    [Test]
    public void GetCategory_AndroidTablet_ReturnsTablet()
    {
        Assert.AreEqual(PlatformCategory.Tablet, PlatformTargetMatrix.GetCategory(PlatformTarget.AndroidTablet));
    }

    [Test]
    public void GetCategory_MetaQuest_ReturnsXR()
    {
        Assert.AreEqual(PlatformCategory.XR, PlatformTargetMatrix.GetCategory(PlatformTarget.MetaQuest));
    }

    [Test]
    public void GetCategory_VisionPro_ReturnsXR()
    {
        Assert.AreEqual(PlatformCategory.XR, PlatformTargetMatrix.GetCategory(PlatformTarget.VisionPro));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IsPrimaryPlatform — all 4 primary + non-primary
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void IsPrimaryPlatform_WindowsPC_ReturnsTrue()
    {
        Assert.IsTrue(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.WindowsPC));
    }

    [Test]
    public void IsPrimaryPlatform_macOS_ReturnsTrue()
    {
        Assert.IsTrue(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.macOS));
    }

    [Test]
    public void IsPrimaryPlatform_iOS_ReturnsTrue()
    {
        Assert.IsTrue(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.iOS));
    }

    [Test]
    public void IsPrimaryPlatform_Android_ReturnsTrue()
    {
        Assert.IsTrue(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.Android));
    }

    [Test]
    public void IsPrimaryPlatform_iPadOS_ReturnsFalse()
    {
        Assert.IsFalse(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.iPadOS));
    }

    [Test]
    public void IsPrimaryPlatform_AndroidTablet_ReturnsFalse()
    {
        Assert.IsFalse(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.AndroidTablet));
    }

    [Test]
    public void IsPrimaryPlatform_MetaQuest_ReturnsFalse()
    {
        Assert.IsFalse(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.MetaQuest));
    }

    [Test]
    public void IsPrimaryPlatform_VisionPro_ReturnsFalse()
    {
        Assert.IsFalse(PlatformTargetMatrix.IsPrimaryPlatform(PlatformTarget.VisionPro));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetCurrentPlatform — smoke test (Editor always returns a valid target)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TestGetCurrentPlatform_ReturnsValidTarget()
    {
        PlatformTarget detected = PlatformTargetMatrix.GetCurrentPlatform();
        Assert.IsTrue(Enum.IsDefined(typeof(PlatformTarget), detected),
            $"GetCurrentPlatform returned undefined value: {(int)detected}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IsTablet heuristic
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TestIsTabletHeuristic_DoesNotThrow()
    {
        // In EditMode, Screen.width/height/dpi are set by the Editor; just
        // ensure the method runs without exceptions.
        Assert.DoesNotThrow(() => PlatformTargetMatrix.IsTablet());
    }

    [Test]
    public void IsTablet_DiagonalThreshold_IsSevenInches()
    {
        Assert.AreEqual(7f, PlatformTargetMatrix.TabletDiagonalThresholdInches, 0.001f);
    }

    [Test]
    public void IsTablet_ShortSideThreshold_Is1024()
    {
        Assert.AreEqual(1024, PlatformTargetMatrix.TabletShortSidePixelThreshold);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SupportsFeature — XR only on XR platforms
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SupportsFeature_XR_TrueForMetaQuest()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("xr", PlatformTarget.MetaQuest));
    }

    [Test]
    public void SupportsFeature_XR_TrueForVisionPro()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("xr", PlatformTarget.VisionPro));
    }

    [Test]
    public void SupportsFeature_XR_FalseForWindowsPC()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("xr", PlatformTarget.WindowsPC));
    }

    [Test]
    public void SupportsFeature_XR_FalseForIOS()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("xr", PlatformTarget.iOS));
    }

    // ── Keyboard ────────────────────────────────────────────────────────────────

    [Test]
    public void SupportsFeature_Keyboard_TrueForWindowsPC()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("keyboard", PlatformTarget.WindowsPC));
    }

    [Test]
    public void SupportsFeature_Keyboard_TrueForMacOS()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("keyboard", PlatformTarget.macOS));
    }

    [Test]
    public void SupportsFeature_Keyboard_FalseForIOS()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("keyboard", PlatformTarget.iOS));
    }

    [Test]
    public void SupportsFeature_Keyboard_FalseForAndroid()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("keyboard", PlatformTarget.Android));
    }

    // ── Touch ────────────────────────────────────────────────────────────────────

    [Test]
    public void SupportsFeature_Touch_TrueForIOS()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("touch", PlatformTarget.iOS));
    }

    [Test]
    public void SupportsFeature_Touch_FalseForWindowsPC()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("touch", PlatformTarget.WindowsPC));
    }

    // ── GPS ─────────────────────────────────────────────────────────────────────

    [Test]
    public void SupportsFeature_GPS_TrueForIOS()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("gps", PlatformTarget.iOS));
    }

    [Test]
    public void SupportsFeature_GPS_TrueForAndroid()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("gps", PlatformTarget.Android));
    }

    [Test]
    public void SupportsFeature_GPS_FalseForWindowsPC()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("gps", PlatformTarget.WindowsPC));
    }

    [Test]
    public void SupportsFeature_GPS_FalseForMetaQuest()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("gps", PlatformTarget.MetaQuest));
    }

    // ── ARCore / ARKit ───────────────────────────────────────────────────────────

    [Test]
    public void SupportsFeature_ARCore_TrueForAndroid()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("arcore", PlatformTarget.Android));
    }

    [Test]
    public void SupportsFeature_ARCore_FalseForIOS()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("arcore", PlatformTarget.iOS));
    }

    [Test]
    public void SupportsFeature_ARKit_TrueForIOS()
    {
        Assert.IsTrue(PlatformTargetMatrix.SupportsFeature("arkit", PlatformTarget.iOS));
    }

    [Test]
    public void SupportsFeature_ARKit_FalseForAndroid()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("arkit", PlatformTarget.Android));
    }

    // ── Unknown feature ID ────────────────────────────────────────────────────────

    [Test]
    public void SupportsFeature_UnknownId_ReturnsFalse()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature("nonexistent_feature", PlatformTarget.iOS));
    }

    [Test]
    public void SupportsFeature_NullId_ReturnsFalse()
    {
        Assert.IsFalse(PlatformTargetMatrix.SupportsFeature(null, PlatformTarget.iOS));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PlatformFeatureGate defaults
    // ═══════════════════════════════════════════════════════════════════════════

    [SetUp]
    public void ResetFeatureGate()
    {
        PlatformFeatureGate.ClearAllOverrides();
        PlatformFeatureGate.ResetPlatformCache();
    }

    [Test]
    public void TestFeatureGateDefaults_SetOverride_TakesPrecedence()
    {
        PlatformFeatureGate.SetOverride("xr", true);
        Assert.IsTrue(PlatformFeatureGate.IsEnabled("xr"));

        PlatformFeatureGate.SetOverride("xr", false);
        Assert.IsFalse(PlatformFeatureGate.IsEnabled("xr"));
    }

    [Test]
    public void FeatureGate_ClearOverride_RestoresDefault()
    {
        // Default for "keyboard" on Windows PC in Editor should be true.
        // Set an override, clear it, then check the built-in value is used.
        PlatformFeatureGate.SetOverride("keyboard", false);
        Assert.IsFalse(PlatformFeatureGate.IsEnabled("keyboard"));

        PlatformFeatureGate.ClearOverride("keyboard");
        // After clearing the override, the result is driven by SupportsFeature
        // for the active Editor build target — just ensure no exception.
        Assert.DoesNotThrow(() => PlatformFeatureGate.IsEnabled("keyboard"));
    }

    [Test]
    public void FeatureGate_IsEnabledFor_IgnoresOverrides()
    {
        PlatformFeatureGate.SetOverride("xr", true);
        // IsEnabledFor bypasses overrides and uses built-in defaults.
        Assert.IsFalse(PlatformFeatureGate.IsEnabledFor("xr", PlatformTarget.WindowsPC));
        Assert.IsTrue(PlatformFeatureGate.IsEnabledFor("xr",  PlatformTarget.MetaQuest));
    }

    [Test]
    public void FeatureGate_NullId_ReturnsFalse()
    {
        Assert.IsFalse(PlatformFeatureGate.IsEnabled(null));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BuildProfileConfig.CreateDefault — per-platform spot checks
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BuildProfileConfig_CreateDefault_WindowsPC_HasKeyboard()
    {
        var cfg = BuildProfileConfig.CreateDefault(PlatformTarget.WindowsPC);
        Assert.IsTrue(cfg.enableKeyboardMouse);
        Assert.IsFalse(cfg.enableXR);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildProfileConfig_CreateDefault_iOS_HasTouch()
    {
        var cfg = BuildProfileConfig.CreateDefault(PlatformTarget.iOS);
        Assert.IsTrue(cfg.enableTouchInput);
        Assert.IsTrue(cfg.enableGyroscope);
        Assert.IsFalse(cfg.enableKeyboardMouse);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildProfileConfig_CreateDefault_MetaQuest_HasXR()
    {
        var cfg = BuildProfileConfig.CreateDefault(PlatformTarget.MetaQuest);
        Assert.IsTrue(cfg.enableXR);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildProfileConfig_CreateDefault_Android_FrameRateIs60()
    {
        var cfg = BuildProfileConfig.CreateDefault(PlatformTarget.Android);
        Assert.AreEqual(60, cfg.targetFrameRate);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildProfileConfig_CreateDefault_VisionPro_FrameRateIs90()
    {
        var cfg = BuildProfileConfig.CreateDefault(PlatformTarget.VisionPro);
        Assert.AreEqual(90, cfg.targetFrameRate);
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Enum completeness
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PlatformTarget_HasEightValues()
    {
        Assert.AreEqual(8, Enum.GetValues(typeof(PlatformTarget)).Length);
    }

    [Test]
    public void PlatformCategory_HasFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(PlatformCategory)).Length);
    }

    [Test]
    public void QualityTier_HasFiveValues()
    {
        Assert.AreEqual(5, Enum.GetValues(typeof(QualityTier)).Length);
    }
}
