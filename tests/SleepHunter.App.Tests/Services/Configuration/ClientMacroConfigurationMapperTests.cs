using System.Windows;
using System.Windows.Input;
using SleepHunter.Models;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Hotkeys;
using SleepHunter.ViewModels.Editing;
using RuntimeTargetOffset =
    SleepHunter.Runtime.Automation.Spells.TargetOffset;

namespace SleepHunter.Tests.Services.Configuration;

public sealed class ClientMacroConfigurationMapperTests
{
    [Test]
    public void ShouldRoundTripCompleteCurrentConfiguration()
    {
        var sourcePlayer = CreatePlayer("Source");
        var source = new ClientMacroConfiguration(sourcePlayer)
        {
            Description = "Typed configuration",
            SpellQueueRotation = SpellRotationMode.RoundRobin,
            UseLyliacVineyard = true,
            FlowerAlternateCharacters = true,
            PrioritizeAlternateCharacters = false,
            MaximumFlowerXDistance = 7,
            MaximumFlowerYDistance = 8
        };
        sourcePlayer.Hotkey = new Hotkey(
            ModifierKeys.Control | ModifierKeys.Shift,
            Key.F6);
        source.ReplaceSkills(
        [
            new SkillQueueEntry(
                new SkillQueueEntryId(11),
                "Unknown Skill")
        ]);
        source.AddToSpellQueue(
            new SpellQueueItemViewModel
            {
                Id = 21,
                Name = "Unknown Spell",
                Target = new SpellTargetViewModel
                {
                    Mode = SpellTargetMode.RelativeRadius,
                    Location = new Point(-2, 3),
                    Offset = new Point(4, -5),
                    InnerRadius = 1,
                    OuterRadius = 4
                },
                TargetLevel = 99,
                HealthCondition = new HealthCondition(10, 90)
            });
        source.AddToSpellQueue(
            new SpellQueueItemViewModel
            {
                Id = 22,
                Name = "Untargeted Spell",
                Target = new SpellTargetViewModel
                {
                    Mode = SpellTargetMode.None,
                    Offset = new Point(1, 2)
                }
            });
        source.AddToFlowerQueue(
            new FlowerQueueItemViewModel
            {
                Id = 31,
                Target = new SpellTargetViewModel
                {
                    Mode = SpellTargetMode.Character,
                    CharacterName = "Flower Target",
                    Offset = new Point(6, 7)
                },
                Interval = TimeSpan.FromMinutes(2),
                ManaThreshold = 500
            });
        var mapper = new ClientMacroConfigurationMapper();

        var snapshot = mapper.CreateSnapshot(source);
        using var writer = new StringWriter();
        MacroConfigurationSerializer.Save(snapshot, writer);
        using var reader = new StringReader(writer.ToString());
        var loaded = MacroConfigurationSerializer.Load(reader);
        var destinationPlayer = CreatePlayer("Destination");
        var destination =
            new ClientMacroConfiguration(destinationPlayer);

        mapper.Apply(destination, loaded);
        var restored = mapper.CreateSnapshot(destination);

        Assert.Multiple(() =>
        {
            Assert.That(
                loaded.Format,
                Is.EqualTo(MacroConfigurationFormat.Current));
            Assert.That(restored.Name, Is.EqualTo("Destination"));
            Assert.That(
                restored.Description,
                Is.EqualTo(snapshot.Description));
            Assert.That(restored.Hotkey, Is.EqualTo(snapshot.Hotkey));
            Assert.That(
                restored.SpellRotation,
                Is.EqualTo(snapshot.SpellRotation));
            Assert.That(restored.Skills, Is.EqualTo(snapshot.Skills));
            Assert.That(restored.Spells, Is.EqualTo(snapshot.Spells));
            Assert.That(restored.Flowers, Is.EqualTo(snapshot.Flowers));
            Assert.That(
                restored.FlowerOptions,
                Is.EqualTo(snapshot.FlowerOptions));
            Assert.That(
                destination.QueuedSpells,
                Has.All.Property(nameof(SpellQueueItemViewModel.IsUndefined))
                    .True);
            Assert.That(
                restored.Spells.Single(
                    entry => entry.Name == "Untargeted Spell")
                    .Target.Offset,
                Is.EqualTo(RuntimeTargetOffset.Zero));
        });
    }

    [Test]
    public void ShouldImportUnknownLegacyEntriesWithoutDiscardingThem()
    {
        const string legacyXml = """
            <MacroState Version="4.11">
              <Name>Legacy</Name>
              <Skills>
                <Skill Name="Unknown Skill" />
              </Skills>
              <SpellRotation>Singular</SpellRotation>
              <Spells>
                <Spell Name="Unknown Spell"
                       Mode="Self"
                       TargetLevel="12" />
              </Spells>
              <Flowering>
                <Flower Mode="Character"
                        Target="Target"
                        HasInterval="False"
                        IfManaLessThan="400" />
              </Flowering>
            </MacroState>
            """;
        using var reader = new StringReader(legacyXml);
        var loaded = MacroConfigurationSerializer.Load(reader);
        var player = CreatePlayer("Destination");
        var destination = new ClientMacroConfiguration(player);
        var mapper = new ClientMacroConfigurationMapper();

        mapper.Apply(destination, loaded);

        Assert.Multiple(() =>
        {
            Assert.That(
                destination.Skills.Select(entry => entry.Name),
                Is.EqualTo(new[] { "Unknown Skill" }));
            Assert.That(
                destination.QueuedSpells.Select(entry => entry.Name),
                Is.EqualTo(new[] { "Unknown Spell" }));
            Assert.That(
                destination.QueuedSpells.Single().TargetLevel,
                Is.EqualTo(12));
            Assert.That(
                destination.FlowerTargets.Single().ManaThreshold,
                Is.EqualTo(400));
            Assert.That(
                destination.SpellQueueRotation,
                Is.EqualTo(SpellRotationMode.Singular));
        });
    }

    private static ClientSession CreatePlayer(string name) =>
        new(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1),
                WindowTitle = "Test Window"
            })
        {
            Name = name
        };
}
