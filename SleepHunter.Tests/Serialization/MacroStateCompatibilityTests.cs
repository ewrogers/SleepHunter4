using System.Xml.Serialization;

using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Services.Serialization;

namespace SleepHunter.Tests.Serialization;

public sealed class MacroStateCompatibilityTests
{
    [Test]
    public void ShouldRoundTripEditableConfigurationThroughLegacyFormat()
    {
        using var player = new Player(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1),
                WindowTitle = "Test Window"
            })
        {
            Name = "Test Character",
            IsLoggedIn = true
        };
        var configuration = new PlayerMacroConfiguration(player)
        {
            SpellQueueRotation = SpellRotationMode.RoundRobin,
            UseLyliacVineyard = true,
            FlowerAlternateCharacters = true
        };
        configuration.AddToSpellQueue(
            new SpellQueueItem
            {
                Name = "Test Spell",
                Target = new SpellTarget
                {
                    Mode = SpellTargetMode.Character,
                    CharacterName = "Target"
                },
                TargetLevel = 12
            });
        var serializer =
            new LegacyMacroConfigurationSerializer();
        using var writer = new StringWriter();

        serializer.Serialize(configuration, writer);
        using var reader = new StringReader(writer.ToString());
        var restored = serializer.Deserialize(reader);

        Assert.Multiple(() =>
        {
            Assert.That(
                restored.Name,
                Is.EqualTo("Test Character"));
            Assert.That(
                restored.SpellRotation,
                Is.EqualTo(SpellRotationMode.RoundRobin));
            Assert.That(restored.UseLyliacVineyard, Is.True);
            Assert.That(
                restored.FlowerAlternateCharacters,
                Is.True);
            Assert.That(restored.Spells, Has.Count.EqualTo(1));
            Assert.That(
                restored.Spells[0].SpellName,
                Is.EqualTo("Test Spell"));
            Assert.That(
                restored.Spells[0].TargetMode,
                Is.EqualTo(SpellTargetMode.Character));
            Assert.That(
                restored.Spells[0].TargetName,
                Is.EqualTo("Target"));
            Assert.That(
                restored.Spells[0].TargetLevel,
                Is.EqualTo(12));
        });
    }

    [Test]
    public void ShouldIgnoreRemovedWaterAndBedsStorageInLegacyMacro()
    {
        const string legacyXml = """
            <MacroState Version="4.10">
              <Name>Test Character</Name>
              <Skills>
                <Skill Name="Assail" />
              </Skills>
              <SpellRotation>RoundRobin</SpellRotation>
              <Spells />
              <Flowering />
              <LocalStorage>
                <Entries>
                  <Entry Key="UseWaterAndBeds.IsEnabled" Value="True" />
                  <Entry Key="UseWaterAndBeds.TileX" Value="5" />
                  <Entry Key="UseWaterAndBeds.TileY" Value="1" />
                </Entries>
              </LocalStorage>
            </MacroState>
            """;
        var serializer = new XmlSerializer(
            typeof(SerializedMacroState),
            string.Empty);
        using var reader = new StringReader(legacyXml);

        var state = (SerializedMacroState)serializer.Deserialize(reader)!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Name, Is.EqualTo("Test Character"));
            Assert.That(
                state.SpellRotation,
                Is.EqualTo(SpellRotationMode.RoundRobin));
            Assert.That(
                state.Skills.Select(skill => skill.SkillName),
                Is.EqualTo(new[] { "Assail" }));
        });
    }

    [Test]
    public void ShouldNotWriteRemovedWaterAndBedsStorage()
    {
        var serializer = new XmlSerializer(
            typeof(SerializedMacroState),
            string.Empty);
        using var writer = new StringWriter();

        serializer.Serialize(
            writer,
            new SerializedMacroState
            {
                Name = "Test Character"
            });
        var xml = writer.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(xml, Does.Not.Contain("LocalStorage"));
            Assert.That(xml, Does.Not.Contain("UseWaterAndBeds"));
        });
    }
}
