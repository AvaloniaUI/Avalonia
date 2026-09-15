using System.Linq;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Simple;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class TreeViewBringIntoViewTests : ScopedTestBase
{
    [Fact]
    public void BringIntoView_Should_Scroll_Back_To_Item_Scrolled_Off_To_The_Left()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var (root, treeView) = CreateTarget([CreateHeader(100), CreateHeader(500), CreateHeader(900)]);

        root.LayoutManager.ExecuteInitialLayoutPass();

        var scrollViewer = GetScrollViewer(treeView);

        scrollViewer.Offset = new Vector(scrollViewer.Extent.Width - scrollViewer.Viewport.Width, 0);
        root.LayoutManager.ExecuteLayoutPass();

        var startOffset = scrollViewer.Offset.X;
        Assert.True(startOffset > 0);

        // The first item is narrow and now completely off to the left:
        // bringing it into view must scroll back so that its header becomes visible.
        var item = Assert.IsType<TreeViewItem>(treeView.ContainerFromIndex(0));
        item.BringIntoView();
        root.LayoutManager.ExecuteLayoutPass();

        Assert.True(scrollViewer.Offset.X < 30);
    }

    [Fact]
    public void BringIntoView_Should_Not_Scroll_When_Item_Is_Already_Visible()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var (root, treeView) = CreateTarget([CreateHeader(100), CreateHeader(500), CreateHeader(900)]);

        root.LayoutManager.ExecuteInitialLayoutPass();

        var scrollViewer = GetScrollViewer(treeView);
        Assert.Equal(0, scrollViewer.Offset.X);

        var item = Assert.IsType<TreeViewItem>(treeView.ContainerFromIndex(0));
        item.BringIntoView();
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(0, scrollViewer.Offset.X);
    }

    [Fact]
    public void BringIntoView_Should_Reveal_A_Nested_Item()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        // A narrow item nested three levels deep, under wide ancestors that make the tree scroll.
        var nestedHeader = CreateHeader(100);
        var nestedItem = new TreeViewItem { Header = nestedHeader };
        var item = nestedItem;

        for (var i = 0; i < 3; i++)
        {
            item = new TreeViewItem
            {
                Header = CreateHeader(900),
                IsExpanded = true,
                ItemsSource = new[] { item }
            };
        }

        var (root, treeView) = CreateTarget([item]);

        root.LayoutManager.ExecuteInitialLayoutPass();

        var scrollViewer = GetScrollViewer(treeView);

        scrollViewer.Offset = new Vector(scrollViewer.Extent.Width - scrollViewer.Viewport.Width, 0);
        root.LayoutManager.ExecuteLayoutPass();
        Assert.True(scrollViewer.Offset.X > 0);

        nestedItem.BringIntoView();
        root.LayoutManager.ExecuteLayoutPass();

        var headerBounds = new Rect(nestedHeader.Bounds.Size).TransformToAABB(nestedHeader.TransformToVisual(scrollViewer)!.Value);

        Assert.True(headerBounds.Left >= -0.5 && headerBounds.Right <= scrollViewer.Viewport.Width + 0.5);
    }

    private static Border CreateHeader(double width) => new()
    {
        Width = width,
        Height = 20,
        HorizontalAlignment = HorizontalAlignment.Left,
        Background = Brushes.Red
    };

    private static (TestRoot Root, TreeView TreeView) CreateTarget(Control[] headers)
    {
        var treeView = new TreeView
        {
            Width = 400,
            Height = 200,
            ItemsSource = headers
        };

        var root = new TestRoot { Width = 400, Height = 200 };
        root.Resources.MergedDictionaries.Add(new SimpleTheme());
        root.Child = treeView;

        return (root, treeView);
    }

    private static ScrollViewer GetScrollViewer(TreeView treeView)
        => treeView.GetVisualDescendants().OfType<ScrollViewer>().Single();
}
