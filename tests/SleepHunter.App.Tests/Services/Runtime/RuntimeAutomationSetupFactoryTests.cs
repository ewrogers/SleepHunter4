using System.Collections.Immutable;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Services.Runtime;
using SleepHunter.Settings;
using SleepHunter.ViewModels.Editing;
using RuntimeSpellTarget =
    SleepHunter.Runtime.Automation.Spells.SpellTarget;

namespace SleepHunter.Tests.Services.Runtime;

public sealed class RuntimeAutomationSetupFactoryTests
{
    [Test]
    public void ShouldComposeQueuesPoliciesAndStaffCatalogs()
    {
        var staff = new StaffCandidate(
            "test staff",
            CharacterClass.Wizard,
            requiredLevel: 50,
            requiredAbilityLevel: 10,
            castLines: 1);
        var provider = new RecordingStaffCandidateProvider(staff);
        var factory = new RuntimeAutomationSetupFactory(provider);
        var settings = CreateSettings();
        var configuration = CreateConfiguration();

        var setup = factory.Create(
            configuration,
            settings,
            CharacterClass.Wizard);
        var automation = setup.ConfigureAutomation.Configuration;
        var spellPolicy = automation.SpellPolicy;
        var flowerPolicy = automation.FlowerPolicy;
        var skillPolicy = automation.SkillPolicy;

        Assert.Multiple(() =>
        {
            Assert.That(
                setup.ReplaceQueues.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(
                setup.ReplaceQueues.SpellQueue.Entries,
                Has.Length.EqualTo(1));
            Assert.That(
                setup.ReplaceQueues.SkillQueue.Entries,
                Has.Length.EqualTo(1));
            Assert.That(
                setup.ReplaceQueues.FlowerQueue.Entries,
                Has.Length.EqualTo(1));
            Assert.That(automation.SpellsEnabled, Is.True);
            Assert.That(automation.SkillsEnabled, Is.True);
            Assert.That(automation.FloweringEnabled, Is.True);
            Assert.That(automation.FlowerBeforeSpells, Is.False);
            Assert.That(spellPolicy.Cast.RequireMana, Is.True);
            Assert.That(
                spellPolicy.Cast.Timing.ZeroLineDuration,
                Is.EqualTo(TimeSpan.FromMilliseconds(250)));
            Assert.That(
                spellPolicy.Cast.Timing.SingleLineDuration,
                Is.EqualTo(TimeSpan.FromMilliseconds(900)));
            Assert.That(
                spellPolicy.Cast.Timing.MultiLineDurationPerLine,
                Is.EqualTo(TimeSpan.FromMilliseconds(750)));
            Assert.That(
                spellPolicy.Cast.Timing.CompletionPadding,
                Is.EqualTo(TimeSpan.FromMilliseconds(100)));
            Assert.That(spellPolicy.AllowStaffSwitching, Is.True);
            Assert.That(
                spellPolicy.Cast.SkipCoolingDownSpells,
                Is.False);
            Assert.That(skillPolicy.Planning.RequireMana, Is.False);
            Assert.That(
                skillPolicy.Planning.AssailMode,
                Is.EqualTo(AssailMode.SkillSlot));
            Assert.That(
                skillPolicy.Planning.DisarmForAssails,
                Is.False);
            Assert.That(
                flowerPolicy.Target.AutoFlowerWaitingCharacters,
                Is.True);
            Assert.That(
                flowerPolicy.Target.PrioritizeAlternateCharacters,
                Is.False);
            Assert.That(
                flowerPolicy.Target.MaximumXDistance,
                Is.EqualTo(7));
            Assert.That(
                flowerPolicy.Target.MaximumYDistance,
                Is.EqualTo(8));
            Assert.That(flowerPolicy.UseVineyard, Is.True);
            Assert.That(flowerPolicy.RestoreMana, Is.True);
            Assert.That(flowerPolicy.RestoreManaOnDemand, Is.True);
            Assert.That(
                flowerPolicy.ManaRestorationThreshold,
                Is.EqualTo(1001));
            Assert.That(
                flowerPolicy.MinimumManaBeforePlant,
                Is.EqualTo(500));
            Assert.That(
                automation.ObservationChanges.MapChange,
                Is.EqualTo(ObservationChangeAction.CloseClient));
            Assert.That(
                automation.ObservationChanges.CoordinateChange,
                Is.EqualTo(ObservationChangeAction.Pause));
            Assert.That(
                automation.PanelPreservation.Enabled,
                Is.True);
            Assert.That(
                automation.SpellStaffCatalog.GetCandidates(
                    configuration.Spells[0].Id),
                Is.EqualTo(ImmutableArray.Create(staff)));
            Assert.That(
                automation.FlowerStaffCatalog.GetCandidates(
                    FlowerActionKind.RestoreMana),
                Is.EqualTo(ImmutableArray.Create(staff)));
            Assert.That(
                provider.Requests,
                Is.EqualTo(new[]
                {
                    ("queued spell", CharacterClass.Wizard),
                    (FlowerSpellNames.ManaRestoration, CharacterClass.Wizard),
                    (FlowerSpellNames.Vineyard, CharacterClass.Wizard),
                    (FlowerSpellNames.Plant, CharacterClass.Wizard)
                }));
        });
    }

    [Test]
    public void ShouldHonorPersistedRotationAndDisableEmptyCategories()
    {
        var provider = new RecordingStaffCandidateProvider();
        var factory = new RuntimeAutomationSetupFactory(provider);
        var settings = CreateSettings();
        settings.AllowStaffSwitching = false;
        var configuration = new MacroConfiguration(
            spellRotation: SpellQueueRotation.Sequential);

        var setup = factory.Create(
            configuration,
            settings,
            CharacterClass.Unknown);
        var automation = setup.ConfigureAutomation.Configuration;

        Assert.Multiple(() =>
        {
            Assert.That(
                setup.ReplaceQueues.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.Sequential));
            Assert.That(automation.SpellsEnabled, Is.False);
            Assert.That(automation.SkillsEnabled, Is.False);
            Assert.That(automation.FloweringEnabled, Is.False);
            Assert.That(
                automation.SpellStaffCatalog,
                Is.SameAs(SpellStaffCatalog.Empty));
            Assert.That(
                automation.FlowerStaffCatalog,
                Is.SameAs(FlowerStaffCatalog.Empty));
            Assert.That(provider.Requests, Is.Empty);
        });
    }

