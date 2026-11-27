using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Themes.Fluent2;
using Avalonia.Themes.UnitTests.Utilities;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class Fluent2TabItemTests() : ThemeTestBase(typeof(Fluent2Theme))
{
    [Theory]
    [InlineData(Dock.Left, HorizontalAlignment.Left)]
    [InlineData(Dock.Right, HorizontalAlignment.Right)]
    public void Selected_Pill_Is_Vertical_For_Side_Placement(Dock placement, HorizontalAlignment alignment)
    {
        var pill = ShowSelectedPill(placement);

        Assert.Equal(0, Grid.GetRow(pill));
        Assert.Equal(alignment, pill.HorizontalAlignment);
        Assert.True(pill.Height > pill.Width);
    }

    [Fact]
    public void Selected_Pill_Is_Above_The_Header_For_Bottom_Placement()
    {
        var pill = ShowSelectedPill(Dock.Bottom);

        Assert.Equal(0, Grid.GetRow(pill));
        Assert.Equal(VerticalAlignment.Top, pill.VerticalAlignment);
        Assert.True(pill.Width > pill.Height);
    }

    [Fact]
    public void Selected_Pill_Is_Below_The_Header_For_Top_Placement()
    {
        var pill = ShowSelectedPill(Dock.Top);

        Assert.Equal(1, Grid.GetRow(pill));
        Assert.Equal(VerticalAlignment.Bottom, pill.VerticalAlignment);
        Assert.True(pill.Width > pill.Height);
    }

    [Fact]
    public void Selected_Pill_Is_At_The_Strip_Edge_For_Right_Placement_When_Header_Is_Narrower()
    {
        var pill = ShowSelectedPill(Dock.Right, "A", "A much longer header");
        var item = pill.FindAncestorOfType<TabItem>()!;
        var strip = pill.FindAncestorOfType<ItemsPresenter>()!;

        var pillRight = Assert.NotNull(pill.TranslatePoint(new Point(pill.Bounds.Width, 0), strip)).X;
        var expected = strip.Bounds.Width - item.BorderThickness.Right - pill.Margin.Right;

        Assert.Equal(expected, pillRight, 3);
    }

    private Rectangle ShowSelectedPill(Dock placement, string firstHeader = "First", string secondHeader = "Second")
    {
        CreateAttachedTheme();
        var target = new TabControl
        {
            TabStripPlacement = placement,
            ItemsSource = new[] { new TabItem { Header = firstHeader }, new TabItem { Header = secondHeader } },
            SelectedIndex = 0,
        };
        var window = new Window { Content = target };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var item = target.GetVisualDescendants().OfType<TabItem>().First(x => x.IsSelected);
        return item.GetVisualDescendants().OfType<Rectangle>().Single(x => x.Name == "PART_SelectedPipe");
    }
}
