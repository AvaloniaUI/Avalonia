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
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests;

// Backdrop rendering, driven through the real compositor + Skia into an SKBitmap framebuffer.
// The core correctness gates are self-contained (no committed reference images): a two-frame partial
// repaint is compared against a full-redraw of the same final state rendered in a fresh compositor, and
// the "erase trick" (wipe the framebuffer between ticks with retention advertised) exposes exactly which
// pixels were repainted. CPU raster => deterministic blur.
public class BackdropCompositionTests : TestBase
{
    public BackdropCompositionTests()
        : base(@"Composition\Backdrop")
    {
    }

    private static ImmutableBlurEffect Blur(double radius) => new(radius);

    private static CompositionBitmapCache MakeCache(Compositor c) =>
        new(c, new ServerCompositionBitmapCache(c.Server));

    private static CompositionVolatileBackdropEffectCacheMode MakeVolatileBackdrop(Compositor c) =>
        new(c, new ServerCompositionVolatileBackdropEffectCacheMode(c.Server));

    private sealed class FuncFramebufferSurface : IFramebufferPlatformSurface
    {
        private readonly Func<IFramebufferRenderTarget> _cb;
        public FuncFramebufferSurface(Func<IFramebufferRenderTarget> cb) => _cb = cb;
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => _cb();
    }

    // Owns a compositor rendering into an SKBitmap framebuffer, plus a single attached container visual
    // (`Root`) the tests populate. `Retained` toggles the per-frame PreviousFrameIsRetained advertisement.
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

    // Shared render-test RMS error budget; the backdrop frames are near-identical to their full-redraw
    // reference, so a real mismatch blows well past it.
    private const double AllowedError = 0.022;

    private static SixLabors.ImageSharp.Image<Rgba32> ToImage(SKBitmap bmp) =>
        SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(bmp.Bytes, bmp.Width, bmp.Height);

    private static void AssertSimilar(SKBitmap actual, SKBitmap reference)
    {
        using var a = ToImage(actual);
        using var b = ToImage(reference);
        var error = TestRenderHelper.CompareImages(a, b);
        Assert.True(error <= AllowedError, $"Frame differs from full-redraw reference: error {error} > {AllowedError}");
    }

    // Populates a scene: an opaque base, a movable "behind" square (earlier DFS), a retained blur backdrop
    // covering the middle, and a "caret" square in front (later DFS). Returns the mutable pieces.
    private static (CompositionSolidColorVisual behind, CompositionSolidColorVisual caret) BuildScene(
        Harness h, double behindY, double caretX, bool volatileBackdrop = false)
    {
        var baseRect = h.Rect(Colors.White, 0, 0, 200, 200);
        var stripe = h.Rect(Colors.Blue, 0, 0, 200, 30); // static pattern for the blur to smear
        var behind = h.Rect(Colors.Red, 20, behindY, 40, 40);

        var backdrop = h.Rect(Colors.Transparent, 40, 40, 120, 120);
        backdrop.BackdropEffect = Blur(5); // R = ceil(5)+1 = 6 device px
        if (volatileBackdrop)
            backdrop.BackdropEffectCache = MakeVolatileBackdrop(h.Compositor);

        var caret = h.Rect(Colors.Black, caretX, 60, 6, 40); // in front, over the backdrop

        h.Root.Children.Add(baseRect);
        h.Root.Children.Add(stripe);
        h.Root.Children.Add(behind);
        h.Root.Children.Add(backdrop);
        h.Root.Children.Add(caret);
        return (behind, caret);
    }

