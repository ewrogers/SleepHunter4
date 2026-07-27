using System.Xml.Linq;

namespace SleepHunter.Tests.Views;

public sealed class ItemDataTemplateTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [TestCase("InventoryItemDataTemplate.xaml")]
    [TestCase("EquipmentItemDataTemplate.xaml")]
    public void ShouldIntegerScaleLargeItemIcons(
        string templateFile)
    {
        var document = XDocument.Load(
            FindTemplateFile(templateFile));
        var icon = document
            .Descendants(Presentation + "Image")
            .Single(element =>
                (string?)element.Attribute("Name") == "Icon");
        var emptySlot = document
            .Descendants(Presentation + "Rectangle")
            .Single(element =>
                (string?)element.Attribute("Name") ==
                "EmptyRectangle");
        var scaleTrigger = document
            .Descendants(Presentation + "DataTrigger")
            .Single(element =>
                ((string?)element.Attribute("Binding"))?
                    .Contains(
                        "Settings.InventoryIconSize",
                        StringComparison.Ordinal) == true);
        var transformSetter = scaleTrigger
            .Elements(Presentation + "Setter")
            .Single(element =>
                (string?)element.Attribute("TargetName") ==
                    "Icon" &&
                (string?)element.Attribute("Property") ==
                    "RenderTransform");
        var scale = transformSetter
            .Descendants(Presentation + "ScaleTransform")
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)scaleTrigger.Attribute("Value"),
                Is.EqualTo("62"));
            Assert.That(
                (string?)scale.Attribute("ScaleX"),
                Is.EqualTo("2"));
            Assert.That(
                (string?)scale.Attribute("ScaleY"),
                Is.EqualTo("2"));
            Assert.That(
                (string?)icon.Attribute(
                    "RenderOptions.BitmapScalingMode"),
                Is.EqualTo("NearestNeighbor"));
            Assert.That(
                (string?)icon.Attribute(
                    "RenderTransformOrigin"),
                Is.EqualTo("0.5,0.5"));
            Assert.That(
                (string?)icon.Attribute(
                    "SnapsToDevicePixels"),
                Is.EqualTo("True"));
            Assert.That(
                (string?)icon.Parent?.Attribute(
                    "ClipToBounds"),
                Is.EqualTo("True"));
            Assert.That(
                icon.Parent,
                Is.Not.SameAs(emptySlot.Parent));
            Assert.That(
                emptySlot.Parent?.Attribute(
                    "ClipToBounds"),
                Is.Null);
        });
    }

    private static string FindTemplateFile(
        string templateFile)
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
                templateFile);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate {templateFile} " +
            "from the test directory.");
    }
}
