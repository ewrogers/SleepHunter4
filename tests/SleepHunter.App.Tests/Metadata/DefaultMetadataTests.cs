using System.Xml.Linq;

namespace SleepHunter.Tests.Metadata;

public sealed class DefaultMetadataTests
{
    private static readonly string[] LevelElevenCasterStaves =
    [
        "Holy Apollo",
        "Holy Diana",
        "Holy Gaea",
        "Holy Hermes",
        "Holy Kronos",
        "Holy Zeus",
        "Magus Apollo",
        "Magus Ares",
        "Magus Diana",
        "Magus Gaea",
        "Magus Kronos"
    ];

    [Test]
    public void ShouldRequireLevelElevenForEarlyPriestAndWizardStaves()
    {
        var document = XDocument.Load(FindDataFile("Staves.xml"));
        var staves = document
            .Descendants("Staff")
            .ToDictionary(
                staff => (string)staff.Attribute("Name")!,
                StringComparer.OrdinalIgnoreCase);

        Assert.Multiple(() =>
        {
            foreach (var staffName in LevelElevenCasterStaves)
            {
                Assert.That(
                    (int?)staves[staffName].Attribute("Level"),
                    Is.EqualTo(11),
                    staffName);
            }

            Assert.That(
                staves.Values
                    .Where(staff =>
                        (string?)staff.Attribute("Class") is
                            "Priest" or "Wizard")
                    .Select(staff => (int?)staff.Attribute("Level")),
                Has.None.EqualTo(19));
        });
    }

    [Test]
    public void ShouldNotMarkInstrumentalAttackRanksAsAssails()
    {
        var document = XDocument.Load(FindDataFile("Skills.xml"));
        var instrumentalAttacks = document
            .Descendants("Skill")
            .Where(skill =>
                ((string?)skill.Attribute("Name"))?
                    .StartsWith(
                        "Instrumental Attack ",
                        StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(instrumentalAttacks, Has.Length.EqualTo(12));
            Assert.That(
                instrumentalAttacks.Select(skill =>
                    (bool?)skill.Attribute("IsAssail") ?? false),
                Is.All.False);
        });
    }

    private static string FindDataFile(string fileName)
    {
        var directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "data",
                fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate data/{fileName} from the test directory.");
    }
}
