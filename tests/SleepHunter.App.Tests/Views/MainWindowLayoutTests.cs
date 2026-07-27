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

    [Test]
    public void ShouldCaptureHotkeysFromFocusableClientRows()
    {
        var document = XDocument.Load(FindMainWindowFile());
        var window = document.Root!;
        var clientList = document
            .Descendants(Presentation + "ListBox")
            .Single(element =>
                (string?)element.Attribute("Name") ==
                "clientListBox");
        var clientItemStyle = clientList
            .Descendants(Presentation + "Style")
            .Single(element =>
                (string?)element.Attribute("TargetType") ==
                "ListBoxItem");
        var keyHandler = clientItemStyle
            .Elements(Presentation + "EventSetter")
            .Single(element =>
                (string?)element.Attribute("Event") ==
                "KeyDown");

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)window.Attribute(
                    "SourceInitialized"),
                Is.EqualTo(
                    "Window_SourceInitialized"));
            Assert.That(
                (string?)keyHandler.Attribute(
                    "Handler"),
                Is.EqualTo(
                    "clientListBox_KeyDown"));
            Assert.That(
                clientItemStyle
                    .Elements(Presentation + "Setter")
                    .Any(element =>
                        (string?)element.Attribute(
                            "Property") ==
                        "Focusable" &&
                        (string?)element.Attribute(
                            "Value") ==
                        "True"),
                Is.True);
            Assert.That(
                clientItemStyle
                    .Elements(Presentation + "Setter")
                    .Any(element =>
                        (string?)element.Attribute(
                            "Property") ==
                        "IsTabStop" &&
                        (string?)element.Attribute(
                            "Value") ==
                        "True"),
                Is.True);
        });
    }

    [Test]
    public void ShouldBindClientPresentationStateToRuntimeBackedViewModels()
    {
        var document = XDocument.Load(FindMainWindowFile());
        var window = document.Root!;
        var tabs = document
            .Descendants(Presentation + "TabControl")
            .Single(element =>
                (string?)element.Attribute("Name") ==
                "tabControl");
        var inventory = FindNamedElement(
            document,
            "TabItem",
            "inventoryTab");
        var flower = FindNamedElement(
            document,
            "TabItem",
            "flowerTab");
        var alternateCharacters = FindNamedElement(
            document,
            "CheckBox",
            "flowerAlternateCharactersCheckBox");
        var vineyard = FindNamedElement(
            document,
            "CheckBox",
            "flowerVineyardCheckBox");

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)window.Attribute("Title"),
                Is.EqualTo("{Binding WindowTitle}"));
            Assert.That(
                (string?)tabs.Attribute("SelectedIndex"),
                Does.Contain(
                    "SelectedClient.Session.SelectedTabIndex"));
            Assert.That(
                tabs.Attribute("SelectionChanged"),
                Is.Null);
            Assert.That(
                (string?)inventory.Attribute("IsEnabled"),
                Is.EqualTo(
                    "{Binding SelectedClient.IsLoggedIn}"));
            Assert.That(
                (string?)flower.Attribute("IsEnabled"),
                Is.EqualTo(
                    "{Binding SelectedClient.CanFlower}"));
            Assert.That(
                (string?)flower.Attribute("Visibility"),
                Does.Contain(
                    "SelectedClient.SupportsFlowering"));
            Assert.That(
                (string?)alternateCharacters.Attribute(
                    "IsEnabled"),
                Is.EqualTo(
                    "{Binding SelectedClient.HasLyliacPlant}"));
            Assert.That(
                (string?)vineyard.Attribute("IsEnabled"),
                Is.EqualTo(
                    "{Binding SelectedClient.HasLyliacVineyard}"));
        });
    }

    private static XElement FindNamedElement(
        XDocument document,
        string elementName,
        string name) =>
        document
            .Descendants(Presentation + elementName)
            .Single(element =>
                (string?)element.Attribute("Name") == name);

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
