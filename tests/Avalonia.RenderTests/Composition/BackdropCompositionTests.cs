#if AVALONIA_SKIA
using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Skia.Helpers;
using Avalonia.Threading;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests;

// Backdrop effects drawn by the real compositor and Skia into an SKBitmap framebuffer.
// The tests need no reference images. A partial redraw is compared with a full redraw of the same final
// state by a new compositor. Erasing the framebuffer between frames, while reporting it as retained, shows
// which pixels a frame redrew. CPU rendering makes the blur deterministic.
public class BackdropCompositionTests : TestBase
{
    public BackdropCompositionTests()
        : base(@"Composition\Backdrop")
    {
    }

    private static ImmutableBlurEffect Blur(double radius) => new(radius);

    private static CompositionBitmapCache MakeCache(Compositor c) =>
        new(c, new ServerCompositionBitmapCache(c.Server));

    private static CompositionVolatileBackdropEffectCache MakeVolatileCacheMode(Compositor c) =>
        new(c, new ServerCompositionVolatileBackdropEffectCache(c.Server));

    private static CompositionRetainedBackdropEffectCache MakeRetainedCacheMode(Compositor c) =>
        new(c, new ServerCompositionRetainedBackdropEffectCache(c.Server));

    private static CompositionBackdropEffectCache MakeBackdropCacheMode(Compositor c, bool retained) =>
        retained ? MakeRetainedCacheMode(c) : MakeVolatileCacheMode(c);

    private static ImmutableDropShadowEffect DropShadow(double offset, double blur) =>
        new(offset, offset, blur, Colors.Black, 1);

    private sealed class FuncFramebufferSurface : IFramebufferPlatformSurface
    {
        private readonly Func<IFramebufferRenderTarget> _cb;
        public FuncFramebufferSurface(Func<IFramebufferRenderTarget> cb) => _cb = cb;
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => _cb();
    }

    // A compositor that draws into an SKBitmap framebuffer, with one container visual (`Root`) for the tests
    // to fill. `Retained` sets PreviousFrameIsRetained for the next frames.
    private sealed class Harness : IDisposable
    {
        private readonly ManualRenderTimer _timer = new();
        private readonly CompositingRenderer _renderer;
        private readonly TestRenderRoot _root;
        private bool _retained;

        public Compositor Compositor { get; }
        public CompositionContainerVisual Root { get; }
        public SKBitmap Fb { get; }

        public Harness(int width, int height, double scaling = 1)
        {
            Compositor = new Compositor(RenderLoop.FromTimer(_timer), null, true,
                new DispatcherCompositorScheduler(), true, Dispatcher.UIThread, new CompositionOptions
                {
                    UseRegionDirtyRectClipping = true
                });

            var pxW = (int)Math.Ceiling(width * scaling);
            var pxH = (int)Math.Ceiling(height * scaling);
            Fb = new SKBitmap(pxW, pxH, SKColorType.Rgba8888, SKAlphaType.Premul);

            ILockedFramebuffer LockFb() => new LockedFramebuffer(Fb.GetAddress(0, 0), new(Fb.Width, Fb.Height),
                Fb.RowBytes, new Vector(96 * scaling, 96 * scaling), PixelFormat.Rgba8888, AlphaFormat.Premul, null);

            IFramebufferRenderTarget rt = new FuncFramebufferRenderTarget((_, out props) =>
            {
                props = new() { PreviousFrameIsRetained = _retained };
                return LockFb();
            }, retainsFrameContents: true);

            var control = new Canvas { Width = width, Height = height };
            _root = new TestRenderRoot(scaling, null!);
            _renderer = new CompositingRenderer(_root, Compositor, () => new[] { new FuncFramebufferSurface(() => rt) });
            _root.Initialize(_renderer, control);
            _renderer.Start();

            Root = Compositor.CreateContainerVisual();
            Root.ClipToBounds = false;
            ElementComposition.SetElementChildVisual(control, Root);
        }

        internal CompositingRenderer Renderer => _renderer;

        public CompositionSolidColorVisual Rect(Color color, double x, double y, double w, double h)
        {
            var v = Compositor.CreateSolidColorVisual();
            v.Color = color;
            v.Offset = new Vector3D(x, y, 0);
            v.Size = new Vector(w, h);
            return v;
        }

        public void Retained(bool value) => _retained = value;
        public void Erase() => Fb.Erase(SKColor.Empty);

        public void Frame()
        {
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
            _timer.TriggerTick();
        }

        public SKColor Px(int x, int y) => Fb.GetPixel(x, y);

        public void Dispose()
        {
            _renderer.Dispose();
            Fb.Dispose();
        }
    }

    private static bool IsPainted(SKColor c) => c.Alpha != 0;

