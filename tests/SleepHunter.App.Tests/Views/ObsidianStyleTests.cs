using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ShapePath = System.Windows.Shapes.Path;

namespace SleepHunter.Tests.Views;

[Apartment(ApartmentState.STA)]
public sealed class ObsidianStyleTests
{
    private ResourceDictionary resources = null!;

    [SetUp]
    public void SetUp()
    {
        resources = (ResourceDictionary)Application.LoadComponent(
            new Uri(
                "/SleepHunter;component/Obsidian.xaml",
                UriKind.Relative));
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
                Is.EqualTo(2));
        });
    }
}
