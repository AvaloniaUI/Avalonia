#if AVALONIA_SKIA
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.RenderTests;

// A visual with a BitmapCache must refresh the cache layer when its own content or its children change.
public class BitmapCacheCompositionTests : TestBase
{
    public BitmapCacheCompositionTests()
        : base(@"Composition\BitmapCache")
    {
    }

    private sealed class RectControl : Control
    {
        public Color Color { get; set; } = Colors.Red;
        public double RectWidth { get; set; } = 40;

        public override void Render(DrawingContext context) =>
            context.FillRectangle(new SolidColorBrush(Color), new Rect(0, 0, RectWidth, 40));
    }

    private static void AssertColor(FramebufferCompositionFixture h, int x, int y, Color expected)
    {
        var px = h.Px(x, y);
        Assert.True(px.Red == expected.R && px.Green == expected.G && px.Blue == expected.B && px.Alpha == expected.A,
            $"pixel at ({x}, {y}): expected {expected}, got {px}");
    }

    private static FramebufferCompositionFixture Start(Control content)
    {
        var h = new FramebufferCompositionFixture(200, 200) { Canvas = { Children = { content } } };
        h.Retained(true);
        h.Frame();
        h.Frame();
        return h;
    }

    [Fact]
    public void Own_Content_Change_Of_Cached_Visual_Updates_Cache()
    {
        var rect = new RectControl { Width = 100, Height = 100, CacheMode = new BitmapCache() };
        Canvas.SetLeft(rect, 20);
        Canvas.SetTop(rect, 20);
        using var h = Start(rect);
        AssertColor(h, 40, 40, Colors.Red);

        rect.Color = Colors.Blue;
        rect.InvalidateVisual();
        h.Frame();
        AssertColor(h, 40, 40, Colors.Blue);

        rect.Color = Colors.Lime;
        rect.InvalidateVisual();
        h.Frame();
        AssertColor(h, 40, 40, Colors.Lime);
    }

    [Fact]
    public void Own_Content_Bounds_Change_Of_Cached_Visual_Updates_Cache()
    {
        var rect = new RectControl { Width = 100, Height = 100, CacheMode = new BitmapCache() };
        Canvas.SetLeft(rect, 20);
        Canvas.SetTop(rect, 20);
        using var h = Start(rect);
        AssertColor(h, 40, 40, Colors.Red);
        Assert.Equal(0, h.Px(90, 40).Alpha);

        rect.Color = Colors.Blue;
        rect.RectWidth = 80;
        rect.InvalidateVisual();
        h.Frame();
        AssertColor(h, 40, 40, Colors.Blue);
        AssertColor(h, 90, 40, Colors.Blue);

        rect.Color = Colors.Lime;
        rect.RectWidth = 20;
        rect.InvalidateVisual();
        h.Frame();
        AssertColor(h, 30, 40, Colors.Lime);
        Assert.Equal(0, h.Px(50, 40).Alpha);
        Assert.Equal(0, h.Px(90, 40).Alpha);
    }

    [Fact]
    public void Child_Content_Change_Under_Cached_Parent_Updates_Cache()
    {
        var rect = new RectControl { Width = 100, Height = 100 };
        var host = new Decorator { Child = rect, CacheMode = new BitmapCache() };
        Canvas.SetLeft(host, 20);
        Canvas.SetTop(host, 20);
        using var h = Start(host);
        AssertColor(h, 40, 40, Colors.Red);

        rect.Color = Colors.Blue;
        rect.InvalidateVisual();
        h.Frame();
        AssertColor(h, 40, 40, Colors.Blue);
    }

    [Fact]
    public void Own_And_Child_Content_Changes_Of_Cached_Visual_Update_Cache()
    {
        // The parent draws its own rectangle at (0, 0) and the child draws one at (50, 0) of the parent.
        var child = new RectControl { Width = 40, Height = 40 };
        var parent = new RectParent(child) { Width = 100, Height = 100, CacheMode = new BitmapCache() };
        Canvas.SetLeft(parent, 20);
        Canvas.SetTop(parent, 20);
        using var h = Start(parent);
        AssertColor(h, 30, 30, Colors.Red);
        AssertColor(h, 80, 30, Colors.Red);

        parent.Color = Colors.Blue;
        parent.InvalidateVisual();
        h.Frame();
        AssertColor(h, 30, 30, Colors.Blue);
        AssertColor(h, 80, 30, Colors.Red);

        child.Color = Colors.Lime;
        child.InvalidateVisual();
        h.Frame();
        AssertColor(h, 30, 30, Colors.Blue);
        AssertColor(h, 80, 30, Colors.Lime);

        parent.Color = Colors.Yellow;
        parent.InvalidateVisual();
        child.Color = Colors.Magenta;
        child.InvalidateVisual();
        h.Frame();
        AssertColor(h, 30, 30, Colors.Yellow);
        AssertColor(h, 80, 30, Colors.Magenta);
    }

