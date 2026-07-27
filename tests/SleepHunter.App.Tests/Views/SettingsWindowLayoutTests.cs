using System.Xml.Linq;

namespace SleepHunter.Tests.Views;

public sealed class SettingsWindowLayoutTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Test]
    public void ShouldPlaceActiveEffectsSettingBelowSpellLevels()
    {
        var document = XDocument.Load(FindSettingsWindowFile());
        var window = document.Root!;
        var showSpellLevels = FindNamedElement(
            document,
            "showSpellLevelsCheckBox");
        var showActiveEffects = FindNamedElement(
            document,
            "showActiveEffectsCheckBox");
        var optionsStack = showActiveEffects.Parent;
        var binding = (string?)showActiveEffects.Attribute(
            "IsChecked");

        Assert.Multiple(() =>
        {
            Assert.That(
                showSpellLevels.Name,
                Is.EqualTo(Presentation + "CheckBox"));
            Assert.That(
                showActiveEffects.Name,
                Is.EqualTo(Presentation + "CheckBox"));
            Assert.That(
                optionsStack?.Name,
                Is.EqualTo(Presentation + "StackPanel"));
            Assert.That(
                (int?)optionsStack?.Attribute(
                    "Grid.Row"),
                Is.EqualTo(14));
            Assert.That(
                (int?)optionsStack?.Attribute(
                    "Grid.Column"),
                Is.EqualTo(1));
            Assert.That(
                showSpellLevels.ElementsAfterSelf().FirstOrDefault(),
                Is.SameAs(showActiveEffects));
            Assert.That(
                binding,
                Does.Contain("Settings.ShowActiveEffects"));
            Assert.That(
                (string?)showActiveEffects.Attribute("ToolTip"),
                Is.EqualTo(
                    "Show active effects in the character list."));
            Assert.That(
                (int?)window.Attribute("Height"),
                Is.EqualTo(660));
            Assert.That(
                showActiveEffects.Value.Trim(),
                Is.EqualTo("Show Active Effects"));
        });
    }

    private static XElement FindNamedElement(
        XDocument document,
        string name) =>
        document
            .Descendants()
            .Single(element =>
                (string?)element.Attribute("Name") == name);

    private static string FindSettingsWindowFile()
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
                "SettingsWindow.xaml");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate SettingsWindow.xaml " +
            "from the test directory.");
    }
}
