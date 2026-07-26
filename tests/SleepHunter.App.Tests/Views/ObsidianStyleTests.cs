using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

using SleepHunter.Themes;

using ShapePath = System.Windows.Shapes.Path;

namespace SleepHunter.Tests.Views;

[Apartment(ApartmentState.STA)]
public sealed class ObsidianStyleTests
{
    private Application application = null!;
    private ResourceDictionary resources = null!;

    [SetUp]
    public void SetUp()
    {
        resources = (ResourceDictionary)Application.LoadComponent(
            new Uri(
                "/SleepHunter;component/Obsidian.xaml",
                UriKind.Relative));
        application = Application.Current ?? new Application();
        application.Resources.MergedDictionaries.Add(resources);
    }

    [TearDown]
    public void TearDown()
    {
        application.Resources.MergedDictionaries.Remove(resources);
    }

    [TestCase("ObsidianListBox")]
    [TestCase("ObsidianListView")]
    public void ShouldUseARecyclingVirtualizingItemsPanel(
        string styleKey)
    {
        var style = (Style)resources[styleKey];
        var setter = style.Setters
            .OfType<Setter>()
            .Single(candidate =>
                candidate.Property == ItemsControl.ItemsPanelProperty);
        var template = (ItemsPanelTemplate)setter.Value;

        var panel = template.LoadContent();

        Assert.That(
            panel,
            Is.TypeOf<VirtualizingStackPanel>());
    }

