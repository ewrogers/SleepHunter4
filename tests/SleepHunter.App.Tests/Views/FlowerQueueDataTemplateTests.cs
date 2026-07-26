using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using SleepHunter.Converters;

namespace SleepHunter.Tests.Views;

[Apartment(ApartmentState.STA)]
public sealed class FlowerQueueDataTemplateTests
{
    private Application application = null!;
    private ResourceDictionary resources = null!;

    [SetUp]
    public void SetUp()
    {
        resources = new ResourceDictionary
        {
            ["TimeSpanConverter"] = new TimeSpanConverter()
        };
        resources.MergedDictionaries.Add(
            (ResourceDictionary)Application.LoadComponent(
                new Uri(
                    "/SleepHunter;component/Obsidian.xaml",
                    UriKind.Relative)));
        application = Application.Current ?? new Application();
        application.Resources.MergedDictionaries.Add(resources);
        resources.MergedDictionaries.Add(
            (ResourceDictionary)Application.LoadComponent(
                new Uri(
                    "/SleepHunter;component/Templates/FlowerQueueDataTemplate.xaml",
                    UriKind.Relative)));
    }

    [TearDown]
    public void TearDown()
    {
        application.Resources.MergedDictionaries.Remove(resources);
    }

    [Test]
    public void ShouldPresentFlowerConditionsAsExplicitAlternatives()
    {
        var template =
            (DataTemplate)resources["FlowerQueueItemDataTemplate"];
        var content = (FrameworkElement)template.LoadContent();
        var leadText = content.FindName(
            "ConditionLeadText") as TextBlock;
        var orText = content.FindName(
            "OrText") as TextBlock;
        var timerKeywordText = content.FindName(
            "TimerKeywordText") as TextBlock;
        var manaKeywordText = content.FindName(
            "ManaKeywordText") as TextBlock;
        var intervalText = content.FindName(
            "IntervalText") as TextBlock;
        var thresholdText = content.FindName(
            "ThresholdText") as TextBlock;
        var intervalBinding = BindingOperations.GetBinding(
            intervalText!,
            TextBlock.TextProperty);
        var thresholdBinding = BindingOperations.GetBinding(
            thresholdText!,
            TextBlock.TextProperty);
        var manaTrigger = FindNullTrigger(
            template,
            "ManaThreshold");
        var intervalTrigger = FindNullTrigger(
            template,
            "Interval");

        Assert.Multiple(() =>
        {
            Assert.That(
                leadText?.Text,
                Is.EqualTo("WHEN"));
            Assert.That(
                orText?.Text,
                Is.EqualTo("OR"));
            Assert.That(orText?.Parent, Is.TypeOf<WrapPanel>());
            Assert.That(
                timerKeywordText?.Text,
                Is.EqualTo("TIMER"));
            Assert.That(
                manaKeywordText?.Text,
                Is.EqualTo("MP"));
            Assert.That(
                timerKeywordText?.FontWeight,
                Is.EqualTo(FontWeights.SemiBold));
            Assert.That(
                manaKeywordText?.FontWeight,
                Is.EqualTo(FontWeights.SemiBold));
            Assert.That(
                intervalText?.FontWeight,
                Is.EqualTo(FontWeights.Normal));
            Assert.That(
                thresholdText?.FontWeight,
                Is.EqualTo(FontWeights.Normal));
            Assert.That(
                intervalBinding?.StringFormat,
                Is.EqualTo("> {0}"));
            Assert.That(
                thresholdBinding?.StringFormat,
                Is.EqualTo("< {0}"));
            Assert.That(
                CollapsesTarget(manaTrigger, "OrText"),
                Is.True);
            Assert.That(
                CollapsesTarget(intervalTrigger, "OrText"),
                Is.True);
        });
    }

    private static DataTrigger FindNullTrigger(
        DataTemplate template,
        string propertyName) =>
        template.Triggers
            .OfType<DataTrigger>()
            .Single(trigger =>
                trigger.Value is null &&
                trigger.Binding is Binding binding &&
                binding.Path.Path == propertyName);

    private static bool CollapsesTarget(
        DataTrigger trigger,
        string targetName) =>
        trigger.Setters
            .OfType<Setter>()
            .Any(setter =>
                setter.TargetName == targetName &&
                setter.Property == UIElement.VisibilityProperty &&
                Equals(setter.Value, Visibility.Collapsed));
}