    // The frames are almost the same as their full redraw reference, so a real difference is far above this.
    private const double AllowedError = 0.022;

    private static SKBitmap ToUnpremulRgba(SKBitmap bmp)
    {
        var converted = new SKBitmap(
            new SKImageInfo(bmp.Width, bmp.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bmp.PeekPixels().ReadPixels(converted.Info, converted.GetPixels(), converted.RowBytes);
        return converted;
    }

    private static void AssertSimilar(SKBitmap actual, SKBitmap reference)
    {
        using var a = ToUnpremulRgba(actual);
        using var b = ToUnpremulRgba(reference);
        var error = TestRenderHelper.CompareImages(a, b);
        Assert.True(error <= AllowedError, $"Frame differs from full-redraw reference: error {error} > {AllowedError}");
    }

    // An opaque base, a movable `behind` square (earlier in draw order), a retained blur backdrop visual over
    // the middle, and a `caret` square in front of it.
    private static (CompositionSolidColorVisual behind, CompositionSolidColorVisual caret) BuildScene(
        Harness h, double behindY, double caretX, bool volatileBackdrop = false)
    {
        var baseRect = h.Rect(Colors.White, 0, 0, 200, 200);
        var stripe = h.Rect(Colors.Blue, 0, 0, 200, 30); // a pattern for the blur to show
        var behind = h.Rect(Colors.Red, 20, behindY, 40, 40);

        var backdropVisual = h.Rect(Colors.Transparent, 40, 40, 120, 120);
        backdropVisual.BackdropEffect = Blur(5); // sampling radius: ceil(5)+1 = 6 device pixels
        if (volatileBackdrop)
            backdropVisual.BackdropEffectCache = MakeVolatileCacheMode(h.Compositor);

        var caret = h.Rect(Colors.Black, caretX, 60, 6, 40); // in front of the backdrop visual

        h.Root.Children.Add(baseRect);
        h.Root.Children.Add(stripe);
        h.Root.Children.Add(behind);
        h.Root.Children.Add(backdropVisual);
        h.Root.Children.Add(caret);
        return (behind, caret);
    }

    [Fact]
    public void Retained_Front_Change_Equals_Full_Redraw()
    {
        // The caret moves. It is in front, so it is not input: the result equals a full redraw.
        using var reference = new Harness(200, 200);
        BuildScene(reference, behindY: 40, caretX: 100);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var (_, caret) = BuildScene(h, behindY: 40, caretX: 70);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Retained(true);
        caret.Offset = new Vector3D(100, 60, 0); // a change in front only
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Behind_Change_Equals_Full_Redraw()
    {
        using var reference = new Harness(200, 200);
        BuildScene(reference, behindY: 120, caretX: 100);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var (behind, _) = BuildScene(h, behindY: 40, caretX: 100);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Retained(true);
        behind.Offset = new Vector3D(20, 120, 0); // a change behind: the snapshot is refreshed
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Behind_Change_Equals_Full_Redraw_At_Scaling_2()
    {
        // With scaling 2, the root scale is part of the AABB and of the draw transform. An error in either shows
        // as a difference from the full redraw.
        using var reference = new Harness(200, 200, scaling: 2);
        BuildScene(reference, behindY: 120, caretX: 100);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200, scaling: 2);
        var (behind, _) = BuildScene(h, behindY: 40, caretX: 100);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Retained(true);
        behind.Offset = new Vector3D(20, 120, 0);
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Backdrop_Move_Equals_Full_Redraw()
    {
        // A move without a size change keeps the snapshot. It has to be refreshed completely.
        CompositionSolidColorVisual Build(Harness h, double x)
        {
            h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
            h.Root.Children.Add(h.Rect(Colors.Blue, 0, 0, 200, 30));
            h.Root.Children.Add(h.Rect(Colors.Red, 20, 40, 40, 40));
            var backdropVisual = h.Rect(Colors.Transparent, x, 20, 80, 80);
            backdropVisual.BackdropEffect = Blur(5);
            backdropVisual.BackdropEffectCache = MakeRetainedCacheMode(h.Compositor);
            h.Root.Children.Add(backdropVisual);
            return backdropVisual;
        }

        using var reference = new Harness(200, 200);
        Build(reference, 90);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var backdropVisual = Build(h, 10);
        h.Frame();
        h.Frame();

        h.Retained(true);
        backdropVisual.Offset = new Vector3D(90, 20, 0);
        h.Frame();
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Backdrop_Parent_Opacity_Change_Equals_Full_Redraw()
    {
        // The parent's own content is behind the backdrop visual. Its opacity change has to reach the backdrop
        // visual, even when nothing else is dirty.
        CompositionSolidColorVisual Build(Harness h, float opacity)
        {
            h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
            var ancestor = h.Rect(Colors.Blue, 20, 20, 120, 120);
            ancestor.Opacity = opacity;
            var backdropVisual = h.Rect(Colors.Transparent, 20, 20, 80, 80);
            backdropVisual.BackdropEffect = Blur(5);
            backdropVisual.BackdropEffectCache = MakeRetainedCacheMode(h.Compositor);
            ancestor.Children.Add(backdropVisual);
            h.Root.Children.Add(ancestor);
            return ancestor;
        }

        using var reference = new Harness(200, 200);
        Build(reference, 0.3f);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var ancestor = Build(h, 1);
        h.Frame();
        h.Frame();

        h.Retained(true);
        ancestor.Opacity = 0.3f;
        h.Frame();
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Backdrop_Earlier_Sibling_Removal_Equals_Full_Redraw()
    {
        // The dirty rect of the removed visual is collected after the update walk has passed the backdrop visual.
        (CompositionContainerVisual panel, CompositionSolidColorVisual? behind) Build(Harness h, bool withBehind)
        {
            h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
            var panel = h.Compositor.CreateContainerVisual();
            panel.ClipToBounds = false;
            CompositionSolidColorVisual? behind = null;
            if (withBehind)
            {
                behind = h.Rect(Colors.Red, 40, 40, 40, 40);
                panel.Children.Add(behind);
            }

            var backdropVisual = h.Rect(Colors.Transparent, 20, 20, 100, 100);
            backdropVisual.BackdropEffect = Blur(5);
            backdropVisual.BackdropEffectCache = MakeRetainedCacheMode(h.Compositor);
            panel.Children.Add(backdropVisual);
            h.Root.Children.Add(panel);
            return (panel, behind);
        }

        using var reference = new Harness(200, 200);
        Build(reference, false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var (panel, behind) = Build(h, true);
        h.Frame();
        h.Frame();

        h.Retained(true);
        panel.Children.Remove(behind!);
        h.Frame();
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rotated_Backdrop_Is_Limited_To_The_Rotated_Bounds(bool retained)
    {
        using var h = new Harness(200, 200);
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
        h.Root.Children.Add(h.Rect(Colors.Black, 0, 0, 60, 200)); // hard edge at x = 60

        // An 80x80 square centred at (100,100), rotated 45 degrees: a diamond with a 56.6 half-diagonal.
        var backdropVisual = h.Rect(Colors.Transparent, 60, 60, 80, 80);
        backdropVisual.ClipToBounds = false;
        backdropVisual.CenterPoint = new Vector3D(40, 40, 0);
        backdropVisual.RotationAngle = (float)(Math.PI / 4);
        backdropVisual.BackdropEffect = Blur(8);
        backdropVisual.BackdropEffectCache = MakeBackdropCacheMode(h.Compositor, retained);
        h.Root.Children.Add(backdropVisual);
        h.Frame();
        h.Frame();

        // (60,100) is inside the diamond: the edge is blurred there.
        var inside = h.Px(60, 100);
        Assert.InRange((int)inside.Red, 60, 200);

        // (58..61, 55) is inside the AABB of the diamond but outside of the diamond: the edge stays hard.
        Assert.True(h.Px(58, 55).Red < 10, $"left={h.Px(58, 55)}");
        Assert.True(h.Px(61, 55).Red > 245, $"right={h.Px(61, 55)}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Change_Just_Outside_Backdrop_Bounds_Equals_Full_Redraw(bool retained)
    {
        // A visual moves next to the backdrop visual: closer to its edge than the blur radius, but not over it.
        CompositionSolidColorVisual Build(Harness h, double neighbourX)
        {
            h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
            var next = h.Rect(Colors.Red, neighbourX, 40, 10, 80);
            h.Root.Children.Add(next);
            var backdropVisual = h.Rect(Colors.Transparent, 40, 40, 80, 80);
            backdropVisual.BackdropEffect = Blur(8);
            backdropVisual.BackdropEffectCache = MakeBackdropCacheMode(h.Compositor, retained);
            h.Root.Children.Add(backdropVisual);
            return next;
        }

        using var reference = new Harness(200, 200);
        Build(reference, 29); // x 29..39, the backdrop bounds start at 40
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var next = Build(h, 0);
        h.Frame();
        h.Frame();

        h.Retained(true);
        next.Offset = new Vector3D(29, 40, 0);
        h.Frame();
        h.Frame();

        // Compare the edge pixels directly: the strip is too small to show in a whole-image error.
        for (var x = 40; x < 46; x++)
        {
            var expected = reference.Px(x, 80);
            var actual = h.Px(x, 80);
            Assert.True(Math.Abs(expected.Green - actual.Green) <= 8, $"x={x}: expected {expected}, actual {actual}");
        }
    }

    // Stripes behind a backdrop visual with subtree bounds. Its only child defines the bounds.
    private static CompositionSolidColorVisual BuildSubtreeBoundsScene(Harness h, bool retained, double childWidth)
    {
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
        for (var x = 0; x < 200; x += 20)
            h.Root.Children.Add(h.Rect(Colors.Black, x, 0, 10, 200));

        var container = h.Compositor.CreateContainerVisual();
        container.ClipToBounds = false;
        container.Offset = new Vector3D(10, 40, 0);
        container.BackdropEffect = Blur(8);
        container.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;
        container.BackdropEffectCache = MakeBackdropCacheMode(h.Compositor, retained);
        var child = h.Rect(Color.FromArgb(0x20, 0xFF, 0, 0), 0, 0, childWidth, 100);
        container.Children.Add(child);
        h.Root.Children.Add(container);
        return child;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Subtree_Bounds_Backdrop_Follows_Child_Resize_In_The_Same_Frame(bool retained)
    {
        using var reference = new Harness(200, 200);
        BuildSubtreeBoundsScene(reference, retained, 180);
        for (var i = 0; i < 3; i++)
            reference.Frame();

        using var h = new Harness(200, 200);
        var child = BuildSubtreeBoundsScene(h, retained, 20);
        for (var i = 0; i < 3; i++)
            h.Frame();

        h.Retained(true);
        child.Size = new Vector(180, 100);
        h.Frame(); // one frame: the effect has to cover the new bounds already

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Subtree_Bounds_Backdrop_Appears_In_The_First_Frame(bool retained)
    {
        using var reference = new Harness(200, 200);
        BuildSubtreeBoundsScene(reference, retained, 180);
        for (var i = 0; i < 3; i++)
            reference.Frame();

        using var h = new Harness(200, 200);
        BuildSubtreeBoundsScene(h, retained, 180);
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Layout_Bounds_Backdrop_Visual_Without_Content_Draws_The_Effect(bool retained)
    {
        using var h = new Harness(200, 200);
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
        h.Root.Children.Add(h.Rect(Colors.Black, 0, 0, 100, 200)); // hard edge at x = 100

        // No content and no children: only the layout rect says where the effect is.
        var backdropVisual = h.Compositor.CreateContainerVisual();
        backdropVisual.Offset = new Vector3D(60, 60, 0);
        backdropVisual.Size = new Vector(80, 80);
        backdropVisual.BackdropEffect = Blur(8);
        backdropVisual.BackdropEffectCache = MakeBackdropCacheMode(h.Compositor, retained);
        h.Root.Children.Add(backdropVisual);
        h.Frame();
        h.Frame();

        var edge = h.Px(100, 100);
        Assert.InRange((int)edge.Red, 60, 200);
        // Outside of the layout rect the edge stays hard.
        Assert.True(h.Px(98, 30).Red < 10 && h.Px(101, 30).Red > 245, $"{h.Px(98, 30)} {h.Px(101, 30)}");
    }

    [Fact]
    public void Volatile_Overlap_Equals_Full_Redraw()
    {
        using var reference = new Harness(200, 200);
        BuildScene(reference, behindY: 120, caretX: 100, volatileBackdrop: true);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var (behind, _) = BuildScene(h, behindY: 40, caretX: 100, volatileBackdrop: true);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Retained(true);
        behind.Offset = new Vector3D(20, 120, 0);
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Behind_Change_Repaints_Only_The_Output_Dirty_Rect_Not_The_Whole_Aabb()
    {
        using var h = new Harness(200, 200);
        var (behind, _) = BuildScene(h, behindY: 45, caretX: 100);
        h.Retained(false);
        h.Frame();
        h.Frame();

        // Erase, report the frame as retained, move `behind` a little: only the bounds of `behind` and the
        // output dirty rect are redrawn.
        h.Erase();
        h.Retained(true);
        behind.Offset = new Vector3D(20, 50, 0);
        h.Frame();

        // The AABB is (40,40)-(160,160), `behind` covers (20,45)-(60,90).
        // A pixel of the effect far from the change (bottom right of the AABB) is not redrawn.
        Assert.False(IsPainted(h.Px(150, 150)), "retained backdropVisual far from the change should stay erased");
        // The changed area is redrawn.
        Assert.True(IsPainted(h.Px(45, 70)), "the damaged/halo area should be repainted");
    }

    [Fact]
    public void Volatile_Overlap_Repaints_The_Whole_Aabb()
    {
        using var h = new Harness(200, 200);
        var (behind, _) = BuildScene(h, behindY: 45, caretX: 100, volatileBackdrop: true);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Erase();
        h.Retained(true);
        behind.Offset = new Vector3D(20, 50, 0);
        h.Frame();

        // Whole-AABB invalidation: every pixel of the volatile AABB (40,40)-(160,160) is redrawn, including the
        // far corner that the retained case above leaves alone.
        Assert.True(IsPainted(h.Px(150, 150)), "volatile backdropVisual must repaint its whole AABB");
        Assert.True(IsPainted(h.Px(45, 45)), "volatile backdropVisual must repaint its whole AABB");
    }

    [Fact]
    public void Retained_Front_Change_Does_Not_Repaint_Backdrop_Interior()
    {
        using var h = new Harness(200, 200);
        var (_, caret) = BuildScene(h, behindY: 40, caretX: 70);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Erase();
        h.Retained(true);
        caret.Offset = new Vector3D(120, 60, 0); // a move in front, both positions inside the AABB
        h.Frame();

        // Only the old and new caret columns are redrawn, not the effect between them.
        Assert.True(IsPainted(h.Px(72, 80)), "old caret column repainted");
        Assert.True(IsPainted(h.Px(122, 80)), "new caret column repainted");
        Assert.False(IsPainted(h.Px(100, 150)), "backdropVisual interior away from the caret should stay erased");
    }

    [Fact]
    public void Surfaceless_Context_Skips_Backdrop_Without_Crashing()
    {
        using var h = new Harness(200, 200);
        var backdropVisual = h.Rect(Colors.Transparent, 40, 40, 120, 120);
        backdropVisual.BackdropEffect = Blur(5);
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
        h.Root.Children.Add(backdropVisual);
        h.Frame();
        h.Frame();

        var state = backdropVisual.Server.BackdropState;
        Assert.NotNull(state);
        Assert.NotNull(state!.Aabb); // with an AABB, drawing gets as far as the SupportsBackdrop check

        // Draw the server tree into a context without a surface (as for PDF or picture recording): the backdrop
        // effect is skipped without an exception.
        using var bitmap = new SKBitmap(200, 200, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        var ctx = (IDrawingContextImpl)DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
        Assert.False(((IDrawingContextImplWithBackdropSupport)ctx).SupportsBackdrop);

        var serverRoot = h.Renderer.CompositionTarget.Server.Root!;
        ctx.Transform = Matrix.Identity;
        var ex = Record.Exception(() => serverRoot.Render(ctx, new LtrbRect(0, 0, 200, 200), null));
        Assert.Null(ex);
    }

    // A container with an effect, which opens an intermediate layer for its whole subtree. A red/blue split is
    // drawn into that intermediate layer, and a visual over it can have a backdrop effect. The split is not in
    // the root layer, so only the volatile path can read it, and the retained path has to skip. The container's
    // effect has a zero radius: it opens the intermediate layer but does not blur, so the split stays hard
    // unless the backdrop effect blurs it. Returns the visual over the split.
    private static CompositionSolidColorVisual BuildAncestorEffectTree(Harness h, bool withBackdrop,
        bool volatileBackdrop)
    {
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200)); // opaque base

        var container = h.Compositor.CreateContainerVisual();
        container.ClipToBounds = false;
        container.Offset = new Vector3D(40, 40, 0);
        container.Effect = Blur(0); // opens an intermediate layer, no blur of its own
        container.Children.Add(h.Rect(Colors.Red, 0, 0, 60, 120));   // drawn into the intermediate layer
        container.Children.Add(h.Rect(Colors.Blue, 60, 0, 60, 120)); // split at container x 60, root x 100

        var inner = h.Rect(Colors.Transparent, 0, 0, 120, 120); // covers the split
        if (withBackdrop)
        {
            inner.BackdropEffect = Blur(8);
            if (volatileBackdrop)
                inner.BackdropEffectCache = MakeVolatileCacheMode(h.Compositor);
        }
        container.Children.Add(inner);
        h.Root.Children.Add(container);
        return inner;
    }

    [Fact]
    public void Volatile_Backdrop_Under_Parent_Effect_Samples_Through_And_Renders()
    {
        // The parent's effect opens an intermediate layer. The volatile path reads through it, so the backdrop
        // effect is drawn correctly and equals a full redraw of the same tree.
        using var reference = new Harness(200, 200);
        BuildAncestorEffectTree(reference, withBackdrop: true, volatileBackdrop: true);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var inner = BuildAncestorEffectTree(h, withBackdrop: true, volatileBackdrop: true);
        h.Retained(false);
        var ex = Record.Exception(() =>
        {
            h.Frame();
            h.Frame();
        });
        Assert.Null(ex); // no exception

        Assert.NotNull(inner.Server.BackdropState!.Aabb);
        AssertSimilar(h.Fb, reference.Fb); // equals a full redraw of the same tree

        // The split was read through the intermediate layer and blurred: the split column (root x 100) is a
        // red/blue mix, and some colour has spread a few pixels into each half.
        var seam = h.Px(100, 100);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
        Assert.True(h.Px(98, 100).Blue > 30, $"blur should bleed blue left of the seam, got {h.Px(98, 100)}");
        Assert.True(h.Px(102, 100).Red > 30, $"blur should bleed red right of the seam, got {h.Px(102, 100)}");
    }

    [Fact]
    public void Retained_Backdrop_Under_Parent_Effect_Is_Skipped_Not_Garbage()
    {
        // The retained path cannot read the intermediate layer that the parent's effect opened. The backdrop
        // effect is skipped, so the frame equals the same tree without a backdrop effect.
        using var reference = new Harness(200, 200);
        BuildAncestorEffectTree(reference, withBackdrop: false, volatileBackdrop: false);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var inner = BuildAncestorEffectTree(h, withBackdrop: true, volatileBackdrop: false);
        h.Retained(false);
        var ex = Record.Exception(() =>
        {
            h.Frame();
            h.Frame();
        });
        Assert.Null(ex); // no exception

        // There is an AABB (so drawing is attempted), but the backdrop effect is skipped: the frame equals the
        // reference without it, and the split stays a hard red/blue edge.
        Assert.NotNull(inner.Server.BackdropState);
        Assert.NotNull(inner.Server.BackdropState!.Aabb);
        AssertSimilar(h.Fb, reference.Fb);
        Assert.True(h.Px(98, 100).Red > 200 && h.Px(98, 100).Blue < 40, $"seam must stay hard, got {h.Px(98, 100)}");
        Assert.True(h.Px(102, 100).Blue > 200 && h.Px(102, 100).Red < 40, $"seam must stay hard, got {h.Px(102, 100)}");
    }

    [Fact]
    public void Blur_Backdrop_Over_Pattern_Softens_A_Hard_Edge()
    {
        // A hard red/blue split behind a blur backdrop visual: the split column becomes a red/blue mix.
        using var h = new Harness(200, 200);
        h.Root.Children.Add(h.Rect(Colors.Red, 0, 0, 100, 200));
        h.Root.Children.Add(h.Rect(Colors.Blue, 100, 0, 100, 200));
        var backdropVisual = h.Rect(Colors.Transparent, 40, 40, 120, 120);
        backdropVisual.BackdropEffect = Blur(8);
        h.Root.Children.Add(backdropVisual);
        h.Frame();
        h.Frame();

        var seam = h.Px(100, 100); // on the split, inside the backdrop bounds
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);

        // Outside of the backdrop bounds the split stays hard.
        var hardRed = h.Px(60, 20);
        Assert.True(hardRed.Red > 200 && hardRed.Blue < 40, $"expected pure red outside AABB, got {hardRed}");
    }

    [Fact]
    public void Container_With_Children_Renders_Backdrop_Effect_But_Empty_Container_Has_No_Region()
    {
        using var h = new Harness(200, 200);
        h.Root.Children.Add(h.Rect(Colors.Red, 0, 0, 100, 200));
        h.Root.Children.Add(h.Rect(Colors.Blue, 100, 0, 100, 200));

        // With subtree bounds, a container without own content gets its backdrop bounds from its children.
        var container = h.Compositor.CreateContainerVisual();
        container.ClipToBounds = false;
        container.BackdropEffect = Blur(8);
        container.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;
        container.Offset = new Vector3D(40, 40, 0);
        container.Children.Add(h.Rect(Colors.Transparent, 0, 0, 120, 120));
        h.Root.Children.Add(container);

        // The same container without children: no subtree bounds, so no AABB and nothing drawn.
        var empty = h.Compositor.CreateContainerVisual();
        empty.BackdropEffect = Blur(8);
        empty.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;
        h.Root.Children.Add(empty);

        h.Frame();
        h.Frame();

        Assert.NotNull(container.Server.BackdropState!.Aabb); // has bounds from its children
        Assert.Null(empty.Server.BackdropState!.Aabb);        // no content, no bounds

        var seam = h.Px(100, 100);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
    }

    // A visual with a bitmap cache. Inside it: a red/blue pattern and a retained blur backdrop visual, which
    // reads the bitmap cache layer. The content does not start at the origin of the cached visual, so the
    // layer space mapping has an offset.
    private static void BuildCachedSplitScene(Harness h)
    {
        var cacheHost = h.Compositor.CreateContainerVisual();
        cacheHost.ClipToBounds = false;
        cacheHost.CacheMode = MakeCache(h.Compositor);
        cacheHost.Children.Add(h.Rect(Colors.Red, 20, 20, 60, 120));
        cacheHost.Children.Add(h.Rect(Colors.Blue, 80, 20, 60, 120)); // split at x = 80
        var backdropVisual = h.Rect(Colors.Transparent, 20, 20, 120, 120);
        backdropVisual.BackdropEffect = Blur(8);
        cacheHost.Children.Add(backdropVisual);
        h.Root.Children.Add(cacheHost);
    }

    [Fact]
    public void Retained_Backdrop_Inside_BitmapCache_Over_Pattern_Equals_Full_Redraw()
    {
        using var reference = new Harness(200, 200);
        BuildCachedSplitScene(reference);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        BuildCachedSplitScene(h);
        h.Retained(false);
        h.Frame();
        h.Frame();
        h.Frame();

        // The backdrop visual blurred the bitmap cache layer: the split is mixed, the halves are not.
        var seam = h.Px(80, 80);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
        var red = h.Px(35, 80);
        Assert.True(red.Red > 180 && red.Blue < 60, $"expected red-dominant inside the cache blur, got {red}");
        var blue = h.Px(125, 80);
        Assert.True(blue.Blue > 180 && blue.Red < 60, $"expected blue-dominant inside the cache blur, got {blue}");

        // The whole frame equals a full redraw of the same scene.
        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Backdrop_Inside_BitmapCache_Behind_Change_Equals_Full_Redraw()
    {
        // A movable `behind` square and a retained blur backdrop visual, both inside the bitmap cache. The square
        // stays over the opaque base, so the bounds of the cached subtree (and the layer size) do not change.
        static CompositionSolidColorVisual Build(Harness h, double moverY)
        {
            var cacheHost = h.Compositor.CreateContainerVisual();
            cacheHost.ClipToBounds = false;
            cacheHost.CacheMode = MakeCache(h.Compositor);
            cacheHost.Children.Add(h.Rect(Colors.White, 20, 20, 120, 120));
            var mover = h.Rect(Colors.Red, 40, moverY, 40, 40); // earlier in draw order: behind the backdrop visual
            cacheHost.Children.Add(mover);
            var backdropVisual = h.Rect(Colors.Transparent, 20, 20, 120, 120);
            backdropVisual.BackdropEffect = Blur(8);
            cacheHost.Children.Add(backdropVisual);
            h.Root.Children.Add(cacheHost);
            return mover;
        }

        using var reference = new Harness(200, 200);
        Build(reference, moverY: 90);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var mover = Build(h, moverY: 40);
        h.Retained(false);
        h.Frame();
        h.Frame();
        h.Frame();

        // Move the content behind, inside the bitmap cache. The snapshot is refreshed from the bitmap cache layer,
        // and the frame equals a full redraw of the final state.
        h.Retained(true);
        mover.Offset = new Vector3D(40, 90, 0);
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    // A transparent backdrop visual over a red square on a transparent target. The shadow is offset by 20.
    private static CompositionSolidColorVisual BuildDropShadowScene(Harness h, bool retained, double squareY)
    {
        var square = h.Rect(Colors.Red, 70, squareY, 30, 30);
        var backdropVisual = h.Rect(Colors.Transparent, 30, 30, 80, 80);
        backdropVisual.ClipToBounds = false; // as for a control: only the backdrop bounds limit the shadow
        backdropVisual.BackdropEffect = DropShadow(20, 0);
        backdropVisual.BackdropEffectCache = MakeBackdropCacheMode(h.Compositor, retained);
        h.Root.Children.Add(square);
        h.Root.Children.Add(backdropVisual);
        return square;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropShadow_Backdrop_Shadows_Translucent_Backdrop_Clipped_To_Bounds(bool retained)
    {
        // Same as CSS backdrop-filter: drop-shadow(): the shadow shows only where the backdrop is transparent, and
        // is clipped to the backdrop bounds (30..110).
        using var h = new Harness(200, 200);
        BuildDropShadowScene(h, retained, squareY: 70);
        h.Frame();
        h.Frame();

        var square = h.Px(80, 80);
        Assert.True(square.Red > 250 && square.Green < 5, $"square={square}");
        var shadow = h.Px(105, 105); // the shadow of the square covers 90..120
        Assert.True(shadow.Alpha == 255 && shadow.Red < 5, $"shadow={shadow}");
        Assert.False(IsPainted(h.Px(115, 115)), "shadow drawn outside the backdropVisual bounds");
        Assert.False(IsPainted(h.Px(60, 60)), "shadow drawn where the backdropVisual has nothing to shadow");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropShadow_Backdrop_Over_Opaque_Content_Equals_No_Backdrop(bool retained)
    {
        // Opaque backdrop pixels cover their own shadow, and the edges of the bounds cast no shadow outside.
        void Build(Harness h, bool withBackdrop)
        {
            h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
            h.Root.Children.Add(h.Rect(Colors.Blue, 0, 60, 200, 30));
            var backdropVisual = h.Rect(Colors.Transparent, 40, 40, 120, 120);
            backdropVisual.ClipToBounds = false;
            if (withBackdrop)
            {
                backdropVisual.BackdropEffect = DropShadow(10, 4);
                backdropVisual.BackdropEffectCache = MakeBackdropCacheMode(h.Compositor, retained);
            }

            h.Root.Children.Add(backdropVisual);
        }

        using var reference = new Harness(200, 200);
        Build(reference, false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        Build(h, true);
        h.Frame();
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropShadow_Backdrop_Behind_Change_Equals_Full_Redraw(bool retained)
    {
        // The shadow reads its input at an offset. The output dirty rect has to cover that.
        using var reference = new Harness(200, 200);
        BuildDropShadowScene(reference, retained, squareY: 40);
        reference.Retained(false);
        reference.Frame();
        reference.Frame();

        using var h = new Harness(200, 200);
        var square = BuildDropShadowScene(h, retained, squareY: 70);
        h.Retained(false);
        h.Frame();
        h.Frame();

        h.Retained(true);
        square.Offset = new Vector3D(70, 40, 0);
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Volatile_DropShadow_Backdrop_Does_Not_Clip_Overflowing_Children()
    {
        // The effect is clipped to the layout bounds (30..80). A child outside of them is still drawn.
        using var h = new Harness(200, 200);
        var host = h.Compositor.CreateContainerVisual();
        host.Offset = new Vector3D(30, 30, 0);
        host.Size = new Vector(50, 50);
        host.ClipToBounds = false;
        host.BackdropEffect = DropShadow(10, 0);
        host.BackdropEffectCache = MakeVolatileCacheMode(h.Compositor);
        host.Children.Add(h.Rect(Colors.Green, 60, 60, 30, 30));
        h.Root.Children.Add(host);
        h.Frame();
        h.Frame();

        var child = h.Px(105, 105);
        Assert.True(child.Green > 100 && child.Alpha == 255, $"child={child}");
    }

    [Fact]
    public void Public_BackdropEffect_On_A_Control_Renders()
    {
        // The public Visual.BackdropEffect and BackdropEffectCache properties on a control, drawn by the real
        // compositor and Skia. Checked by pixel, without a reference image.
        const int size = 200;
        using var fb = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);

        ILockedFramebuffer LockFb() => new LockedFramebuffer(fb.GetAddress(0, 0), new(fb.Width, fb.Height),
            fb.RowBytes, new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul, null);
        IFramebufferRenderTarget rt = new FuncFramebufferRenderTarget((_, out props) =>
        {
            props = new() { PreviousFrameIsRetained = false };
            return LockFb();
        }, retainsFrameContents: true);

        var timer = new ManualRenderTimer();
        var compositor = new Compositor(RenderLoop.FromTimer(timer), null, true,
            new DispatcherCompositorScheduler(), true, Dispatcher.UIThread,
            new CompositionOptions { UseRegionDirtyRectClipping = true });

        var frosted = new Border
        {
            Margin = new Thickness(40),
            Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 20,
                Color = Colors.Black
            }),
            BackdropEffect = new BlurEffect { Radius = 10 },
            BackdropEffectCache = new RetainedBackdropEffectCache()
        };
        var content = new Panel
        {
            Width = size,
            Height = size,
            Children =
            {
                new Border { Background = Brushes.Red },
                frosted
            }
        };

        var root = new TestRenderRoot(1, null!);
        using (var renderer = new CompositingRenderer(root, compositor,
                   () => new[] { new FuncFramebufferSurface(() => rt) }))
        {
            root.Initialize(renderer, content);
            renderer.Start();
            for (var i = 0; i < 2; i++)
            {
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                timer.TriggerTick();
            }

            // The shadow makes the subtree bounds larger, but the backdrop bounds are the 120x120 layout rect of the
            // Border by default.
            Assert.Equal(new LtrbPixelRect(40, 40, 160, 160),
                frosted.CompositionVisual!.Server.BackdropState!.Aabb!.Value);
        }

        // Drawn through the compositor: the red content shows through the overlay.
        var center = fb.GetPixel(size / 2, size / 2);
        Assert.NotEqual((byte)0, center.Alpha);
        Assert.True(center.Red > 100, $"expected red-dominant content behind the backdropVisual, got {center}");
    }
}
#endif
