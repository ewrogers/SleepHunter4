using System.Xml.Serialization;

using SleepHunter.Macro;
using SleepHunter.Services.Serialization;

namespace SleepHunter.Tests.Serialization;

public sealed class MacroStateCompatibilityTests
{
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
