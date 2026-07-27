using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using SleepHunter.Converters;

namespace SleepHunter.Tests.Views;

[Apartment(ApartmentState.STA)]
public sealed class ClientListItemDataTemplateTests
{
    private Application application = null!;
    private ResourceDictionary resources = null!;

    [SetUp]
    public void SetUp()
    {
        resources = new ResourceDictionary
        {
            ["NumericConverter"] = new NumericConverter(),
            ["VisibilityConverter"] = new VisibilityConverter()
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
                    "/SleepHunter;component/Templates/ClientListItemDataTemplate.xaml",
                    UriKind.Relative)));
    }

    [TearDown]
    public void TearDown()
    {
        application.Resources.MergedDictionaries.Remove(resources);
    }

    [Test]
    public void ShouldRenderVitalsInsideTallerContrastingBars()
    {
        var progressBarStyle =
            (Style)resources["VitalProgressBar"];
        var minimumHeight = progressBarStyle.Setters
            .OfType<Setter>()
            .Single(setter =>
                setter.Property == FrameworkElement.MinHeightProperty);
        var background = progressBarStyle.Setters
            .OfType<Setter>()
            .Single(setter =>
                setter.Property == Control.BackgroundProperty);
        var template =
            (DataTemplate)resources["ClientListItemDataTemplate"];
        var content = (FrameworkElement)template.LoadContent();
        var healthBar = content.FindName(
            "HealthBar") as ProgressBar;
        var manaBar = content.FindName(
            "ManaBar") as ProgressBar;
        var healthLayout = content.FindName(
            "HealthBarLayout") as Grid;
        var manaLayout = content.FindName(
            "ManaBarLayout") as Grid;
        var healthContainer = content.FindName(
            "HealthBarContainer") as Border;
        var manaContainer = content.FindName(
            "ManaBarContainer") as Border;
        var healthLabelBackground = content.FindName(
            "HealthLabelBackground") as Border;
        var healthValueBackground = content.FindName(
            "HealthValueBackground") as Border;
        var manaLabelBackground = content.FindName(
            "ManaLabelBackground") as Border;
        var manaValueBackground = content.FindName(
            "ManaValueBackground") as Border;
        var healthLabel = content.FindName(
            "HealthLabelText") as TextBlock;
        var healthValue = content.FindName(
            "HealthText") as TextBlock;
        var manaLabel = content.FindName(
            "ManaLabelText") as TextBlock;
        var manaValue = content.FindName(
            "ManaText") as TextBlock;
        healthBar?.ApplyTemplate();
        manaBar?.ApplyTemplate();
        var healthSegments = healthBar?.Template.FindName(
            "SegmentGrid",
            healthBar) as UniformGrid;
        var manaSegments = manaBar?.Template.FindName(
            "SegmentGrid",
            manaBar) as UniformGrid;
        var firstHealthSegment =
            healthSegments?.Children[0] as Border;
        var firstHealthSegmentOutline =
            firstHealthSegment?.Child as Border;
        var endcapColor = Color.FromRgb(23, 23, 23);
        var borderColor = Color.FromRgb(38, 38, 38);
        var segmentBorderColor =
            Color.FromArgb(32, 255, 255, 255);

        Assert.Multiple(() =>
        {
            Assert.That(minimumHeight.Value, Is.EqualTo(20));
            Assert.That(
                ((SolidColorBrush)background.Value).Color,
                Is.EqualTo(Color.FromRgb(64, 64, 64)));
            Assert.That(healthBar, Is.Not.Null);
            Assert.That(manaBar, Is.Not.Null);
            Assert.That(
                ((SolidColorBrush)healthBar!.Foreground).Color,
                Is.EqualTo(Color.FromRgb(30, 58, 138)));
            Assert.That(
                ((SolidColorBrush)manaBar!.Foreground).Color,
                Is.EqualTo(Color.FromRgb(30, 58, 138)));
            Assert.That(healthSegments?.Children.Count, Is.EqualTo(10));
            Assert.That(manaSegments?.Children.Count, Is.EqualTo(10));
            Assert.That(
                ((SolidColorBrush)firstHealthSegmentOutline!.BorderBrush).Color,
                Is.EqualTo(segmentBorderColor));
            Assert.That(
                healthLayout?.ColumnDefinitions[0].Width.Value,
                Is.EqualTo(34));
            Assert.That(
                healthLayout?.ColumnDefinitions[2].Width.Value,
                Is.EqualTo(92));
            Assert.That(
                manaLayout?.ColumnDefinitions[0].Width.Value,
                Is.EqualTo(34));
            Assert.That(
                manaLayout?.ColumnDefinitions[2].Width.Value,
                Is.EqualTo(92));
            Assert.That(
                ((SolidColorBrush)healthLabelBackground!.Background).Color,
                Is.EqualTo(endcapColor));
            Assert.That(
                ((SolidColorBrush)healthValueBackground!.Background).Color,
                Is.EqualTo(endcapColor));
            Assert.That(
                ((SolidColorBrush)manaLabelBackground!.Background).Color,
                Is.EqualTo(endcapColor));
            Assert.That(
                ((SolidColorBrush)manaValueBackground!.Background).Color,
                Is.EqualTo(endcapColor));
            Assert.That(
                ((SolidColorBrush)healthContainer!.BorderBrush).Color,
                Is.EqualTo(borderColor));
            Assert.That(
                ((SolidColorBrush)manaContainer!.BorderBrush).Color,
                Is.EqualTo(borderColor));
            Assert.That(healthLabel?.FontSize, Is.EqualTo(12));
            Assert.That(healthValue?.FontSize, Is.EqualTo(12));
            Assert.That(manaLabel?.FontSize, Is.EqualTo(12));
            Assert.That(manaValue?.FontSize, Is.EqualTo(12));
        });
    }
}