    [Test]
    public void ShouldEnableFloweringForVineyardWithoutQueuedTargets()
    {
        var factory = new RuntimeAutomationSetupFactory(
            new RecordingStaffCandidateProvider());
        var settings = CreateSettings();
        settings.AllowStaffSwitching = false;
        var configuration = new MacroConfiguration(
            flowerOptions: new FlowerOptions(useVineyard: true));

        var setup = factory.Create(
            configuration,
            settings,
            CharacterClass.Unknown);

        Assert.That(
            setup.ConfigureAutomation.Configuration.FloweringEnabled,
            Is.True);
    }

    [Test]
    public void ShouldRejectSettingsThatCannotBecomeRuntimePolicy()
    {
        var factory = new RuntimeAutomationSetupFactory(
            new RecordingStaffCandidateProvider());
        var settings = CreateSettings();

        Assert.Multiple(() =>
        {
            settings.FasSpioradThreshold = double.PositiveInfinity;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => factory.Create(
                    MacroConfiguration.Empty,
                    settings,
                    CharacterClass.Wizard));

            settings.FasSpioradThreshold = 1000;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => factory.Create(
                    MacroConfiguration.Empty,
                    settings,
                    (CharacterClass)int.MaxValue));
        });
    }

    [Test]
    public void ShouldFilterStaffMetadataForObservedCharacterClass()
    {
        var neutral = Staff("neutral", CharacterClassFlags.All, level: 1);
        var wizard = Staff("wizard", CharacterClassFlags.Wizard, level: 2);
        var priest = Staff("priest", CharacterClassFlags.Priest, level: 3);
        var hybrid = Staff(
            "hybrid",
            CharacterClassFlags.Wizard | CharacterClassFlags.Priest,
            level: 4);
        var lines = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase)
        {
            [neutral.Name] = 4,
            [wizard.Name] = 1,
            [priest.Name] = 0,
            [hybrid.Name] = 2
        };
        var provider = new RuntimeStaffCandidateProvider(
            () => [neutral, wizard, priest, hybrid],
            (staffName, _) => lines[staffName]);

        var candidates = provider.GetCandidates(
            "queued spell",
            CharacterClass.Wizard);
        var unknownCandidates = provider.GetCandidates(
            "queued spell",
            CharacterClass.Unknown);

        Assert.Multiple(() =>
        {
            Assert.That(
                candidates.Select(candidate => candidate.Name),
                Is.EqualTo(new[] { "hybrid", "neutral", "wizard" }));
            Assert.That(
                candidates.Single(candidate => candidate.Name == "neutral")
                    .RequiredClass,
                Is.Null);
            Assert.That(
                candidates
                    .Where(candidate => candidate.Name != "neutral")
                    .Select(candidate => candidate.RequiredClass),
                Is.All.EqualTo(CharacterClass.Wizard));
            Assert.That(
                candidates.Select(candidate => candidate.RequiredLevel),
                Is.EqualTo(new[] { 4, 1, 2 }));
            Assert.That(
                unknownCandidates.Select(candidate => candidate.Name),
                Is.EqualTo(new[] { "neutral" }));
        });
    }

    [Test]
    public void ShouldRejectUnsupportedStaffClassFlags()
    {
        var invalid = Staff(
            "invalid",
            (CharacterClassFlags)0x40,
            level: 1);
        var provider = new RuntimeStaffCandidateProvider(
            () => [invalid],
            (_, _) => 1);

        Assert.Throws<InvalidOperationException>(
            () => provider.GetCandidates(
                "queued spell",
                CharacterClass.Wizard));
    }

    private static MacroConfiguration CreateConfiguration() =>
        new(
            spellRotation: null,
            skills:
            [
                new SkillQueueEntry(
                    new SkillQueueEntryId(1),
                    "queued skill")
            ],
            spells:
            [
                new SpellQueueEntry(
                    new SpellQueueEntryId(2),
                    "queued spell",
                    target: RuntimeSpellTarget.Self)
            ],
            flowers:
            [
                new FlowerQueueEntry(
                    new FlowerQueueEntryId(3),
                    RuntimeSpellTarget.Character("alternate"),
                    interval: TimeSpan.FromMinutes(1))
            ],
            flowerOptions: new FlowerOptions(
                useVineyard: true,
                flowerAlternateCharacters: true,
                prioritizeAlternateCharacters: true,
                maximumXDistance: 7,
                maximumYDistance: 8));

    private static UserSettings CreateSettings() =>
        new()
        {
            SpellRotationMode = SpellRotationMode.RoundRobin,
            ZeroLineDelay = TimeSpan.FromMilliseconds(250),
            SingleLineDelay = TimeSpan.FromMilliseconds(900),
            MultipleLineDelay = TimeSpan.FromMilliseconds(750),
            RequireManaForSpells = true,
            SkipSpellsOnCooldown = false,
            AllowStaffSwitching = true,
            UseSpaceForAssail = false,
            DisarmForAssails = false,
            UseFasSpiorad = true,
            UseFasSpioradOnDemand = true,
            FasSpioradThreshold = 1000.2,
            FlowerHasMinimum = true,
            FlowerMinimumMana = 500,
            FlowerAltsFirst = false,
            FlowerBeforeSpellMacros = false,
            MapChangeAction = ObservationChangeAction.CloseClient,
            CoordsChangeAction = ObservationChangeAction.Pause,
            PreserveUserPanel = true
        };

    private static StaffMetadata Staff(
        string name,
        CharacterClassFlags playerClass,
        int level) =>
        new()
        {
            Name = name,
            Class = playerClass,
            Level = level,
            AbilityLevel = level + 10
        };

    private sealed class RecordingStaffCandidateProvider :
        IRuntimeStaffCandidateProvider
    {
        private readonly ImmutableArray<StaffCandidate> candidates;

        public RecordingStaffCandidateProvider(
            params StaffCandidate[] candidates)
        {
            this.candidates = [.. candidates];
        }

        public List<(string SpellName, CharacterClass CharacterClass)> Requests
        {
            get;
        } = [];

        public ImmutableArray<StaffCandidate> GetCandidates(
            string spellName,
            CharacterClass characterClass)
        {
            Requests.Add((spellName, characterClass));
            return candidates;
        }
    }
}
