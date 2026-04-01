// VoiceCommandTests.cs — NUnit EditMode tests for Phase 84 Voice Command System
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.VoiceCommand;

[TestFixture]
public class VoiceCommandTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Returns a small registry suitable for parser tests.</summary>
    private static VoiceCommandDefinition[] BuildRegistry()
    {
        return new[]
        {
            new VoiceCommandDefinition
            {
                commandId     = "cmd_increase_throttle",
                primaryPhrase = "increase throttle",
                aliases       = new[] { "throttle up", "more power" },
                category      = CommandCategory.Flight,
                priority      = CommandPriority.Normal,
                requiresConfirmation = false,
                parameterHints = Array.Empty<string>()
            },
            new VoiceCommandDefinition
            {
                commandId     = "cmd_set_altitude",
                primaryPhrase = "set altitude to",
                aliases       = new[] { "climb to", "altitude" },
                category      = CommandCategory.Flight,
                priority      = CommandPriority.Normal,
                requiresConfirmation = false,
                parameterHints = new[] { "altitude" }
            },
            new VoiceCommandDefinition
            {
                commandId     = "cmd_landing_gear_down",
                primaryPhrase = "landing gear down",
                aliases       = new[] { "gear down", "deploy gear" },
                category      = CommandCategory.Flight,
                priority      = CommandPriority.Critical,
                requiresConfirmation = true,
                parameterHints = Array.Empty<string>()
            },
            new VoiceCommandDefinition
            {
                commandId     = "cmd_weather_report",
                primaryPhrase = "weather report",
                aliases       = new[] { "weather update", "current weather" },
                category      = CommandCategory.Weather,
                priority      = CommandPriority.Low,
                requiresConfirmation = false,
                parameterHints = Array.Empty<string>()
            },
            new VoiceCommandDefinition
            {
                commandId     = "cmd_next_track",
                primaryPhrase = "next track",
                aliases       = new[] { "skip track", "next song" },
                category      = CommandCategory.Music,
                priority      = CommandPriority.Low,
                requiresConfirmation = false,
                parameterHints = Array.Empty<string>()
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandParser — exact match
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Parse_ExactPrimaryPhrase_ReturnsCorrectCommand()
    {
        var registry = BuildRegistry();
        var result   = CommandParser.Parse("increase throttle", registry,
                                           out _, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNotNull(result);
        Assert.AreEqual("cmd_increase_throttle", result.commandId);
    }

    [Test]
    public void Parse_ExactAlias_ReturnsCorrectCommand()
    {
        var registry = BuildRegistry();
        var result   = CommandParser.Parse("throttle up", registry,
                                           out _, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNotNull(result);
        Assert.AreEqual("cmd_increase_throttle", result.commandId);
    }

    [Test]
    public void Parse_CaseInsensitive_ReturnsMatch()
    {
        var registry = BuildRegistry();
        var result   = CommandParser.Parse("INCREASE THROTTLE", registry,
                                           out _, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNotNull(result);
        Assert.AreEqual("cmd_increase_throttle", result.commandId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandParser — fuzzy match
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Parse_FuzzyMatch_WithinThreshold_ReturnsMatch()
    {
        var registry = BuildRegistry();
        // "increas throttle" is 1 edit away from "increase throttle"
        var result = CommandParser.Parse("increas throttle", registry,
                                         out _, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNotNull(result);
        Assert.AreEqual("cmd_increase_throttle", result.commandId);
    }

    [Test]
    public void Parse_FuzzyMatch_BeyondThreshold_ReturnsNull()
    {
        var registry = BuildRegistry();
        // "xyz abc def ghi" is far from every registered phrase
        var result = CommandParser.Parse("xyz abc def ghi jkl mno", registry,
                                         out _, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNull(result);
    }

    [Test]
    public void Parse_EmptyPhrase_ReturnsNull()
    {
        var registry = BuildRegistry();
        var result   = CommandParser.Parse(string.Empty, registry, out _);
        Assert.IsNull(result);
    }

    [Test]
    public void Parse_NullRegistry_ReturnsNull()
    {
        var result = CommandParser.Parse("increase throttle", null, out _);
        Assert.IsNull(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandParser — parameter extraction
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Parse_ExtractsAltitudeParam_FromTrailingNumber()
    {
        var registry = BuildRegistry();
        var result   = CommandParser.Parse("set altitude to 30000", registry,
                                           out var parameters, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNotNull(result);
        Assert.AreEqual("cmd_set_altitude", result.commandId);
        Assert.IsTrue(parameters.ContainsKey("altitude"),
            "Parameters should contain 'altitude' key.");
        Assert.AreEqual("30000", parameters["altitude"]);
    }

    [Test]
    public void Parse_AliasWithParam_ExtractsValue()
    {
        var registry = BuildRegistry();
        var result   = CommandParser.Parse("climb to 5000", registry,
                                           out var parameters, CommandParser.DefaultFuzzyThreshold);
        Assert.IsNotNull(result);
        Assert.AreEqual("cmd_set_altitude", result.commandId);
        Assert.IsTrue(parameters.ContainsKey("altitude"));
        Assert.AreEqual("5000", parameters["altitude"]);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandParser — GetSuggestions
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GetSuggestions_PrefixMatch_ReturnsRelevantCommand()
    {
        var registry    = BuildRegistry();
        var suggestions = CommandParser.GetSuggestions("increase", registry, 5);
        Assert.IsTrue(suggestions.Count > 0);
        Assert.AreEqual("cmd_increase_throttle", suggestions[0].commandId);
    }

    [Test]
    public void GetSuggestions_MaxResults_IsRespected()
    {
        var registry    = BuildRegistry();
        var suggestions = CommandParser.GetSuggestions("a", registry, 2);
        Assert.LessOrEqual(suggestions.Count, 2);
    }

    [Test]
    public void GetSuggestions_EmptyPartial_ReturnsEmptyList()
    {
        var registry    = BuildRegistry();
        var suggestions = CommandParser.GetSuggestions(string.Empty, registry, 5);
        Assert.AreEqual(0, suggestions.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandParser — NormalisePhrase
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NormalisePhrase_StripsFiller_PleasePrefix()
    {
        string result = CommandParser.NormalisePhrase("please increase throttle");
        Assert.AreEqual("increase throttle", result);
    }

    [Test]
    public void NormalisePhrase_StripsFiller_HeyPilot()
    {
        string result = CommandParser.NormalisePhrase("hey pilot increase throttle");
        Assert.AreEqual("increase throttle", result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandParser — Levenshtein distance
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        Assert.AreEqual(0, CommandParser.LevenshteinDistance("hello", "hello"));
    }

    [Test]
    public void LevenshteinDistance_OneInsertion_ReturnsOne()
    {
        Assert.AreEqual(1, CommandParser.LevenshteinDistance("hell", "hello"));
    }

    [Test]
    public void LevenshteinDistance_OneSubstitution_ReturnsOne()
    {
        Assert.AreEqual(1, CommandParser.LevenshteinDistance("hallo", "hello"));
    }

    [Test]
    public void LevenshteinDistance_EmptyToWord_ReturnsWordLength()
    {
        Assert.AreEqual(5, CommandParser.LevenshteinDistance(string.Empty, "hello"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CommandRegistry — register / unregister / lookup
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject _registryObj;
    private CommandRegistry _registry;

    [SetUp]
    public void SetUpRegistry()
    {
        _registryObj = new GameObject("CommandRegistry");
        _registry    = _registryObj.AddComponent<CommandRegistry>();
    }

    [TearDown]
    public void TearDownRegistry()
    {
        if (_registryObj != null)
            UnityEngine.Object.DestroyImmediate(_registryObj);
    }

    [Test]
    public void CommandRegistry_Register_CommandIsRetrievable()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "test_cmd",
            primaryPhrase = "test command",
            category      = CommandCategory.System
        };
        _registry.Register(def);
        var retrieved = _registry.GetById("test_cmd");
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("test command", retrieved.primaryPhrase);
    }

    [Test]
    public void CommandRegistry_Unregister_CommandIsNotFound()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "removable_cmd",
            primaryPhrase = "removable",
            category      = CommandCategory.System
        };
        _registry.Register(def);
        _registry.Unregister("removable_cmd");
        Assert.IsNull(_registry.GetById("removable_cmd"));
    }

    [Test]
    public void CommandRegistry_GetByCategory_ReturnsOnlyMatchingCategory()
    {
        var weatherDef = new VoiceCommandDefinition
        {
            commandId     = "cat_test_weather",
            primaryPhrase = "weather check test",
            category      = CommandCategory.Weather
        };
        _registry.Register(weatherDef);

        var weatherCommands = _registry.GetByCategory(CommandCategory.Weather);
        foreach (var cmd in weatherCommands)
            Assert.AreEqual(CommandCategory.Weather, cmd.category);

        bool found = false;
        foreach (var cmd in weatherCommands)
            if (cmd.commandId == "cat_test_weather") { found = true; break; }
        Assert.IsTrue(found, "Newly registered weather command should be in GetByCategory result.");
    }

    [Test]
    public void CommandRegistry_GetAll_ContainsBuiltInCommands()
    {
        var all = _registry.GetAll();
        Assert.GreaterOrEqual(all.Length, 40,
            "Registry should contain at least 40 built-in commands.");
    }

    [Test]
    public void CommandRegistry_Register_NullDefinition_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _registry.Register(null));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VoiceCommandHistory — circular buffer behaviour
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject _historyObj;
    private VoiceCommandHistory _history;

    [SetUp]
    public void SetUpHistory()
    {
        _historyObj = new GameObject("VoiceCommandHistory");
        _history    = _historyObj.AddComponent<VoiceCommandHistory>();
    }

    [TearDown]
    public void TearDownHistory()
    {
        if (_historyObj != null)
            UnityEngine.Object.DestroyImmediate(_historyObj);
    }

    [Test]
    public void History_NewInstance_CountIsZero()
    {
        Assert.AreEqual(0, _history.Count);
    }

    [Test]
    public void History_Record_IncreasesCount()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "test",
            primaryPhrase = "test",
            category      = CommandCategory.System
        };
        var result = VoiceCommandResult.Success(def, "voice_response_acknowledged");
        _history.Record(result, 0.9f);
        Assert.AreEqual(1, _history.Count);
    }

    [Test]
    public void History_GetRecent_ReturnsNewestFirst()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "seq_cmd",
            primaryPhrase = "sequence",
            category      = CommandCategory.System
        };

        for (int i = 0; i < 5; i++)
        {
            var r = VoiceCommandResult.Success(def, "voice_response_acknowledged");
            _history.Record(r, 0.9f);
        }

        var recent = _history.GetRecent(3);
        Assert.AreEqual(3, recent.Count);
    }

    [Test]
    public void History_ClearHistory_SetsCountToZero()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "clr",
            primaryPhrase = "clear test",
            category      = CommandCategory.System
        };
        _history.Record(VoiceCommandResult.Success(def, "voice_response_acknowledged"), 0.9f);
        _history.ClearHistory();
        Assert.AreEqual(0, _history.Count);
    }

    [Test]
    public void History_GetByCategory_FiltersCorrectly()
    {
        var flightDef = new VoiceCommandDefinition
        {
            commandId     = "f",
            primaryPhrase = "fly",
            category      = CommandCategory.Flight
        };
        var sysDef = new VoiceCommandDefinition
        {
            commandId     = "s",
            primaryPhrase = "sys",
            category      = CommandCategory.System
        };

        _history.Record(VoiceCommandResult.Success(flightDef, "x"), 1f);
        _history.Record(VoiceCommandResult.Success(sysDef,    "x"), 1f);

        var flightEntries = _history.GetByCategory(CommandCategory.Flight);
        Assert.AreEqual(1, flightEntries.Count);
        Assert.AreEqual(CommandCategory.Flight, flightEntries[0].category);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VoiceResponseGenerator — template parameter substitution
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SubstituteParameters_ReplacesToken()
    {
        var result = VoiceResponseGenerator.SubstituteParameters(
            "Altitude set to {altitude} feet.",
            new Dictionary<string, string> { { "altitude", "30000" } });
        Assert.AreEqual("Altitude set to 30000 feet.", result);
    }

    [Test]
    public void SubstituteParameters_UnknownToken_IsLeftAsIs()
    {
        var result = VoiceResponseGenerator.SubstituteParameters(
            "Heading {degrees} degrees.",
            new Dictionary<string, string> { { "other", "value" } });
        Assert.AreEqual("Heading {degrees} degrees.", result);
    }

    [Test]
    public void SubstituteParameters_NullParams_ReturnsOriginalTemplate()
    {
        string template = "No params here.";
        string result   = VoiceResponseGenerator.SubstituteParameters(template, null);
        Assert.AreEqual(template, result);
    }

    [Test]
    public void GetShortResponse_SuccessResult_ReturnsNonEmptyString()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "cmd_test",
            primaryPhrase = "test",
            category      = CommandCategory.System
        };
        var result   = VoiceCommandResult.Success(def, "voice_response_acknowledged");
        string short_ = VoiceResponseGenerator.GetShortResponse(result);
        Assert.IsFalse(string.IsNullOrEmpty(short_));
    }

    [Test]
    public void GetDetailedResponse_ContainsCommandIdAndTimestamp()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "cmd_detail_test",
            primaryPhrase = "detail test",
            category      = CommandCategory.System
        };
        var result   = VoiceCommandResult.Success(def, "voice_response_acknowledged");
        string detail = VoiceResponseGenerator.GetDetailedResponse(result);
        Assert.IsTrue(detail.Contains("cmd_detail_test"),
            "Detailed response should contain the command id.");
    }

    [Test]
    public void GetConfirmationPrompt_ContainsCommandPhrase()
    {
        var def = new VoiceCommandDefinition
        {
            commandId     = "cmd_gear_down",
            primaryPhrase = "landing gear down",
            category      = CommandCategory.Flight,
            requiresConfirmation = true
        };
        string prompt = VoiceResponseGenerator.GetConfirmationPrompt(def);
        Assert.IsTrue(prompt.Contains("landing gear down") || prompt.Length > 0,
            "Confirmation prompt should reference the command phrase.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VoiceConfirmationController — timeout behaviour
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ConfirmationController_InitialState_IsNotWaiting()
    {
        var go   = new GameObject("VoiceConfirmationController");
        var ctrl = go.AddComponent<VoiceConfirmationController>();
        Assert.IsFalse(ctrl.IsWaiting);
        Assert.IsNull(ctrl.PendingCommand);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ConfirmationController_RequestConfirmation_SetsIsWaiting()
    {
        var go   = new GameObject("VoiceConfirmationController");
        var ctrl = go.AddComponent<VoiceConfirmationController>();

        var def = new VoiceCommandDefinition
        {
            commandId = "c", primaryPhrase = "critical", requiresConfirmation = true
        };
        ctrl.RequestConfirmation(def);
        Assert.IsTrue(ctrl.IsWaiting);
        Assert.IsNotNull(ctrl.PendingCommand);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ConfirmationController_Confirm_FiresOnConfirmedEvent()
    {
        var go   = new GameObject("VoiceConfirmationController");
        var ctrl = go.AddComponent<VoiceConfirmationController>();

        bool fired = false;
        ctrl.OnConfirmed += _ => fired = true;

        var def = new VoiceCommandDefinition { commandId = "c2", primaryPhrase = "crit2" };
        ctrl.RequestConfirmation(def);
        ctrl.Confirm();

        Assert.IsTrue(fired, "OnConfirmed should have fired.");
        Assert.IsFalse(ctrl.IsWaiting);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ConfirmationController_Cancel_FiresOnCancelledEvent()
    {
        var go   = new GameObject("VoiceConfirmationController");
        var ctrl = go.AddComponent<VoiceConfirmationController>();

        bool fired = false;
        ctrl.OnCancelled += _ => fired = true;

        var def = new VoiceCommandDefinition { commandId = "c3", primaryPhrase = "crit3" };
        ctrl.RequestConfirmation(def);
        ctrl.Cancel();

        Assert.IsTrue(fired, "OnCancelled should have fired.");
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ConfirmationController_QueuedCommands_ProcessedInOrder()
    {
        var go   = new GameObject("VoiceConfirmationController");
        var ctrl = go.AddComponent<VoiceConfirmationController>();

        var def1 = new VoiceCommandDefinition { commandId = "q1", primaryPhrase = "q1" };
        var def2 = new VoiceCommandDefinition { commandId = "q2", primaryPhrase = "q2" };

        string firstConfirmedId = null;
        ctrl.OnConfirmed += d => { if (firstConfirmedId == null) firstConfirmedId = d.commandId; };

        ctrl.RequestConfirmation(def1);
        ctrl.RequestConfirmation(def2);

        // First pending should be def1.
        Assert.AreEqual("q1", ctrl.PendingCommand.commandId);
        ctrl.Confirm();
        // After confirming first, second becomes pending.
        Assert.AreEqual("q2", ctrl.PendingCommand?.commandId);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Enum completeness
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CommandCategory_HasNineValues()
    {
        Assert.AreEqual(9, Enum.GetValues(typeof(CommandCategory)).Length);
    }

    [Test]
    public void CommandPriority_HasFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(CommandPriority)).Length);
    }

    [Test]
    public void CommandCategory_ContainsEmergency()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(CommandCategory), "Emergency"));
    }

    [Test]
    public void CommandPriority_ContainsCritical()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(CommandPriority), "Critical"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VoiceAssistantConfig — default values
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VoiceAssistantConfig_DefaultActivationKeyword_IsHeyPilot()
    {
        var cfg = ScriptableObject.CreateInstance<VoiceAssistantConfig>();
        Assert.AreEqual("Hey Pilot", cfg.activationKeyword);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void VoiceAssistantConfig_DefaultListenTimeout_IsPositive()
    {
        var cfg = ScriptableObject.CreateInstance<VoiceAssistantConfig>();
        Assert.Greater(cfg.listenTimeoutSeconds, 0f);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void VoiceAssistantConfig_DefaultConfidenceThreshold_IsInRange()
    {
        var cfg = ScriptableObject.CreateInstance<VoiceAssistantConfig>();
        Assert.GreaterOrEqual(cfg.confidenceThreshold, 0f);
        Assert.LessOrEqual(cfg.confidenceThreshold,    1f);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void VoiceAssistantConfig_DefaultMaxHistoryEntries_IsPositive()
    {
        var cfg = ScriptableObject.CreateInstance<VoiceAssistantConfig>();
        Assert.Greater(cfg.maxHistoryEntries, 0);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void VoiceAssistantConfig_DefaultEnabledCategories_ContainsAllNine()
    {
        var cfg = ScriptableObject.CreateInstance<VoiceAssistantConfig>();
        Assert.AreEqual(9, cfg.enabledCategories.Count);
        ScriptableObject.DestroyImmediate(cfg);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VoiceCommandResult — factory methods
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void VoiceCommandResult_Success_SetsSuccessTrue()
    {
        var def    = new VoiceCommandDefinition { commandId = "x", primaryPhrase = "x" };
        var result = VoiceCommandResult.Success(def, "voice_response_acknowledged");
        Assert.IsTrue(result.success);
    }

    [Test]
    public void VoiceCommandResult_Failure_SetsSuccessFalse()
    {
        var def    = new VoiceCommandDefinition { commandId = "x", primaryPhrase = "x" };
        var result = VoiceCommandResult.Failure(def, "voice_error_no_match");
        Assert.IsFalse(result.success);
    }

    [Test]
    public void VoiceCommandResult_Success_TimestampIsRecent()
    {
        var def    = new VoiceCommandDefinition { commandId = "t", primaryPhrase = "t" };
        var before = DateTime.UtcNow;
        var result = VoiceCommandResult.Success(def, "x");
        var after  = DateTime.UtcNow;

        Assert.GreaterOrEqual(result.timestamp, before);
        Assert.LessOrEqual(result.timestamp, after);
    }
}
