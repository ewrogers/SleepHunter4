using System.Xml.Linq;

namespace SleepHunter.Tests.Views;

public sealed class TargetWindowLayoutTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [TestCase("SpellTargetWindow.xaml")]
    [TestCase("FlowerTargetWindow.xaml")]
    public void ShouldSelectAlternateCharactersAsStringItems(
        string windowFile)
    {
        var document = XDocument.Load(
            FindWindowFile(windowFile));
        var characterSelector = document
            .Descendants(Presentation + "ComboBox")
            .Single(element =>
                (string?)element.Attribute("Name") ==
                "characterComboBox");

        Assert.Multiple(() =>
        {
            Assert.That(
                characterSelector.Attribute(
                    "SelectedValuePath"),
                Is.Null);
            Assert.That(
                (string?)characterSelector.Attribute(
                    "SelectedIndex"),
                Is.EqualTo("0"));
        });
    }

    private static string FindWindowFile(
        string windowFile)
    {
        var directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "SleepHunter.App",
                "Views",
                windowFile);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate {windowFile} " +
            "from the test directory.");
    }
}
