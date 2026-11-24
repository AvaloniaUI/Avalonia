#if AVALONIA_SKIA
using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests;

internal sealed class FuncFramebufferSurface : IFramebufferPlatformSurface
{
    private readonly Func<IFramebufferRenderTarget> _cb;
    public FuncFramebufferSurface(Func<IFramebufferRenderTarget> cb) => _cb = cb;
    public IFramebufferRenderTarget CreateFramebufferRenderTarget() => _cb();
}

// A compositor that draws into an SKBitmap framebuffer. Tests fill `Canvas` with controls or `Root` with
// composition visuals. `Retained` sets PreviousFrameIsRetained for the next frames.
internal sealed class FramebufferCompositionFixture : IDisposable
{
    private readonly ManualRenderTimer _timer = new();
    private readonly CompositingRenderer _renderer;
    private readonly TestRenderRoot _root;
    private bool _retained;

    public Compositor Compositor { get; }
    public CompositionContainerVisual Root { get; }
    public SKBitmap Fb { get; }

    public Canvas Canvas { get; }

    public FramebufferCompositionFixture(int width, int height, double scaling = 1)
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

        Canvas = new Canvas { Width = width, Height = height };
        _root = new TestRenderRoot(scaling, null!);
        _renderer = new CompositingRenderer(_root, Compositor, () => new[] { new FuncFramebufferSurface(() => rt) });
        _root.Initialize(_renderer, Canvas);
        _renderer.Start();

        Root = Compositor.CreateContainerVisual();
        Root.ClipToBounds = false;
        ElementComposition.SetElementChildVisual(Canvas, Root);
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
        _root.LayoutManager.ExecuteLayoutPass();
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
#endif