    [TestCase("ObsidianScrollViewer")]
    [TestCase("ObsidianGridViewScrollViewer")]
    public void ShouldForwardLogicalScrollingToTheContentPresenter(
        string styleKey)
    {
        var scrollViewer = new ScrollViewer
        {
            CanContentScroll = true,
            Style = (Style)resources[styleKey]
        };

        scrollViewer.ApplyTemplate();
        var presenter = scrollViewer.Template.FindName(
            "PART_ScrollContentPresenter",
            scrollViewer) as ScrollContentPresenter;

        Assert.Multiple(() =>
        {
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter!.CanContentScroll, Is.True);
        });
    }

    [Test]
    public void ShouldOnlyRealizeVisibleMetadataRows()
    {
        var gridView = new GridView();
        gridView.Columns.Add(
            new GridViewColumn
            {
                DisplayMemberBinding = new Binding("."),
                Width = 200
            });
        var listView = new ListView
        {
            Height = 240,
            ItemsSource = Enumerable.Range(1, 1000),
            Style = (Style)resources["ObsidianListView"],
            View = gridView,
            Width = 320
        };

        listView.Measure(new Size(320, 240));
        listView.Arrange(new Rect(0, 0, 320, 240));
        listView.UpdateLayout();
        var realizedCount = Enumerable
            .Range(0, listView.Items.Count)
            .Count(index =>
                listView.ItemContainerGenerator
                    .ContainerFromIndex(index) is not null);

        Assert.Multiple(() =>
        {
            Assert.That(realizedCount, Is.GreaterThan(0));
            Assert.That(realizedCount, Is.LessThan(100));
            Assert.That(
                listView.ItemContainerGenerator
                    .ContainerFromIndex(999),
                Is.Null);
        });
    }

    [Test]
    public void ShouldCenterTheCheckboxTickVertically()
    {
        var checkBox = new CheckBox
        {
            IsChecked = true,
            Style = (Style)resources["ObsidianCheckBox"]
        };

        checkBox.ApplyTemplate();
        var checkMark = checkBox.Template.FindName(
            "CheckMark",
            checkBox) as ShapePath;

        Assert.Multiple(() =>
        {
            Assert.That(checkMark, Is.Not.Null);
            Assert.That(
                checkMark!.RenderTransform,
                Is.TypeOf<TranslateTransform>());
            Assert.That(
                ((TranslateTransform)checkMark.RenderTransform).Y,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldUseAnAlphaInsetBorderForCheckedCheckboxes()
    {
        var checkBox = new CheckBox
        {
            IsChecked = true,
            Style = (Style)resources["ObsidianCheckBox"]
        };

        checkBox.ApplyTemplate();
        var insetBorder = checkBox.Template.FindName(
            "AccentInsetBorder",
            checkBox) as Border;
        var insetBrush =
            (SolidColorBrush)resources[
                "ObsidianAccentInsetBorderBrush"];

        Assert.Multiple(() =>
        {
            Assert.That(insetBorder, Is.Not.Null);
            Assert.That(insetBorder!.Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(
                insetBrush.Color,
                Is.EqualTo(Color.FromArgb(32, 255, 255, 255)));
        });

        checkBox.IsChecked = false;

        Assert.That(
            insetBorder!.Visibility,
            Is.EqualTo(Visibility.Collapsed));
    }

    [Test]
    public void ShouldSlightlyHighlightComboBoxBordersOnHover()
    {
        var style = (Style)resources["ObsidianComboBox"];
        var template = (ControlTemplate)style.Setters
            .OfType<Setter>()
            .Single(setter =>
                setter.Property == Control.TemplateProperty)
            .Value;
        var hoverTrigger = template.Triggers
            .OfType<Trigger>()
            .Single(trigger =>
                trigger.Property == UIElement.IsMouseOverProperty &&
                Equals(trigger.Value, true));
        var hoverBrush =
            (Brush)resources["ObsidianDisabled"];

        Assert.That(
            hoverTrigger.Setters
                .OfType<Setter>()
                .Any(setter =>
                    setter.Property ==
                        Control.BorderBrushProperty &&
                    Equals(setter.Value, hoverBrush)),
            Is.True);
    }

    [Test]
    public void ShouldSlightlyHighlightNumericUpDownBordersOnHover()
    {
        var style =
            (Style)resources["ObsidianNumericUpDown"];
        var hoverTrigger = style.Triggers
            .OfType<Trigger>()
            .Single(trigger =>
                trigger.Property == UIElement.IsMouseOverProperty &&
                Equals(trigger.Value, true));
        var hoverBrush =
            (Brush)resources["ObsidianDisabled"];

        Assert.That(
            hoverTrigger.Setters
                .OfType<Setter>()
                .Any(setter =>
                    setter.Property ==
                        Control.BorderBrushProperty &&
                    Equals(setter.Value, hoverBrush)),
            Is.True);
    }

    [Test]
    public void ShouldSlightlyHighlightCheckboxBordersOnHover()
    {
        var style = (Style)resources["ObsidianCheckBox"];
        var template = (ControlTemplate)style.Setters
            .OfType<Setter>()
            .Single(setter =>
                setter.Property == Control.TemplateProperty)
            .Value;
        var hoverTrigger = template.Triggers
            .OfType<Trigger>()
            .Single(trigger =>
                trigger.Property == UIElement.IsMouseOverProperty &&
                Equals(trigger.Value, true));
        var hoverBrush =
            (Brush)resources["ObsidianDisabled"];

        Assert.That(
            hoverTrigger.Setters
                .OfType<Setter>()
                .Any(setter =>
                    setter.TargetName == "Border" &&
                    setter.Property ==
                        Border.BorderBrushProperty &&
                    Equals(setter.Value, hoverBrush)),
            Is.True);
    }

    [Test]
    public void ShouldInsetTheAccentComboBoxCaret()
    {
        var toggleButton = new ToggleButton
        {
            Style =
                (Style)resources["ObsidianComboBoxToggleButton"]
        };

        toggleButton.ApplyTemplate();
        var insetBorder = toggleButton.Template.FindName(
            "AccentInsetBorder",
            toggleButton) as Border;

        Assert.Multiple(() =>
        {
            Assert.That(insetBorder, Is.Not.Null);
            Assert.That(insetBorder!.Visibility, Is.EqualTo(Visibility.Visible));
        });

        toggleButton.IsEnabled = false;

        Assert.That(
            insetBorder!.Visibility,
            Is.EqualTo(Visibility.Collapsed));
    }

    [TestCase("ObsidianButton")]
    [TestCase("ObsidianIconButton")]
    [TestCase("ObsidianToolBarButton")]
    [TestCase("ObsidianCommandButton")]
    public void ShouldShowTheAccentInsetWhileButtonsArePressed(
        string styleKey)
    {
        var style = (Style)resources[styleKey];
        var template = (ControlTemplate)style.Setters
            .OfType<Setter>()
            .Single(setter =>
                setter.Property == Control.TemplateProperty)
            .Value;
        var pressedTrigger = template.Triggers
            .OfType<Trigger>()
            .Single(trigger =>
                trigger.Property == ButtonBase.IsPressedProperty
                && Equals(trigger.Value, true));

        Assert.That(
            pressedTrigger.Setters
                .OfType<Setter>()
                .Any(setter =>
                    setter.TargetName == "AccentInsetBorder"
                    && setter.Property
                        == UIElement.VisibilityProperty
                    && Equals(
                        setter.Value,
                        Visibility.Visible)),
            Is.True);
    }

    [TestCase("ObsidianSpinButton", true)]
    [TestCase("ObsidianTextBoxClearButton", false)]
    public void ShouldInsetAccentAuxiliaryButtons(
        string styleKey,
        bool isRepeatButton)
    {
        ButtonBase button = isRepeatButton
            ? new RepeatButton()
            : new Button();
        button.Style = (Style)resources[styleKey];

        button.ApplyTemplate();
        var insetBorder = button.Template.FindName(
            "AccentInsetBorder",
            button) as Border;

        Assert.Multiple(() =>
        {
            Assert.That(insetBorder, Is.Not.Null);
            Assert.That(insetBorder!.Visibility, Is.EqualTo(Visibility.Visible));
        });

        button.IsEnabled = false;

        Assert.That(
            insetBorder!.Visibility,
            Is.EqualTo(Visibility.Collapsed));
    }

    [TestCase("ObsidianSpinButton", "ContentSite")]
    [TestCase("ObsidianComboBoxToggleButton", "Arrow")]
    public void ShouldDepressComboAndNumericGlyphsWhenPressed(
        string styleKey,
        string targetName)
    {
        var style = (Style)resources[styleKey];
        var template = (ControlTemplate)style.Setters
            .OfType<Setter>()
            .Single(setter =>
                setter.Property == Control.TemplateProperty)
            .Value;
        var pressedTrigger = template.Triggers
            .OfType<Trigger>()
            .Single(trigger =>
                trigger.Property == ButtonBase.IsPressedProperty
                && Equals(trigger.Value, true));
        var pressAnimation = pressedTrigger.EnterActions
            .OfType<BeginStoryboard>()
            .SelectMany(action =>
                action.Storyboard.Children
                    .OfType<DoubleAnimation>())
            .Single(animation =>
                Storyboard.GetTargetName(animation)
                    == targetName);
        var releaseAnimation = pressedTrigger.ExitActions
            .OfType<BeginStoryboard>()
            .SelectMany(action =>
                action.Storyboard.Children
                    .OfType<DoubleAnimation>())
            .Single(animation =>
                Storyboard.GetTargetName(animation)
                    == targetName);

        Assert.Multiple(() =>
        {
            Assert.That(pressAnimation.To, Is.EqualTo(1));
            Assert.That(
                pressAnimation.Duration.TimeSpan,
                Is.EqualTo(TimeSpan.FromMilliseconds(50)));
            Assert.That(releaseAnimation.To, Is.EqualTo(0));
            Assert.That(
                releaseAnimation.Duration.TimeSpan,
                Is.EqualTo(TimeSpan.FromMilliseconds(50)));
        });
    }

    [Test]
    public void ShouldInsetTheSharedProgressIndicator()
    {
        var progressBar = new ProgressBar
        {
            Style = (Style)resources["ObsidianProgressBar"],
            Value = 50,
            Width = 100
        };

        progressBar.ApplyTemplate();
        var indicator = progressBar.Template.FindName(
            "PART_Indicator",
            progressBar) as Border;
        var insetBorder = progressBar.Template.FindName(
            "IndicatorInsetBorder",
            progressBar) as Border;

        Assert.Multiple(() =>
        {
            Assert.That(progressBar.MinHeight, Is.EqualTo(10));
            Assert.That(indicator, Is.Not.Null);
            Assert.That(insetBorder, Is.Not.Null);
            Assert.That(
                insetBorder!.BorderBrush,
                Is.SameAs(
                    resources[
                        "ObsidianAccentInsetBorderBrush"]));
            Assert.That(insetBorder.Margin, Is.EqualTo(new Thickness(0)));
        });
    }

    [TestCase(
        "ObsidianTabItem",
        1,
        1,
        1,
        0)]
    [TestCase(
        "ObsidianVerticalTabItem",
        1,
        1,
        0,
        1)]
    public void ShouldUseAThreeSidedInsetOnSelectedTabs(
        string styleKey,
        double left,
        double top,
        double right,
        double bottom)
    {
        var tabItem = new TabItem
        {
            IsSelected = true,
            Style = (Style)resources[styleKey]
        };

        tabItem.ApplyTemplate();
        var insetBorder = tabItem.Template.FindName(
            "AccentInsetBorder",
            tabItem) as Border;

        Assert.Multiple(() =>
        {
            Assert.That(insetBorder, Is.Not.Null);
            Assert.That(insetBorder!.Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(
                insetBorder.BorderThickness,
                Is.EqualTo(
                    new Thickness(
                        left,
                        top,
                        right,
                        bottom)));
        });
    }

    [Test]
    public void ShouldInsetAccentMetadataColumnHeaders()
    {
        var header = new GridViewColumnHeader
        {
            Style =
                (Style)resources[
                    "ObsidianGridViewColumnHeader"]
        };

        header.ApplyTemplate();
        var insetBorder = header.Template.FindName(
            "AccentInsetBorder",
            header) as Border;
        var gripper = header.Template.FindName(
            "PART_HeaderGripper",
            header) as Thumb;
        gripper?.ApplyTemplate();
        var resizeHitTarget = gripper?.Template.FindName(
            "ResizeHitTarget",
            gripper) as Border;

        Assert.Multiple(() =>
        {
            Assert.That(insetBorder, Is.Not.Null);
            Assert.That(insetBorder!.Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(gripper, Is.Not.Null);
            Assert.That(gripper!.Width, Is.EqualTo(18));
            Assert.That(resizeHitTarget, Is.Not.Null);
            Assert.That(resizeHitTarget!.Child, Is.Null);
        });
    }

    [TestCase("LeftResizeHandle", "SizeWE", 10)]
    [TestCase("RightResizeHandle", "SizeWE", 11)]
    [TestCase("TopResizeHandle", "SizeNS", 12)]
    [TestCase("TopLeftResizeHandle", "SizeNWSE", 13)]
    [TestCase("TopRightResizeHandle", "SizeNESW", 14)]
    [TestCase("BottomResizeHandle", "SizeNS", 15)]
    [TestCase("BottomLeftResizeHandle", "SizeNESW", 16)]
    [TestCase("BottomRightResizeHandle", "SizeNWSE", 17)]
    public void ShouldExposeNativeResizeHandleForEachEdgeAndCorner(
        string handleName,
        string expectedCursor,
        int expectedHitTest)
    {
        var window = new Window
        {
            ResizeMode = ResizeMode.CanResize,
            Style = (Style)resources["ObsidianWindow"]
        };

        window.ApplyTemplate();
        var resizeHandle = window.Template.FindName(
            handleName,
            window) as Border;

        Assert.Multiple(() =>
        {
            Assert.That(resizeHandle, Is.Not.Null);
            Assert.That(
                resizeHandle!.Cursor.ToString(),
                Is.EqualTo(expectedCursor));
            Assert.That(
                Obsidian.GetResizeHitTest(handleName),
                Is.EqualTo((nuint)expectedHitTest));
        });
    }

    [TestCase(ResizeMode.CanResize, Visibility.Visible)]
    [TestCase(ResizeMode.CanResizeWithGrip, Visibility.Visible)]
    [TestCase(ResizeMode.CanMinimize, Visibility.Collapsed)]
    [TestCase(ResizeMode.NoResize, Visibility.Collapsed)]
    public void ShouldOnlyExposeResizeHandlesForResizableWindows(
        ResizeMode resizeMode,
        Visibility expectedVisibility)
    {
        var window = new Window
        {
            ResizeMode = resizeMode,
            Style = (Style)resources["ObsidianWindow"]
        };

        window.ApplyTemplate();
        var resizeHandles = window.Template.FindName(
            "WindowResizeHandles",
            window) as Grid;

        Assert.Multiple(() =>
        {
            Assert.That(resizeHandles, Is.Not.Null);
            Assert.That(
                resizeHandles!.Visibility,
                Is.EqualTo(expectedVisibility));
        });
    }
}
