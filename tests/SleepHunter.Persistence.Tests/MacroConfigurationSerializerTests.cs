using System.Collections.Immutable;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Persistence.Tests;

public sealed class MacroConfigurationSerializerTests
{
    [Test]
    public void ShouldRoundTripCurrentConfigurationWithoutLosingTargets()
    {
        var configuration = new MacroConfiguration(
            name: "Test Macro",
            description: "Round-trip coverage",
            hotkey: new HotkeyConfiguration(
                "F6",
                HotkeyModifiers.Control | HotkeyModifiers.Shift),
            spellRotation: SpellQueueRotation.RoundRobin,
            skills:
            [
                new SkillQueueEntry(new SkillQueueEntryId(11), "Assail"),
                new SkillQueueEntry(new SkillQueueEntryId(12), "Rescue")
            ],
            spells:
            [
                Spell(21, "none", SpellTarget.None),
                Spell(
                    22,
                    "self",
                    SpellTarget.Self.WithOffset(1, -2)),
                Spell(
                    23,
                    "character",
                    SpellTarget.Character(
                        "Alt",
                        new TargetOffset(3, 4))),
                Spell(
                    24,
                    "relative",
                    SpellTarget.RelativeTile(
                        -2,
                        5,
                        new TargetOffset(-6, 7))),
                Spell(
                    25,
                    "absolute",
                    SpellTarget.AbsoluteTile(
                        100,
                        200,
                        new TargetOffset(8, -9))),
                Spell(
                    26,
                    "screen",
                    SpellTarget.ScreenPoint(
                        315,
                        160,
                        new TargetOffset(10, 11))),
                Spell(
                    27,
                    "relative area",
                    SpellTarget.RelativeArea(
                        0,
                        1,
                        1,
                        3,
                        new TargetOffset(-12, 13))),
                new SpellQueueEntry(
                    new SpellQueueEntryId(28),
                    "absolute area",
                    targetLevel: 99,
                    SpellTarget.AbsoluteArea(
                        50,
                        60,
                        0,
                        2,
                        new TargetOffset(14, -15)),
                    new HealthCondition(
                        minimumPercentExclusive: 12.5,
                        maximumPercentInclusive: 87.25))
            ],
            flowers:
            [
                new FlowerQueueEntry(
                    new FlowerQueueEntryId(31),
                    SpellTarget.Self,
                    interval: TimeSpan.FromTicks(1234567)),
                new FlowerQueueEntry(
                    new FlowerQueueEntryId(32),
                    SpellTarget.Character("Alt"),
                    manaThreshold: 500),
                new FlowerQueueEntry(
                    new FlowerQueueEntryId(33),
                    SpellTarget.AbsoluteArea(50, 50, 1, 2),
                    interval: TimeSpan.Zero)
            ],
            flowerOptions: new FlowerOptions(
                useVineyard: true,
                flowerAlternateCharacters: true,
                prioritizeAlternateCharacters: false,
                maximumXDistance: 8,
                maximumYDistance: 9));
        using var writer = new StringWriter();

        MacroConfigurationSerializer.Save(configuration, writer);
        var result = MacroConfigurationSerializer.Load(
            new StringReader(writer.ToString()));
        using var secondWriter = new StringWriter();
        MacroConfigurationSerializer.Save(
            result.Configuration,
            secondWriter);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Format,
                Is.EqualTo(MacroConfigurationFormat.Current));
            Assert.That(
                result.SourceVersion,
                Is.EqualTo(MacroConfigurationSerializer.CurrentVersion));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Configuration.Name, Is.EqualTo(configuration.Name));
            Assert.That(
                result.Configuration.Description,
                Is.EqualTo(configuration.Description));
            Assert.That(
                result.Configuration.Hotkey,
                Is.EqualTo(configuration.Hotkey));
            Assert.That(
                result.Configuration.SpellRotation,
                Is.EqualTo(configuration.SpellRotation));
            Assert.That(
                result.Configuration.Skills,
                Is.EqualTo(configuration.Skills));
            Assert.That(
                result.Configuration.Spells,
                Is.EqualTo(configuration.Spells));
            Assert.That(
                result.Configuration.Flowers,
                Is.EqualTo(configuration.Flowers));
            Assert.That(
                result.Configuration.FlowerOptions,
                Is.EqualTo(configuration.FlowerOptions));
            Assert.That(
                result.Configuration.CreateSpellQueue().Entries,
                Is.EqualTo(configuration.Spells));
            Assert.That(
                result.Configuration.CreateSkillQueue().Entries,
                Is.EqualTo(configuration.Skills));
            Assert.That(
                result.Configuration.CreateFlowerQueue().Entries,
                Is.EqualTo(configuration.Flowers));
            Assert.That(secondWriter.ToString(), Is.EqualTo(writer.ToString()));
            Assert.That(
                writer.ToString().TrimStart(),
                Does.StartWith("{"));
            Assert.That(
                writer.ToString(),
                Does.Contain(
                    "\"format\": \"SleepHunter.MacroConfiguration\""));
        });
    }

    [Test]
    public void ShouldPreserveDefaultRotationForCallerResolution()
    {
        var configuration = new MacroConfiguration(
            spells:
            [
                Spell(1, "spell", SpellTarget.Self)
            ]);
        using var writer = new StringWriter();
        MacroConfigurationSerializer.Save(configuration, writer);

        var loaded = MacroConfigurationSerializer.Load(
            new StringReader(writer.ToString())).Configuration;

        Assert.Multiple(() =>
        {
            Assert.That(loaded.SpellRotation, Is.Null);
            Assert.That(
                loaded.CreateSpellQueue(SpellQueueRotation.Sequential).Rotation,
                Is.EqualTo(SpellQueueRotation.Sequential));
        });
    }

    [Test]
    public void ShouldRejectUnsupportedVersionAndDocumentTypeDeclarations()
    {
        const string unsupported = """
            {
              "format": "SleepHunter.MacroConfiguration",
              "version": "99",
              "metadata": {},
              "skills": [],
              "spells": [],
              "flowering": {
                "queue": []
              }
            }
            """;
        const string dtd =
            "<!DOCTYPE MacroState [<!ENTITY x \"value\">]>" +
            "<MacroState Version=\"4.11\"><Name>&x;</Name>" +
            "</MacroState>";

        Assert.Multiple(() =>
        {
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(unsupported)),
                Throws.TypeOf<MacroConfigurationException>()
                    .With.Message.Contains("Unsupported"));
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(dtd)),
                Throws.TypeOf<MacroConfigurationException>()
                    .With.Message.Contains("DTD"));
        });
    }

    [Test]
    public void ShouldRejectUnknownCommentedOrNullJsonEntries()
    {
        const string unknown = """
            {
              "format": "SleepHunter.MacroConfiguration",
              "version": "1",
              "metadata": {},
              "skills": [],
              "spells": [],
              "flowering": {
                "queue": []
              },
              "unexpected": true
            }
            """;
        const string commented = """
            {
              "format": "SleepHunter.MacroConfiguration",
              "version": "1",
              // Comments are not part of the schema.
              "metadata": {},
              "skills": [],
              "spells": [],
              "flowering": {
                "queue": []
              }
            }
            """;
        const string duplicated = """
            {
              "format": "SleepHunter.MacroConfiguration",
              "format": "SleepHunter.MacroConfiguration",
              "version": "1",
              "metadata": {},
              "skills": [],
              "spells": [],
              "flowering": {
                "queue": []
              }
            }
            """;
        const string nullEntry = """
            {
              "format": "SleepHunter.MacroConfiguration",
              "version": "1",
              "metadata": {},
              "skills": [null],
              "spells": [],
              "flowering": {
                "queue": []
              }
            }
            """;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(unknown)),
                Throws.TypeOf<MacroConfigurationException>());
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(commented)),
                Throws.TypeOf<MacroConfigurationException>());
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(duplicated)),
                Throws.TypeOf<MacroConfigurationException>());
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(nullEntry)),
                Throws.TypeOf<MacroConfigurationException>()
                    .With.Message.Contains("null entries"));
        });
    }

    [Test]
    public void ShouldAcceptUtf8BomAndRejectCurrentXmlOrOversizedInput()
    {
        using var writer = new StringWriter();
        MacroConfigurationSerializer.Save(
            new MacroConfiguration(name: "JSON"),
            writer);
        var withBom = $"\uFEFF{writer}";
        const string retiredCurrentXml =
            "<MacroConfiguration Version=\"1\" />";
        var oversized = new string(
            ' ',
            (4 * 1024 * 1024) + 1);

        var loaded = MacroConfigurationSerializer.Load(
            new StringReader(withBom));

        Assert.Multiple(() =>
        {
            Assert.That(
                loaded.Configuration.Name,
                Is.EqualTo("JSON"));
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(retiredCurrentXml)),
                Throws.TypeOf<MacroConfigurationException>()
                    .With.Message.Contains("Unsupported"));
            Assert.That(
                () => MacroConfigurationSerializer.Load(
                    new StringReader(oversized)),
                Throws.TypeOf<MacroConfigurationException>()
                    .With.Message.Contains("cannot exceed"));
        });
    }

    [Test]
    public void ShouldRejectAmbiguousConfigurationEntries()
    {
        var duplicateSkillIds = new[]
        {
            new SkillQueueEntry(new SkillQueueEntryId(1), "first"),
            new SkillQueueEntry(new SkillQueueEntryId(1), "second")
        }.ToImmutableArray();
        var duplicateSkillNames = new[]
        {
            new SkillQueueEntry(new SkillQueueEntryId(1), "same"),
            new SkillQueueEntry(new SkillQueueEntryId(2), "SAME")
        }.ToImmutableArray();
        var duplicateSpellIds = new[]
        {
            Spell(1, "first", SpellTarget.Self),
            Spell(1, "second", SpellTarget.Self)
        }.ToImmutableArray();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new MacroConfiguration(skills: duplicateSkillIds));
            Assert.Throws<ArgumentException>(
                () => _ = new MacroConfiguration(
                    skills: duplicateSkillNames));
            Assert.Throws<ArgumentException>(
                () => _ = new MacroConfiguration(spells: duplicateSpellIds));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MacroConfiguration(
                    spellRotation: (SpellQueueRotation)99));
        });
    }

    [Test]
    public void ShouldSaveAndReplaceConfigurationFile()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"SleepHunter.Persistence.Tests.{Guid.NewGuid():N}");
        var filePath = Path.Combine(
            directory,
            $"test{MacroConfigurationSerializer.CurrentFileExtension}");

        try
        {
            MacroConfigurationSerializer.Save(
                new MacroConfiguration(name: "first"),
                filePath);
            MacroConfigurationSerializer.Save(
                new MacroConfiguration(name: "second"),
                filePath);

            var result = MacroConfigurationSerializer.Load(filePath);

            Assert.Multiple(() =>
            {
                Assert.That(result.Configuration.Name, Is.EqualTo("second"));
                Assert.That(
                    Directory.EnumerateFiles(directory),
                    Is.EqualTo(new[] { filePath }));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void ShouldWriteJsonRegardlessOfRequestedFileExtension()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"SleepHunter.Persistence.Tests.{Guid.NewGuid():N}");
        var filePath = Path.Combine(
            directory,
            $"requested{MacroConfigurationSerializer.LegacyFileExtension}");

        try
        {
            MacroConfigurationSerializer.Save(
                new MacroConfiguration(name: "current"),
                filePath);

            var document = File.ReadAllText(filePath);
            var loaded = MacroConfigurationSerializer.Load(filePath);

            Assert.Multiple(() =>
            {
                Assert.That(document.TrimStart(), Does.StartWith("{"));
                Assert.That(
                    loaded.Format,
                    Is.EqualTo(MacroConfigurationFormat.Current));
                Assert.That(
                    loaded.Configuration.Name,
                    Is.EqualTo("current"));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void ShouldHonorStreamOwnership()
    {
        var configuration = new MacroConfiguration(name: "stream");
        using var openStream = new MemoryStream();
        MacroConfigurationSerializer.Save(
            configuration,
            openStream,
            leaveOpen: true);
        openStream.Position = 0;
        var loaded = MacroConfigurationSerializer.Load(
            openStream,
            leaveOpen: true);
        var closedSaveStream = new MemoryStream();
        MacroConfigurationSerializer.Save(
            configuration,
            closedSaveStream,
            leaveOpen: false);
        var closedLoadStream = new MemoryStream(openStream.ToArray());
        MacroConfigurationSerializer.Load(
            closedLoadStream,
            leaveOpen: false);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Configuration.Name, Is.EqualTo("stream"));
            Assert.That(openStream.CanRead, Is.True);
            Assert.That(closedSaveStream.CanRead, Is.False);
            Assert.That(closedLoadStream.CanRead, Is.False);
        });
    }

    private static SpellQueueEntry Spell(
        long id,
        string name,
        SpellTarget target) =>
        new(
            new SpellQueueEntryId(id),
            name,
            target: target);
}
