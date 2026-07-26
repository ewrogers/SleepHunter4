using System.Xml.Linq;

namespace SleepHunter.Tests.Views;

public sealed class SpellQueueDataTemplateTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [TestCase("SpellQueueDataTemplate.xaml")]
    [TestCase("SpellTargetDataTemplates.xaml")]
    public void ShouldUseWhiteNamesAndTheSkillToggleActiveHighlight(
        string fileName)
    {
        var document = XDocument.Load(FindTemplateFile(fileName));
        var nameText = document
            .Descendants(Presentation + "TextBlock")
            .Single(element =>
                (string?)element.Attribute("Name") == "NameText");
        var selectionTriggers = document
            .Descendants(Presentation + "DataTrigger")
            .Where(trigger =>
                ((string?)trigger.Attribute("Binding"))?
                    .Contains(
                        "Path=IsSelected",
                        StringComparison.Ordinal) == true)
            .ToArray();
        var activeTrigger = document
            .Descendants(Presentation + "DataTrigger")
            .Single(trigger =>
                (string?)trigger.Attribute("Binding") ==
                    "{Binding IsActive}" &&
                (string?)trigger.Attribute("Value") == "True");
        var activeSetters = activeTrigger
            .Elements(Presentation + "Setter")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)nameText.Attribute("Foreground"),
                Is.EqualTo(
                    "{DynamicResource ObsidianForeground}"));
            Assert.That(
                selectionTriggers.SelectMany(trigger =>
                    trigger.Elements(Presentation + "Setter")),
                Has.None.Matches<XElement>(setter =>
                    (string?)setter.Attribute("TargetName") ==
                        "NameText" &&
                    (string?)setter.Attribute("Property") ==
                        "Foreground"));
            Assert.That(
                GetSetterValue(
                    activeSetters,
                    "Icon",
                    "Opacity"),
                Is.EqualTo("1"));
            Assert.That(
                GetSetterValue(
                    activeSetters,
                    "IconBorder",
                    "BorderBrush"),
                Is.EqualTo(
                    "{DynamicResource ObsidianBackground}"));
            Assert.That(
                GetSetterValue(
                    activeSetters,
                    "IconBorder",
                    "Background"),
                Is.EqualTo(
                    "{StaticResource ObsidianSeparatorColor}"));
            Assert.That(
                GetSetterValue(
                    activeSetters,
                    "IconBorder",
                    "BorderThickness"),
                Is.EqualTo("3"));
            Assert.That(
                GetSetterValue(
                    activeSetters,
                    "IconBorder",
                    "CornerRadius"),
                Is.EqualTo("3"));
            Assert.That(
                GetSetterValue(
                    activeSetters,
                    "IconBorder",
                    "Margin"),
                Is.EqualTo("5,2"));
        });
    }

    private static string? GetSetterValue(
        IEnumerable<XElement> setters,
        string targetName,
        string property) =>
        (string?)setters.Single(setter =>
            (string?)setter.Attribute("TargetName") == targetName &&
            (string?)setter.Attribute("Property") == property)
            .Attribute("Value");

    private static string FindTemplateFile(string fileName)
    {
        var directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "SleepHunter.App",
                "Templates",
                fileName);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate the spell queue template '{fileName}'.");
    }
}
