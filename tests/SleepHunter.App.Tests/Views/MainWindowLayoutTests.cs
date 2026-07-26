using System.Xml.Linq;

namespace SleepHunter.Tests.Views;

public sealed class MainWindowLayoutTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Test]
    public void ShouldShowCenteredGuidanceOnlyForAnEmptyVisibleSpellQueue()
    {
        var document = XDocument.Load(FindMainWindowFile());
        var placeholder = document
            .Descendants(Presentation + "TextBlock")
            .Single(element =>
                (string?)element.Attribute("Name") ==
                "EmptySpellQueuePlaceholder");
        var trigger = placeholder
            .Descendants(Presentation + "MultiDataTrigger")
            .Single();
        var conditions = trigger
            .Descendants(Presentation + "Condition")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)placeholder.Attribute("Text"),
                Is.EqualTo(
                    "Double click a spell to add to queue"));
            Assert.That(
                (string?)placeholder.Attribute(
                    "HorizontalAlignment"),
                Is.EqualTo("Center"));
            Assert.That(
                (string?)placeholder.Attribute(
                    "VerticalAlignment"),
                Is.EqualTo("Center"));
            Assert.That(
                (string?)placeholder.Attribute("Opacity"),
                Is.EqualTo("0.65"));
            Assert.That(
                HasCondition(
                    conditions,
                    "Path=Visibility",
                    "Visible"),
                Is.True);
            Assert.That(
                HasCondition(
                    conditions,
                    "Path=HasItems",
                    "False"),
                Is.True);
        });
    }

    [Test]
    public void ShouldInsetTheAccentSpellQueueHeader()
    {
        var document = XDocument.Load(FindMainWindowFile());
        var inset = document
            .Descendants(Presentation + "Border")
            .Single(element =>
                (string?)element.Attribute("Name") ==
                "SpellQueueHeaderAccentInset");

        Assert.That(
            (string?)inset.Attribute("Style"),
            Is.EqualTo(
                "{StaticResource ObsidianAccentInsetBorder}"));
    }

    [Test]
    public void ShouldDescribeAnEmptyClientListInTheStatusBar()
    {
        var document = XDocument.Load(FindMainWindowFile());
        var statusText = document
            .Descendants(Presentation + "TextBlock")
            .Single(element =>
                ((string?)element.Attribute("Text"))?
                    .Contains(
                        "SelectedClient.RuntimeStatus",
                        StringComparison.Ordinal) == true);

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)statusText.Attribute("Text"),
                Does.Contain("FallbackValue='No clients'"));
            Assert.That(
                (string?)statusText.Attribute("Text"),
                Does.Contain("TargetNullValue='No clients'"));
            Assert.That(
                (string?)statusText.Attribute("ToolTip"),
                Does.Contain("FallbackValue='No clients'"));
            Assert.That(
                (string?)statusText.Attribute("ToolTip"),
                Does.Contain("TargetNullValue='No clients'"));
        });
    }

    private static bool HasCondition(
        IEnumerable<XElement> conditions,
        string bindingFragment,
        string value) =>
        conditions.Any(condition =>
            ((string?)condition.Attribute("Binding"))?
                .Contains(
                    bindingFragment,
                    StringComparison.Ordinal) == true &&
            (string?)condition.Attribute("Value") == value);

    private static string FindMainWindowFile()
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
                "MainWindow.xaml");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate the main window XAML from the test directory.");
    }
}
