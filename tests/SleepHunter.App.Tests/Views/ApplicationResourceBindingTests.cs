using System.Threading;
using System.Windows;
using System.Xml.Linq;

using SleepHunter.Controls;
using SleepHunter.Settings;

namespace SleepHunter.Tests.Views;

[Apartment(ApartmentState.STA)]
public sealed class ApplicationResourceBindingTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly string[] TemplateFiles =
    [
        "AbilityDataTemplate.xaml",
        "ClientListItemDataTemplate.xaml",
        "EquipmentItemDataTemplate.xaml",
        "InventoryItemDataTemplate.xaml",
        "SpellQueueDataTemplate.xaml",
        "SpellTargetDataTemplates.xaml"
    ];

    [Test]
    public void ShouldPublishSettingsToEveryTemplateProxy()
    {
        var settingsManager = new UserSettingsManager();
        var firstProxy = new BindingProxy();
        var secondProxy = new BindingProxy();
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                ["UserSettingsManagerProxy"] = firstProxy
            });
        resources.MergedDictionaries.Add(
            new ResourceDictionary());
        resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                ["UserSettingsManagerProxy"] = secondProxy
            });

        App.BindTemplateResources(
            resources,
            settingsManager);

        Assert.Multiple(() =>
        {
            Assert.That(
                firstProxy.Value,
                Is.SameAs(settingsManager));
            Assert.That(
                secondProxy.Value,
                Is.SameAs(settingsManager));
        });
    }

    [TestCaseSource(nameof(TemplateFiles))]
    public void ShouldUseALocalSettingsProxy(
        string templateFile)
    {
        var document = XDocument.Load(
            FindTemplateFile(templateFile));
        var proxy = document
            .Root!
            .Elements()
            .Single(element =>
                element.Name.LocalName ==
                nameof(BindingProxy));
        var settingsReferences = document
            .Descendants()
            .Attributes()
            .Select(attribute => attribute.Value)
            .Where(value =>
                value.Contains(
                    "UserSettingsManager",
                    StringComparison.Ordinal))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)proxy.Attribute(Xaml + "Key"),
                Is.EqualTo("UserSettingsManagerProxy"));
            Assert.That(settingsReferences, Is.Not.Empty);
            Assert.That(
                settingsReferences,
                Has.All.Contains(
                    "UserSettingsManagerProxy"));
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
