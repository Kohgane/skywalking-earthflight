// LiveryEditorTests.cs — Phase 115: Advanced Aircraft Livery Editor
// Comprehensive NUnit EditMode tests (45+ tests) covering:
// enums, config, layer management, blend modes, decal placement, UV mapping,
// template system, import/export, brush settings, gradient painter, analytics.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.LiveryEditor;

[TestFixture]
public class LiveryEditorTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryLayerType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryLayerType_AllValuesAreDefined()
    {
        var values = (LiveryLayerType[])Enum.GetValues(typeof(LiveryLayerType));
        Assert.GreaterOrEqual(values.Length, 7, "At least 7 layer types required");
        Assert.Contains(LiveryLayerType.BaseColor, values);
        Assert.Contains(LiveryLayerType.Pattern,   values);
        Assert.Contains(LiveryLayerType.Decal,     values);
        Assert.Contains(LiveryLayerType.Text,      values);
        Assert.Contains(LiveryLayerType.Gradient,  values);
        Assert.Contains(LiveryLayerType.Mask,      values);
        Assert.Contains(LiveryLayerType.Effect,    values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BlendMode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BlendMode_AllValuesAreDefined()
    {
        var values = (BlendMode[])Enum.GetValues(typeof(BlendMode));
        Assert.Contains(BlendMode.Normal,   values);
        Assert.Contains(BlendMode.Multiply, values);
        Assert.Contains(BlendMode.Screen,   values);
        Assert.Contains(BlendMode.Overlay,  values);
        Assert.Contains(BlendMode.SoftLight,values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DecalCategory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DecalCategory_AllValuesAreDefined()
    {
        var values = (DecalCategory[])Enum.GetValues(typeof(DecalCategory));
        Assert.Contains(DecalCategory.Airline,  values);
        Assert.Contains(DecalCategory.Military, values);
        Assert.Contains(DecalCategory.Racing,   values);
        Assert.Contains(DecalCategory.National, values);
        Assert.Contains(DecalCategory.Custom,   values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryExportFormat enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryExportFormat_AllValuesAreDefined()
    {
        var values = (LiveryExportFormat[])Enum.GetValues(typeof(LiveryExportFormat));
        Assert.Contains(LiveryExportFormat.PNG,        values);
        Assert.Contains(LiveryExportFormat.JPEG,       values);
        Assert.Contains(LiveryExportFormat.SWEFLivery, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BrushType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BrushType_AllValuesAreDefined()
    {
        var values = (BrushType[])Enum.GetValues(typeof(BrushType));
        Assert.Contains(BrushType.Round,    values);
        Assert.Contains(BrushType.Square,   values);
        Assert.Contains(BrushType.Soft,     values);
        Assert.Contains(BrushType.Airbrush, values);
        Assert.Contains(BrushType.Eraser,   values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MirrorMode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void MirrorMode_AllValuesAreDefined()
    {
        var values = (MirrorMode[])Enum.GetValues(typeof(MirrorMode));
        Assert.Contains(MirrorMode.None,       values);
        Assert.Contains(MirrorMode.Horizontal, values);
        Assert.Contains(MirrorMode.Vertical,   values);
        Assert.Contains(MirrorMode.Both,       values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GradientType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GradientType_AllValuesAreDefined()
    {
        var values = (GradientType[])Enum.GetValues(typeof(GradientType));
        Assert.Contains(GradientType.Linear,    values);
        Assert.Contains(GradientType.Radial,    values);
        Assert.Contains(GradientType.Angular,   values);
        Assert.Contains(GradientType.Reflected, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PatternType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PatternType_AllValuesAreDefined()
    {
        var values = (PatternType[])Enum.GetValues(typeof(PatternType));
        Assert.Contains(PatternType.Stripes,    values);
        Assert.Contains(PatternType.Chevrons,   values);
        Assert.Contains(PatternType.Camouflage, values);
        Assert.Contains(PatternType.Chequered,  values);
        Assert.Contains(PatternType.Geometric,  values);
        Assert.Contains(PatternType.Noise,      values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UVZone enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UVZone_AllValuesAreDefined()
    {
        var values = (UVZone[])Enum.GetValues(typeof(UVZone));
        Assert.Contains(UVZone.Fuselage,    values);
        Assert.Contains(UVZone.Wings,       values);
        Assert.Contains(UVZone.Tail,        values);
        Assert.Contains(UVZone.Engines,     values);
        Assert.Contains(UVZone.LandingGear, values);
        Assert.Contains(UVZone.Nose,        values);
        Assert.Contains(UVZone.All,         values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryTemplateCategory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryTemplateCategory_AllValuesAreDefined()
    {
        var values = (LiveryTemplateCategory[])Enum.GetValues(typeof(LiveryTemplateCategory));
        Assert.Contains(LiveryTemplateCategory.Commercial, values);
        Assert.Contains(LiveryTemplateCategory.Military,   values);
        Assert.Contains(LiveryTemplateCategory.Racing,     values);
        Assert.Contains(LiveryTemplateCategory.Historic,   values);
        Assert.Contains(LiveryTemplateCategory.Fantasy,    values);
        Assert.Contains(LiveryTemplateCategory.Team,       values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryEditorConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryEditorConfig_DefaultValues_AreReasonable()
    {
        var config = ScriptableObject.CreateInstance<LiveryEditorConfig>();
        Assert.GreaterOrEqual(config.MaxLayers, 1);
        Assert.GreaterOrEqual(config.DefaultTextureResolution, 64);
        Assert.GreaterOrEqual(config.UndoHistoryDepth, 1);
        Assert.GreaterOrEqual(config.RecentColorCount, 1);
        Assert.GreaterOrEqual(config.ColorPalettePresets.Count, 1);
        Assert.GreaterOrEqual(config.SupportedImportFormats.Count, 1);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void LiveryEditorConfig_ColorPalettePresets_ContainWhiteAndBlack()
    {
        var config = ScriptableObject.CreateInstance<LiveryEditorConfig>();
        bool hasWhite = false, hasBlack = false;
        foreach (var p in config.ColorPalettePresets)
        {
            if (p.Value == Color.white) hasWhite = true;
            if (p.Value == Color.black) hasBlack = true;
        }
        Assert.IsTrue(hasWhite, "White preset missing");
        Assert.IsTrue(hasBlack, "Black preset missing");
        UnityEngine.Object.DestroyImmediate(config);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryLayer
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryLayer_Create_HasUniqueId()
    {
        var a = new LiveryLayer("A", LiveryLayerType.BaseColor);
        var b = new LiveryLayer("B", LiveryLayerType.Decal);
        Assert.AreNotEqual(a.LayerId, b.LayerId);
    }

    [Test]
    public void LiveryLayer_DefaultVisibility_IsTrue()
    {
        var layer = new LiveryLayer("Test", LiveryLayerType.Pattern);
        Assert.IsTrue(layer.IsVisible);
    }

    [Test]
    public void LiveryLayer_DefaultOpacity_IsOne()
    {
        var layer = new LiveryLayer("Test", LiveryLayerType.Gradient);
        Assert.AreEqual(1f, layer.Opacity, 0.001f);
    }

    [Test]
    public void LiveryLayer_Duplicate_CreatesNewId()
    {
        var original = new LiveryLayer("Orig", LiveryLayerType.Decal);
        var copy     = original.Duplicate();
        Assert.AreNotEqual(original.LayerId, copy.LayerId);
        Assert.IsTrue(copy.Name.Contains("copy"), "Duplicate name should contain 'copy'");
    }

    [Test]
    public void LiveryLayer_Duplicate_CopiesProperties()
    {
        var original = new LiveryLayer("Orig", LiveryLayerType.Mask)
        {
            Opacity   = 0.5f,
            BlendMode = BlendMode.Multiply,
            IsVisible = false
        };
        var copy = original.Duplicate();
        Assert.AreEqual(original.Opacity,   copy.Opacity,   0.001f);
        Assert.AreEqual(original.BlendMode, copy.BlendMode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LayerBlender
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LayerBlender_Normal_ReturnsSource()
    {
        var dst = Color.white;
        var src = Color.red;
        var result = LayerBlender.Blend(dst, src, BlendMode.Normal);
        Assert.AreEqual(src, result);
    }

    [Test]
    public void LayerBlender_Multiply_BlackGivesBlack()
    {
        var result = LayerBlender.Blend(Color.white, Color.black, BlendMode.Multiply);
        Assert.AreEqual(0f, result.r, 0.01f);
        Assert.AreEqual(0f, result.g, 0.01f);
        Assert.AreEqual(0f, result.b, 0.01f);
    }

    [Test]
    public void LayerBlender_Screen_WhiteGivesWhite()
    {
        var result = LayerBlender.Blend(Color.white, Color.white, BlendMode.Screen);
        Assert.AreEqual(1f, result.r, 0.01f);
    }

    [Test]
    public void LayerBlender_Darken_ChoosesLowerChannel()
    {
        var dst = new Color(0.8f, 0.8f, 0.8f);
        var src = new Color(0.3f, 0.3f, 0.3f);
        var result = LayerBlender.Blend(dst, src, BlendMode.Darken);
        Assert.AreEqual(0.3f, result.r, 0.01f);
    }

    [Test]
    public void LayerBlender_Lighten_ChoosesHigherChannel()
    {
        var dst = new Color(0.2f, 0.2f, 0.2f);
        var src = new Color(0.9f, 0.9f, 0.9f);
        var result = LayerBlender.Blend(dst, src, BlendMode.Lighten);
        Assert.AreEqual(0.9f, result.r, 0.01f);
    }

    [Test]
    public void LayerBlender_Add_ClampedAtOne()
    {
        var result = LayerBlender.Blend(Color.white, Color.white, BlendMode.Add);
        Assert.AreEqual(1f, result.r, 0.01f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LayerHistoryController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LayerHistory_InitialState_CannotUndoOrRedo()
    {
        var go   = new GameObject();
        var ctrl = go.AddComponent<LayerHistoryController>();
        Assert.IsFalse(ctrl.CanUndo);
        Assert.IsFalse(ctrl.CanRedo);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LayerHistory_AfterRecord_CanUndo()
    {
        var go   = new GameObject();
        var ctrl = go.AddComponent<LayerHistoryController>();
        ctrl.Record("layer1", "{}");
        ctrl.Record("layer1", "{\"op\":1}");
        Assert.IsTrue(ctrl.CanUndo);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LayerHistory_Undo_DecrementsCursor()
    {
        var go   = new GameObject();
        var ctrl = go.AddComponent<LayerHistoryController>();
        ctrl.Record("layer1", "s0");
        ctrl.Record("layer1", "s1");
        ctrl.Record("layer1", "s2");
        ctrl.Undo();
        Assert.IsTrue(ctrl.CanRedo);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LayerHistory_Redo_AfterUndo_Works()
    {
        var go   = new GameObject();
        var ctrl = go.AddComponent<LayerHistoryController>();
        ctrl.Record("layer1", "s0");
        ctrl.Record("layer1", "s1");
        ctrl.Undo();
        ctrl.Redo();
        Assert.IsFalse(ctrl.CanRedo);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LayerHistory_NewRecordAfterUndo_ClearsRedoFuture()
    {
        var go   = new GameObject();
        var ctrl = go.AddComponent<LayerHistoryController>();
        ctrl.Record("layer1", "s0");
        ctrl.Record("layer1", "s1");
        ctrl.Undo();
        ctrl.Record("layer1", "s2-branch");
        Assert.IsFalse(ctrl.CanRedo, "Redo future should be cleared after new record");
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GradientStop
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GradientStop_Create_SetsPositionAndColor()
    {
        var stop = GradientStop.Create(0.5f, Color.red);
        Assert.AreEqual(0.5f, stop.Position, 0.001f);
        Assert.AreEqual(Color.red, stop.Color);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PatternGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PatternGenerator_Stripes_ReturnsCorrectSize()
    {
        var tex = PatternGenerator.Generate(PatternType.Stripes, 64, 32, Color.red, Color.blue);
        Assert.AreEqual(64, tex.width);
        Assert.AreEqual(32, tex.height);
        UnityEngine.Object.DestroyImmediate(tex);
    }

    [Test]
    public void PatternGenerator_AllTypes_DoNotThrow()
    {
        foreach (PatternType type in Enum.GetValues(typeof(PatternType)))
        {
            Assert.DoesNotThrow(() =>
            {
                var tex = PatternGenerator.Generate(type, 32, 32, Color.white, Color.black);
                UnityEngine.Object.DestroyImmediate(tex);
            }, $"PatternType.{type} threw an exception");
        }
    }

    [Test]
    public void PatternGenerator_Camouflage_ContainsBothColors()
    {
        var tex   = PatternGenerator.Generate(PatternType.Camouflage, 64, 64, Color.red, Color.blue);
        var pxls  = tex.GetPixels();
        bool hasA = false, hasB = false;
        foreach (var p in pxls)
        {
            if (p.r > 0.5f) hasA = true;
            if (p.b > 0.5f) hasB = true;
            if (hasA && hasB) break;
        }
        Assert.IsTrue(hasA, "Primary colour missing from camouflage texture");
        Assert.IsTrue(hasB, "Secondary colour missing from camouflage texture");
        UnityEngine.Object.DestroyImmediate(tex);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GradientPainter
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GradientPainter_LinearTopBottom_BottomIsFrom()
    {
        var canvas = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        GradientPainter.PaintLinear(canvas, Color.red, Color.blue);
        Color bottom = canvas.GetPixel(8, 0);
        Assert.AreEqual(Color.red.r, bottom.r, 0.1f);
        UnityEngine.Object.DestroyImmediate(canvas);
    }

    [Test]
    public void GradientPainter_MultiStop_SamplesIntermediate()
    {
        var canvas = new Texture2D(32, 1, TextureFormat.RGBA32, false);
        var stops = new List<GradientStop>
        {
            GradientStop.Create(0f, Color.black),
            GradientStop.Create(1f, Color.white)
        };
        GradientPainter.Paint(canvas, GradientType.Linear, stops, Vector2.zero, Vector2.right);
        Color mid = canvas.GetPixel(16, 0);
        Assert.AreEqual(0.5f, mid.r, 0.15f, "Mid-point should be ~grey");
        UnityEngine.Object.DestroyImmediate(canvas);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DecalTransform
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DecalTransform_DefaultScale_IsOne()
    {
        var t = new DecalTransform();
        Assert.AreEqual(Vector2.one, t.Scale);
    }

    [Test]
    public void DecalTransform_DefaultFlipFlags_AreFalse()
    {
        var t = new DecalTransform();
        Assert.IsFalse(t.FlipX);
        Assert.IsFalse(t.FlipY);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryMetadata
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryMetadata_DefaultFormatVersion_IsOne()
    {
        var meta = new LiveryMetadata();
        Assert.AreEqual(1, meta.FormatVersion);
    }

    [Test]
    public void LiveryMetadata_DefaultLists_AreNotNull()
    {
        var meta = new LiveryMetadata();
        Assert.IsNotNull(meta.CompatibleAircraftIds);
        Assert.IsNotNull(meta.Tags);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiverySaveData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiverySaveData_DefaultResolution_Is2048()
    {
        var data = new LiverySaveData();
        Assert.AreEqual(2048, data.TextureResolution);
    }

    [Test]
    public void LiverySaveData_DefaultMetadata_IsNotNull()
    {
        var data = new LiverySaveData();
        Assert.IsNotNull(data.Metadata);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LayerSnapshot
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LayerSnapshot_Create_SetsLayerIdAndState()
    {
        var snap = LayerSnapshot.Create("layer_abc", "{\"op\":\"paint\"}");
        Assert.AreEqual("layer_abc",       snap.LayerId);
        Assert.AreEqual("{\"op\":\"paint\"}", snap.SerializedState);
        Assert.Greater(snap.TimestampUtc, 0L);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BrushSettings
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BrushSettings_Defaults_AreReasonable()
    {
        var b = new BrushSettings();
        Assert.AreEqual(BrushType.Round,     b.Type);
        Assert.AreEqual(MirrorMode.None,     b.Mirror);
        Assert.AreEqual(1f,                  b.Opacity, 0.001f);
        Assert.Greater(b.SizePx, 0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryTemplate
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryTemplate_DefaultPrimaryColor_IsWhite()
    {
        var t = new LiveryTemplate();
        Assert.AreEqual(Color.white, t.PrimaryColor);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DecalAssetRecord
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DecalAssetRecord_Fields_CanBeAssigned()
    {
        var rec = new DecalAssetRecord
        {
            DecalId     = "test_decal",
            DisplayName = "Test",
            Category    = DecalCategory.Custom
        };
        Assert.AreEqual("test_decal",      rec.DecalId);
        Assert.AreEqual(DecalCategory.Custom, rec.Category);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LiveryEditorAnalytics
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LiveryEditorAnalytics_TrackCreated_IncrementsCounter()
    {
        LiveryEditorAnalytics.ResetCounters();
        LiveryEditorAnalytics.TrackLiveryCreated("id1");
        Assert.AreEqual(1, LiveryEditorAnalytics.LiveriesCreated);
    }

    [Test]
    public void LiveryEditorAnalytics_TrackSaved_IncrementsCounter()
    {
        LiveryEditorAnalytics.ResetCounters();
        LiveryEditorAnalytics.TrackLiverySaved("id1");
        Assert.AreEqual(1, LiveryEditorAnalytics.LiveriesSaved);
    }

    [Test]
    public void LiveryEditorAnalytics_TrackExported_IncrementsCounter()
    {
        LiveryEditorAnalytics.ResetCounters();
        LiveryEditorAnalytics.TrackLiveryExported("id1", LiveryExportFormat.PNG);
        Assert.AreEqual(1, LiveryEditorAnalytics.LiveriesExported);
    }

    [Test]
    public void LiveryEditorAnalytics_ResetCounters_SetsAllToZero()
    {
        LiveryEditorAnalytics.TrackEditorOpened();
        LiveryEditorAnalytics.TrackBrushStroke(BrushType.Round);
        LiveryEditorAnalytics.TrackDecalPlaced(DecalCategory.Airline);
        LiveryEditorAnalytics.ResetCounters();
        Assert.AreEqual(0, LiveryEditorAnalytics.SessionsOpened);
        Assert.AreEqual(0, LiveryEditorAnalytics.BrushStrokes);
        Assert.AreEqual(0, LiveryEditorAnalytics.DecalsPlaced);
    }

    [Test]
    public void LiveryEditorAnalytics_GetSessionSummary_ReturnsNonEmptyString()
    {
        LiveryEditorAnalytics.ResetCounters();
        string summary = LiveryEditorAnalytics.GetSessionSummary();
        Assert.IsFalse(string.IsNullOrWhiteSpace(summary));
        StringAssert.Contains("Sessions=0", summary);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ColorPreset
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ColorPreset_DefaultColor_IsWhite()
    {
        var p = new ColorPreset();
        Assert.AreEqual(Color.white, p.Value);
    }
}
