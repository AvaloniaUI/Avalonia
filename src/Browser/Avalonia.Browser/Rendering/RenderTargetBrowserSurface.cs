using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Skia;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.Browser.Rendering;

internal class RenderTargetBrowserSurface : BrowserSurface
{
    private readonly BrowserPlatformGraphics _graphics;

    private record InitParams(Compositor Compositor, BrowserPlatformGraphics Graphics);

    private static InitParams CreateCompositor(JSObject jsSurface)
    {
        var targetId = jsSurface.GetPropertyAsInt32("targetId");
        var graphics = new BrowserPlatformGraphics(targetId);
        var compositor = new Compositor(BrowserSharedRenderLoop.RenderLoop.Value, graphics);

        return new(compositor, graphics);
    }

    public RenderTargetBrowserSurface(JSObject jsSurface) : this(jsSurface, CreateCompositor(jsSurface))
    {
        
    }

    public override object[] GetRenderSurfaces()
    {
        if (_graphics.Target == null)
            return [];
        return [_graphics.Target];
    }

    public override void OnSizeChanged(double pixelWidth, double pixelHeight, double dpr)
    {
        _graphics.CanvasSize = (Size: new PixelSize((int)pixelWidth, (int)pixelHeight), Scaling: dpr);
        base.OnSizeChanged(pixelWidth, pixelHeight, dpr);
    }

    private RenderTargetBrowserSurface(JSObject jsSurface, InitParams init) : base(jsSurface, init.Compositor)
    {
        _graphics = init.Graphics;
        base.Initialize();
    }

    class BrowserPlatformGraphics : IPlatformGraphicsWithFeatures, IPlatformGraphicsReadyStateFeature
    {
        private readonly int _targetId;
        private BrowserRenderTarget? _target;

        public BrowserPlatformGraphics(int targetId)
        {
            
            _targetId = targetId;
        }

        public BrowserRenderTarget? Target =>
            _target ??= BrowserRenderTarget.GetRenderTarget(_targetId, () => CanvasSize);

        public bool IsReady => Target != null && CanvasSize.Size != default;
        public bool UsesContexts => Target!.PlatformGraphicsContext != null;
        public bool UsesSharedContext => UsesContexts;
        public (PixelSize Size, double Scaling) CanvasSize { get; set; }

        public IPlatformGraphicsContext CreateContext() => throw new NotSupportedException();

        public IPlatformGraphicsContext GetSharedContext() => Target!.PlatformGraphicsContext ??
                                                              throw new NotSupportedException(
                                                                  "This platform graphics instance represents software rendering mode and cant create contexts, you are supposed to query IPlatformGraphicsReadyStateFeature to know this");

        public object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IPlatformGraphicsReadyStateFeature))
                return this;
            return null;
        }
    }

    public override void Dispose()
    {
        // Technically this is a hack, but CompositionTarget should be gone at this point too
        var c = Compositor;
        Compositor.InvokeServerJobAsync(() =>
        {
            c.Loop.Remove(c.Server);
        });
        
        base.Dispose();
    }

    public static RenderTargetBrowserSurface Create(JSObject container, IReadOnlyList<BrowserRenderingMode> modes, int topLevelId)
    {
        var js = CanvasHelper.CreateRenderTargetSurface(container, modes.Select(m => (int)m).ToArray(), topLevelId, RenderWorker.WorkerThreadId);
        return new RenderTargetBrowserSurface(js);
    }
}