    [Fact]
    public void Retained_Front_Change_Equals_Full_Redraw()
    {
        // caret moves (front-only); the retained backdrop must not sample the caret => equals a full redraw.
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
        caret.Offset = new Vector3D(100, 60, 0); // front-only change
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
        behind.Offset = new Vector3D(20, 120, 0); // behind change, re-ingested into the retained texture
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Behind_Change_Equals_Full_Redraw_At_Scaling_2()
    {
        // Scaling 2 folds the root DPI scale into both the registry AABB and the draw matrix; a wrong fold
        // shows up as a mismatch against the full redraw.
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
    public void Retained_Behind_Change_Repaints_Only_The_Halo_Not_The_Whole_Aabb()
    {
        using var h = new Harness(200, 200);
        var (behind, _) = BuildScene(h, behindY: 45, caretX: 100);
        h.Retained(false);
        h.Frame();
        h.Frame();

        // Erase, advertise retention, move `behind` a little: only D ∪ ((D∩AABB)⊕R)∩AABB is repainted.
        h.Erase();
        h.Retained(true);
        behind.Offset = new Vector3D(20, 50, 0);
        h.Frame();

        // The backdrop AABB is (40,40)-(160,160). `behind` old∪new is (20,45)-(60,90); the halo stays near it.
        // A backdrop pixel far from the change (bottom-right of the AABB) must NOT have been repainted.
        Assert.False(IsPainted(h.Px(150, 150)), "retained backdrop far from the change should stay erased");
        // The changed area itself is repainted.
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

        // Whole-visual invalidation: every pixel of the volatile AABB (40,40)-(160,160) is repainted,
        // including the far corner untouched by the retained case above.
        Assert.True(IsPainted(h.Px(150, 150)), "volatile backdrop must repaint its whole AABB");
        Assert.True(IsPainted(h.Px(45, 45)), "volatile backdrop must repaint its whole AABB");
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
        caret.Offset = new Vector3D(120, 60, 0); // front-only move, both positions inside the AABB
        h.Frame();

        // Only the caret's old/new columns are touched; the backdrop interior between them is not repainted.
        Assert.True(IsPainted(h.Px(72, 80)), "old caret column repainted");
        Assert.True(IsPainted(h.Px(122, 80)), "new caret column repainted");
        Assert.False(IsPainted(h.Px(100, 150)), "backdrop interior away from the caret should stay erased");
    }

    [Fact]
    public void Surfaceless_Context_Skips_Backdrop_Without_Crashing()
    {
        using var h = new Harness(200, 200);
        var backdrop = h.Rect(Colors.Transparent, 40, 40, 120, 120);
        backdrop.BackdropEffect = Blur(5);
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200));
        h.Root.Children.Add(backdrop);
        h.Frame();
        h.Frame();

        var record = backdrop.Server.BackdropState;
        Assert.NotNull(record);
        Assert.NotNull(record!.Aabb); // AABB established, so the draw site reaches the SupportsBackdrop guard

        // Re-render the live server tree into a canvas-only (surface-less) context: the backdrop draw site
        // must detect SupportsBackdrop == false and skip without throwing (PDF pages / picture recording path).
        using var bitmap = new SKBitmap(200, 200, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        var ctx = (IDrawingContextImpl)DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
        Assert.False(((IDrawingContextImplWithBackdropSupport)ctx).SupportsBackdrop);

        var serverRoot = h.Renderer.CompositionTarget.Server.Root!;
        ctx.Transform = Matrix.Identity;
        var ex = Record.Exception(() => serverRoot.Render(ctx, new LtrbRect(0, 0, 200, 200), null));
        Assert.Null(ex);
    }

    // A container with an ancestor Effect whose SaveLayer is active over the whole subtree. A red/blue split is
    // drawn INTO that layer, then a nested visual over it optionally carries a backdrop. Because the split lives
    // in the effect's offscreen (not the root surface), only the volatile SaveLayerRec path can sample it; the
    // retained path (which reads the root Surface) must skip. A zero-radius ancestor Effect is a visually-neutral
    // way to force that active intermediate save-layer, so the seam stays hard unless the backdrop itself blurs
    // it. Returns the nested (backdrop) visual.
    private static CompositionSolidColorVisual BuildAncestorEffectTree(Harness h, bool withBackdrop,
        bool volatileBackdrop)
    {
        h.Root.Children.Add(h.Rect(Colors.White, 0, 0, 200, 200)); // opaque base

        var container = h.Compositor.CreateContainerVisual();
        container.ClipToBounds = false;
        container.Offset = new Vector3D(40, 40, 0);
        container.Effect = Blur(0); // active intermediate save-layer over the subtree, but no blur of its own
        container.Children.Add(h.Rect(Colors.Red, 0, 0, 60, 120));   // drawn INTO the effect layer
        container.Children.Add(h.Rect(Colors.Blue, 60, 0, 60, 120)); // seam at container-x 60 => root-x 100

        var inner = h.Rect(Colors.Transparent, 0, 0, 120, 120); // covers the split, gives the backdrop a region
        if (withBackdrop)
        {
            inner.BackdropEffect = Blur(8);
            if (volatileBackdrop)
                inner.BackdropEffectCache = MakeVolatileBackdrop(h.Compositor);
        }
        container.Children.Add(inner);
        h.Root.Children.Add(container);
        return inner;
    }

    [Fact]
    public void Volatile_Backdrop_Under_Ancestor_Effect_Samples_Through_And_Renders()
    {
        // The ancestor Effect redirects drawing to an offscreen; SaveLayerRec.Backdrop captures that offscreen
        // (through the layer) and blurs it, so the nested volatile backdrop renders correctly. Deterministic =>
        // equals a fresh full redraw of the same tree.
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
        Assert.Null(ex); // no crash / garbage

        Assert.NotNull(inner.Server.BackdropState!.Aabb);
        AssertSimilar(h.Fb, reference.Fb); // deterministic: matches a fresh redraw of the same tree

        // The backdrop sampled the split through the ancestor layer and blurred it: the seam (root-x 100) is now
        // a red/blue mix, and the blur has bled colour a couple of pixels into each half (impossible for a hard
        // seam, where those pixels stay pure).
        var seam = h.Px(100, 100);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
        Assert.True(h.Px(98, 100).Blue > 30, $"blur should bleed blue left of the seam, got {h.Px(98, 100)}");
        Assert.True(h.Px(102, 100).Red > 30, $"blur should bleed red right of the seam, got {h.Px(102, 100)}");
    }

    [Fact]
    public void Retained_Backdrop_Under_Ancestor_Effect_Is_Skipped_Not_Garbage()
    {
        // The retained path samples the root Surface, which the ancestor Effect's SaveLayer has diverted away
        // from; sampling it would blit garbage. The draw site detects the active save-layer and skips (degrade
        // to no-blur) => the frame equals the same tree without a backdrop.
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
        Assert.Null(ex); // no crash / garbage

        // The AABB is established (the draw site is actually reached), yet the backdrop is skipped, so the frame
        // matches the no-backdrop reference: the seam stays a hard red/blue edge with no blur bleed.
        Assert.NotNull(inner.Server.BackdropState);
        Assert.NotNull(inner.Server.BackdropState!.Aabb);
        AssertSimilar(h.Fb, reference.Fb);
        Assert.True(h.Px(98, 100).Red > 200 && h.Px(98, 100).Blue < 40, $"seam must stay hard, got {h.Px(98, 100)}");
        Assert.True(h.Px(102, 100).Blue > 200 && h.Px(102, 100).Red < 40, $"seam must stay hard, got {h.Px(102, 100)}");
    }

    [Fact]
    public void Blur_Backdrop_Over_Pattern_Softens_A_Hard_Edge()
    {
        // A hard red/blue split behind a blur backdrop: the seam column becomes a red/blue mix.
        using var h = new Harness(200, 200);
        h.Root.Children.Add(h.Rect(Colors.Red, 0, 0, 100, 200));
        h.Root.Children.Add(h.Rect(Colors.Blue, 100, 0, 100, 200));
        var backdrop = h.Rect(Colors.Transparent, 40, 40, 120, 120);
        backdrop.BackdropEffect = Blur(8);
        h.Root.Children.Add(backdrop);
        h.Frame();
        h.Frame();

        var seam = h.Px(100, 100); // on the seam, inside the blurred region
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);

        // Outside the blurred AABB the split stays hard.
        var hardRed = h.Px(60, 20);
        Assert.True(hardRed.Red > 200 && hardRed.Blue < 40, $"expected pure red outside AABB, got {hardRed}");
    }

