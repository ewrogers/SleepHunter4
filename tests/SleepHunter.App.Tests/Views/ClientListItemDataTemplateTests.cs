using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

using SleepHunter.Converters;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.ViewModels;
using SleepHunter.ViewModels.Presentation;

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
    public void ShouldRenderCompactWrappedSpellEffectsWithSteppedBars()
    {
        var durationStyle =
            (Style)resources["SpellEffectDurationProgressBar"];
        var effectTemplate =
            (DataTemplate)resources["ActiveSpellEffectDataTemplate"];
        var effectContent =
            (FrameworkElement)effectTemplate.LoadContent();
        var effectIcon = effectContent.FindName(
            "EffectIcon") as Image;
        var durationBar = effectContent.FindName(
            "EffectDurationBar") as ProgressBar;
        var clientTemplate =
            (DataTemplate)resources["ClientListItemDataTemplate"];
        var clientContent =
            (FrameworkElement)clientTemplate.LoadContent();
        var effectsPanel = clientContent.FindName(
            "SpellEffectsPanel") as ItemsControl;
        var effectsWrapPanel =
            effectsPanel?.ItemsPanel.LoadContent() as WrapPanel;
        var stageColors =
            effectTemplate.Triggers
                .OfType<DataTrigger>()
                .Where(trigger =>
                    trigger.Value is SpellEffectDurationStage)
                .ToDictionary(
                    trigger =>
                        (SpellEffectDurationStage)trigger.Value,
                    trigger =>
                        ((SolidColorBrush)trigger.Setters
                            .OfType<Setter>()
                            .Single(setter =>
                                setter.TargetName ==
                                    "EffectDurationBar" &&
                                setter.Property ==
                                    Control.ForegroundProperty)
                            .Value).Color);
        durationBar?.ApplyTemplate();
        var indicator = durationBar?.Template.FindName(
            "PART_Indicator",
            durationBar) as Border;
        var durationTrack = durationBar?.Template.FindName(
            "PART_Track",
            durationBar) as Grid;
        durationBar?.Measure(new Size(22, 3));
        durationBar?.Arrange(new Rect(0, 0, 22, 3));
        durationBar?.UpdateLayout();
        if (durationBar is not null)
            durationBar.Value = 6;
        durationBar?.UpdateLayout();
        var whiteStageWidth = indicator?.ActualWidth;
        if (durationBar is not null)
            durationBar.Value = 1;
        durationBar?.UpdateLayout();
        var blueStageWidth = indicator?.ActualWidth;
        var pulseTrigger = effectTemplate.Triggers
            .OfType<DataTrigger>()
            .Single(trigger =>
                Equals(
                    trigger.Value,
                    SpellEffectDurationStage.Blue));
        var pulseBinding = pulseTrigger.Binding as Binding;
        var beginPulse = pulseTrigger.EnterActions
            .OfType<BeginStoryboard>()
            .Single();
        var pulseAnimation = beginPulse.Storyboard.Children
            .OfType<DoubleAnimation>()
            .Single();
        var removePulse = pulseTrigger.ExitActions
            .OfType<RemoveStoryboard>()
            .Single();
        var effectsVisibilityTrigger =
            clientTemplate.Triggers
                .OfType<MultiDataTrigger>()
                .Single(trigger =>
                    trigger.Setters
                        .OfType<Setter>()
                        .Any(setter =>
                            setter.TargetName ==
                                "SpellEffectsPanel" &&
                            setter.Property ==
                                UIElement.VisibilityProperty));
        var effectsVisibilityPaths =
            effectsVisibilityTrigger.Conditions
                .Select(condition =>
                    (condition.Binding as Binding)?.Path.Path)
                .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(effectIcon?.Width, Is.EqualTo(22));
            Assert.That(effectIcon?.Height, Is.EqualTo(22));
            Assert.That(
                RenderOptions.GetBitmapScalingMode(effectIcon),
                Is.EqualTo(BitmapScalingMode.NearestNeighbor));
            Assert.That(durationBar?.Width, Is.EqualTo(22));
            Assert.That(durationBar?.Height, Is.EqualTo(3));
            Assert.That(durationBar?.Minimum, Is.EqualTo(0));
            Assert.That(durationBar?.Maximum, Is.EqualTo(6));
            Assert.That(
                stageColors,
                Is.EqualTo(
                    new Dictionary<
                        SpellEffectDurationStage,
                        Color>
                    {
                        [SpellEffectDurationStage.Blue] =
                            Color.FromRgb(0x66, 0x66, 0x66),
                        [SpellEffectDurationStage.Green] =
                            Color.FromRgb(0x85, 0x85, 0x85),
                        [SpellEffectDurationStage.Yellow] =
                            Color.FromRgb(0xA3, 0xA3, 0xA3),
                        [SpellEffectDurationStage.Orange] =
                            Color.FromRgb(0xC2, 0xC2, 0xC2),
                        [SpellEffectDurationStage.Red] =
                            Color.FromRgb(0xE0, 0xE0, 0xE0),
                        [SpellEffectDurationStage.White] =
                            Colors.White
                    }));
            Assert.That(durationTrack, Is.Not.Null);
            Assert.That(indicator, Is.Not.Null);
            Assert.That(
                whiteStageWidth,
                Is.EqualTo(22).Within(0.01));
            Assert.That(
                blueStageWidth,
                Is.EqualTo(22.0 / 6).Within(0.01));
            Assert.That(effectsPanel, Is.Not.Null);
            Assert.That(
                Grid.GetRow(effectsPanel!),
                Is.EqualTo(5));
            Assert.That(
                effectsPanel!.Visibility,
                Is.EqualTo(Visibility.Collapsed));
            Assert.That(effectsWrapPanel, Is.Not.Null);
            Assert.That(
                effectsWrapPanel?.Orientation,
                Is.EqualTo(Orientation.Horizontal));
            Assert.That(
                durationStyle.TargetType,
                Is.EqualTo(typeof(ProgressBar)));
            Assert.That(
                pulseBinding?.Path.Path,
                Is.EqualTo(nameof(
                    ActiveSpellEffectViewModel.DurationStage)));
            Assert.That(
                beginPulse.Storyboard.AutoReverse,
                Is.True);
            Assert.That(
                beginPulse.Storyboard.RepeatBehavior,
                Is.EqualTo(RepeatBehavior.Forever));
            Assert.That(
                Storyboard.GetTargetName(pulseAnimation),
                Is.EqualTo("EffectVisual"));
            Assert.That(
                Storyboard.GetTargetProperty(pulseAnimation)?.Path,
                Is.EqualTo("Opacity"));
            Assert.That(pulseAnimation.From, Is.EqualTo(1));
            Assert.That(pulseAnimation.To, Is.EqualTo(0.3));
            Assert.That(
                pulseAnimation.Duration.TimeSpan,
                Is.EqualTo(TimeSpan.FromSeconds(0.75)));
            Assert.That(
                removePulse.BeginStoryboardName,
                Is.EqualTo("BlueDurationPulse"));
            Assert.That(
                effectsVisibilityPaths,
                Is.EquivalentTo(
                [
                    nameof(
                        ClientListItemViewModel
                            .HasActiveSpellEffects),
                    "Value.Settings.ShowActiveEffects"
                ]));
        });
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
