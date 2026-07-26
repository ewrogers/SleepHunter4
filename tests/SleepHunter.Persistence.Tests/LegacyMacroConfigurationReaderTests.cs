using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Persistence.Tests;

public sealed class LegacyMacroConfigurationReaderTests
{
    private static readonly long[] ExpectedSkillIds = [1, 2];
    private static readonly string[] ExpectedSkillNames = ["Assail", "Rescue"];
    private static readonly long[] ExpectedSpellIds = [1, 2, 3, 4];

    [Test]
    public void ShouldImportLegacyMacroWithStableIdsAndCompleteTargets()
    {
        const string xml = """
            <MacroState Version="4.10">
              <Name>Legacy Macro</Name>
              <Description>Imported configuration</Description>
              <Hotkey Key="F6" Modifiers="Control, Shift" />
              <Skills>
                <Skill Name="Assail" />
                <Skill Name="Rescue" />
                <Skill Name="assail" />
              </Skills>
              <SpellRotation>RoundRobin</SpellRotation>
              <Spells>
                <Spell Name="self" Mode="Self" OffsetX="1" OffsetY="-2" />
                <Spell Name="screen" Mode="AbsoluteXY" X="315" Y="160"
                       OffsetX="3" OffsetY="4" />
                <Spell Name="area" Mode="RelativeRadius" X="-1" Y="2"
                       OffsetX="5" OffsetY="-6" InnerRadius="1"
                       OuterRadius="3" TargetLevel="50" />
                <Spell Name="absolute" Mode="AbsoluteRadius" X="50" Y="60"
                       InnerRadius="0" OuterRadius="2" />
              </Spells>
              <UseLyliacVineyard>true</UseLyliacVineyard>
              <FlowerAlternateCharacters>true</FlowerAlternateCharacters>
              <Flowering>
                <Flower Mode="Character" Target="Alt" HasInterval="false"
                        IfManaLessThan="500" OffsetX="7" OffsetY="8" />
                <Flower Mode="RelativeRadius" X="0" Y="0" InnerRadius="0"
                        OuterRadius="1" HasInterval="true" Interval="1.5" />
                <Flower Mode="Self" HasInterval="false" Interval="2" />
                <Flower Mode="None" HasInterval="false" />
              </Flowering>
              <LocalStorage>
                <Entry Key="UseWaterAndBeds" Value="true" />
              </LocalStorage>
            </MacroState>
            """;

        var result = MacroConfigurationSerializer.Load(
            new StringReader(xml));
        var configuration = result.Configuration;

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Format,
                Is.EqualTo(MacroConfigurationFormat.LegacyV4));
            Assert.That(result.SourceVersion, Is.EqualTo("4.10"));
            Assert.That(configuration.Name, Is.EqualTo("Legacy Macro"));
            Assert.That(
                configuration.Hotkey,
                Is.EqualTo(new HotkeyConfiguration(
                    "F6",
                    HotkeyModifiers.Control | HotkeyModifiers.Shift)));
            Assert.That(
                configuration.Skills.Select(entry => entry.Id.Value),
                Is.EqualTo(ExpectedSkillIds));
            Assert.That(
                configuration.Skills.Select(entry => entry.Name),
                Is.EqualTo(ExpectedSkillNames));
            Assert.That(
                configuration.SpellRotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(
                configuration.Spells.Select(entry => entry.Id.Value),
                Is.EqualTo(ExpectedSpellIds));
            Assert.That(
                configuration.Spells[0].Target,
                Is.EqualTo(SpellTarget.Self.WithOffset(1, -2)));
            Assert.That(
                configuration.Spells[1].Target,
                Is.EqualTo(SpellTarget.ScreenPoint(
                    315,
                    160,
                    new TargetOffset(3, 4))));
            Assert.That(
                configuration.Spells[2].Target,
                Is.EqualTo(SpellTarget.RelativeArea(
                    -1,
                    2,
                    1,
                    3,
                    new TargetOffset(5, -6))));
            Assert.That(configuration.Spells[2].TargetLevel, Is.EqualTo(50));
            Assert.That(
                configuration.Spells[3].Target,
                Is.EqualTo(SpellTarget.AbsoluteArea(50, 60, 0, 2)));
            Assert.That(configuration.Flowers, Has.Length.EqualTo(3));
            Assert.That(
                configuration.Flowers[0].Target,
                Is.EqualTo(SpellTarget.Character(
                    "Alt",
                    new TargetOffset(7, 8))));
            Assert.That(
                configuration.Flowers[0].ManaThreshold,
                Is.EqualTo(500));
            Assert.That(
                configuration.Flowers[1].Interval,
                Is.EqualTo(TimeSpan.FromSeconds(1.5)));
            Assert.That(
                configuration.Flowers[1].Target,
                Is.EqualTo(SpellTarget.RelativeArea(0, 0, 0, 1)));
            Assert.That(
                configuration.Flowers[2].Interval,
                Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(
                configuration.Flowers[2].Target,
                Is.EqualTo(SpellTarget.Self));
            Assert.That(configuration.FlowerOptions.UseVineyard, Is.True);
            Assert.That(
                configuration.FlowerOptions.FlowerAlternateCharacters,
                Is.True);
            Assert.That(
                configuration.FlowerOptions.PrioritizeAlternateCharacters,
                Is.True);
            Assert.That(
                result.Warnings.Select(warning => warning.Code),
                Does.Contain("legacy-skill-duplicate"));
            Assert.That(
                result.Warnings.Count(warning =>
                    warning.Code == "legacy-radius-modernized"),
                Is.EqualTo(3));
            Assert.That(
                result.Warnings.Select(warning => warning.Code),
                Does.Contain("legacy-flower-skipped"));
            Assert.That(
                result.Warnings.Select(warning => warning.Code),
                Does.Contain("legacy-interval-recovered"));
            Assert.That(
                result.Warnings.Select(warning => warning.Code),
                Does.Contain("legacy-local-storage-ignored"));
        });
    }

    [Test]
    public void ShouldMapLegacyRotationsAndPreserveDefaultResolution()
    {
        const string defaultXml = """
            <MacroState Version="4.10">
              <SpellRotation>Default</SpellRotation>
              <Spells>
                <Spell Name="spell" Mode="Self" />
              </Spells>
            </MacroState>
            """;
        const string noneXml = """
            <MacroState Version="4.10">
              <SpellRotation>None</SpellRotation>
            </MacroState>
            """;
        const string singularXml = """
            <MacroState Version="4.10">
              <SpellRotation>Singular</SpellRotation>
            </MacroState>
            """;

        var defaultResult = Load(defaultXml);
        var noneResult = Load(noneXml);
        var singularResult = Load(singularXml);

        Assert.Multiple(() =>
        {
            Assert.That(defaultResult.Configuration.SpellRotation, Is.Null);
            Assert.That(
                defaultResult.Configuration
                    .CreateSpellQueue(SpellQueueRotation.RoundRobin)
                    .Rotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(
                noneResult.Configuration.SpellRotation,
                Is.EqualTo(SpellQueueRotation.Priority));
            Assert.That(
                noneResult.Warnings.Select(warning => warning.Code),
                Does.Contain("legacy-rotation-mapped"));
            Assert.That(
                singularResult.Configuration.SpellRotation,
                Is.EqualTo(SpellQueueRotation.Sequential));
        });
    }

    [Test]
    public void ShouldImportEmptyRadiusAsCenterAndReportMigration()
    {
        const string xml = """
            <MacroState Version="4.10">
              <Spells>
                <Spell Name="relative" Mode="RelativeRadius" X="-2" Y="3"
                       InnerRadius="0" OuterRadius="0" />
                <Spell Name="absolute" Mode="AbsoluteRadius" X="20" Y="30"
                       InnerRadius="4" OuterRadius="2" />
              </Spells>
            </MacroState>
            """;

        var result = Load(xml);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Configuration.Spells[0].Target,
                Is.EqualTo(SpellTarget.RelativeTile(-2, 3)));
            Assert.That(
                result.Configuration.Spells[1].Target,
                Is.EqualTo(SpellTarget.AbsoluteTile(20, 30)));
            Assert.That(
                result.Warnings.Count(warning =>
                    warning.Code == "legacy-radius-single-point"),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldRejectFractionalLegacyCoordinates()
    {
        const string xml = """
            <MacroState Version="4.10">
              <Spells>
                <Spell Name="spell" Mode="RelativeTile" X="1.5" Y="0" />
              </Spells>
            </MacroState>
            """;

        Assert.That(
            () => Load(xml),
            Throws.TypeOf<MacroConfigurationException>()
                .With.Message.Contains("integer coordinate"));
    }

    private static MacroConfigurationLoadResult Load(string xml) =>
        MacroConfigurationSerializer.Load(new StringReader(xml));
}