    [Fact]
    public void Container_Host_With_Children_Renders_Backdrop_But_Empty_Container_Has_No_Region()
    {
        using var h = new Harness(200, 200);
        h.Root.Children.Add(h.Rect(Colors.Red, 0, 0, 100, 200));
        h.Root.Children.Add(h.Rect(Colors.Blue, 100, 0, 100, 200));

        // A bare container (no own content) derives its backdrop region from its children's union.
        var container = h.Compositor.CreateContainerVisual();
        container.ClipToBounds = false;
        container.BackdropEffect = Blur(8);
        container.Offset = new Vector3D(40, 40, 0);
        container.Children.Add(h.Rect(Colors.Transparent, 0, 0, 120, 120));
        h.Root.Children.Add(container);

        // A sibling container with the same backdrop but no children: null subtree bounds => no AABB, no draw.
        var empty = h.Compositor.CreateContainerVisual();
        empty.BackdropEffect = Blur(8);
        h.Root.Children.Add(empty);

        h.Frame();
        h.Frame();

        Assert.NotNull(container.Server.BackdropState!.Aabb); // children give it a region
        Assert.Null(empty.Server.BackdropState!.Aabb);        // content-less container => no region

        var seam = h.Px(100, 100);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
    }

    // A cache host with a red/blue pattern and a retained blur backdrop, all INSIDE the cache. The backdrop's
    // host is the cache, so it samples the cache surface via GetDirtyRectSpaceMapping (the content is
    // offset from the cache-local origin, so the mapping has a non-zero translation).
    private static void BuildCachedSplitScene(Harness h)
    {
        var cacheHost = h.Compositor.CreateContainerVisual();
        cacheHost.ClipToBounds = false;
        cacheHost.CacheMode = MakeCache(h.Compositor);
        cacheHost.Children.Add(h.Rect(Colors.Red, 20, 20, 60, 120));
        cacheHost.Children.Add(h.Rect(Colors.Blue, 80, 20, 60, 120)); // seam at x = 80
        var backdrop = h.Rect(Colors.Transparent, 20, 20, 120, 120);
        backdrop.BackdropEffect = Blur(8);
        cacheHost.Children.Add(backdrop);
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

        // The in-cache backdrop actually blurred the cache surface: the seam is mixed, the halves stay pure.
        var seam = h.Px(80, 80);
        Assert.InRange((int)seam.Red, 40, 220);
        Assert.InRange((int)seam.Blue, 40, 220);
        var red = h.Px(35, 80);
        Assert.True(red.Red > 180 && red.Blue < 60, $"expected red-dominant inside the cache blur, got {red}");
        var blue = h.Px(125, 80);
        Assert.True(blue.Blue > 180 && blue.Red < 60, $"expected blue-dominant inside the cache blur, got {blue}");

        // And the whole composited frame equals a fresh full redraw of the same scene.
        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Retained_Backdrop_Inside_BitmapCache_Behind_Change_Equals_Full_Redraw()
    {
        // A movable "behind" square + a retained blur backdrop, both inside the cache. The mover stays within
        // the opaque base so the cache subtree bounds (and layer) do not resize.
        static CompositionSolidColorVisual Build(Harness h, double moverY)
        {
            var cacheHost = h.Compositor.CreateContainerVisual();
            cacheHost.ClipToBounds = false;
            cacheHost.CacheMode = MakeCache(h.Compositor);
            cacheHost.Children.Add(h.Rect(Colors.White, 20, 20, 120, 120));
            var mover = h.Rect(Colors.Red, 40, moverY, 40, 40); // earlier DFS => behind the backdrop
            cacheHost.Children.Add(mover);
            var backdrop = h.Rect(Colors.Transparent, 20, 20, 120, 120);
            backdrop.BackdropEffect = Blur(8);
            cacheHost.Children.Add(backdrop);
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

        // Move the behind content inside the cache; the in-cache retained backdrop must re-ingest from the
        // cache surface (cache-host capture => render) and match a fresh full redraw of the final state.
        h.Retained(true);
        mover.Offset = new Vector3D(40, 90, 0);
        h.Frame();

        AssertSimilar(h.Fb, reference.Fb);
    }

    [Fact]
    public void Public_BackdropEffect_On_A_Control_Renders()
    {
        // The public Visual.BackdropEffect / BackdropEffectCache API on a control, rendered over live
        // content through the real compositor + Skia. Programmatic assertion only (no reference image).
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
            BackdropEffect = new BlurEffect { Radius = 10 },
            BackdropEffectCache = new RetainedBackdropEffectCacheMode()
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
        }

        // The scene rendered through the compositor: red content shows through the frosted overlay.
        var center = fb.GetPixel(size / 2, size / 2);
        Assert.NotEqual((byte)0, center.Alpha);
        Assert.True(center.Red > 100, $"expected red-dominant content behind the backdrop, got {center}");
    }
}
#endif