    [Fact]
    public void Own_Content_Shrink_Of_Cached_Visual_With_Same_Layer_Size_Updates_Cache()
    {
        // The child keeps the bounds of the cached subtree, so the cache layer is not created again.
        var child = new RectControl { Width = 40, Height = 40 };
        var parent = new RectParent(child) { Width = 100, Height = 100, CacheMode = new BitmapCache() };
        Canvas.SetLeft(parent, 20);
        Canvas.SetTop(parent, 20);
        using var h = Start(parent);
        AssertColor(h, 50, 30, Colors.Red);

        parent.RectWidth = 20;
        parent.InvalidateVisual();
        h.Frame();
        AssertColor(h, 30, 30, Colors.Red);
        Assert.Equal(0, h.Px(50, 30).Alpha);
        AssertColor(h, 80, 30, Colors.Red);
    }

    [Theory, InlineData(false), InlineData(true)]
    public void Child_Removal_With_Dirty_Cached_Visual_Updates_Cache(bool changeOpacity)
    {
        // The second child keeps the bounds of the cached subtree, so the cache layer is not created again.
        static Border Child(double left) => new()
        {
            Background = Brushes.Red, Width = 20, Height = 20, Margin = new Thickness(left, 0, 0, 0),
            HorizontalAlignment = Layout.HorizontalAlignment.Left, VerticalAlignment = Layout.VerticalAlignment.Top
        };
        var removed = Child(30);
        var parent = new VisualChildrenHost { Width = 100, Height = 100, CacheMode = new BitmapCache() };
        parent.Add(removed);
        parent.Add(Child(60));
        Canvas.SetLeft(parent, 20);
        Canvas.SetTop(parent, 20);
        using var h = Start(parent);
        AssertColor(h, 60, 30, Colors.Red);

        // The cached visual is dirty for render in the same frame as the removal.
        parent.Remove(removed);
        if (changeOpacity)
            parent.Opacity = 0.99;
        else
            parent.InvalidateVisual();
        h.Frame();
        Assert.Equal(0, h.Px(60, 30).Alpha);
        Assert.NotEqual(0, h.Px(90, 30).Alpha);
    }

    public enum CachedVisualChange
    {
        None,
        Opacity,
        Offset,
        OwnSize
    }

    [Theory]
    [InlineData(CachedVisualChange.None, false)]
    [InlineData(CachedVisualChange.Opacity, false)]
    [InlineData(CachedVisualChange.Offset, false)]
    [InlineData(CachedVisualChange.OwnSize, true)]
    public void Cache_Is_Redrawn_Only_For_Changes_Inside_The_Layer(CachedVisualChange change, bool expectRedraw)
    {
        const int childCount = 10;
        using var f = new FramebufferCompositionFixture(200, 200);
        var cached = f.Rect(Colors.Blue, 20, 20, 20, 20);
        cached.ClipToBounds = false;
        cached.CacheMode = new CompositionBitmapCache(f.Compositor, new ServerCompositionBitmapCache(f.Compositor.Server));
        for (var i = 0; i < childCount; i++)
            cached.Children.Add(f.Rect(Colors.Red, i * 10, 30, 10, 10));
        f.Root.Children.Add(cached);
        f.Retained(true);
        f.Frame();
        f.Frame();

        var events = new CompositorTestServices.DebugEvents();
        f.Renderer.CompositionTarget.Server.DebugEvents = events;
        switch (change)
        {
            case CachedVisualChange.Opacity: cached.Opacity = 0.5f; break;
            case CachedVisualChange.Offset: cached.Offset = new Vector3D(25, 20, 0); break;
            // Stays inside the bounds of the children, so the cache layer is not created again.
            case CachedVisualChange.OwnSize: cached.Size = new Vector(10, 10); break;
        }
        f.Frame();

        if (expectRedraw)
            Assert.True(events.RenderedVisuals > childCount, $"rendered {events.RenderedVisuals} visuals");
        else
            Assert.True(events.RenderedVisuals < childCount, $"rendered {events.RenderedVisuals} visuals");
    }

    private sealed class VisualChildrenHost : Control
    {
        public void Add(Control child) => VisualChildren.Add(child);
        public void Remove(Control child) => VisualChildren.Remove(child);

        public override void Render(DrawingContext context) =>
            context.FillRectangle(Brushes.Blue, new Rect(0, 0, 20, 20));
    }

    private sealed class RectParent : Decorator
    {
        public RectParent(Control child)
        {
            Child = child;
            child.Margin = new Thickness(50, 0, 0, 0);
            child.HorizontalAlignment = Layout.HorizontalAlignment.Left;
            child.VerticalAlignment = Layout.VerticalAlignment.Top;
        }

        public Color Color { get; set; } = Colors.Red;
        public double RectWidth { get; set; } = 40;

        public override void Render(DrawingContext context) =>
            context.FillRectangle(new SolidColorBrush(Color), new Rect(0, 0, RectWidth, 40));
    }
}
#endif
