using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Browser.Skia;

internal abstract class BrowserSurface : IDisposable, IRenderTimer
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly BrowserRenderingMode _renderingMode;

    protected BrowserSurface(JSObject jsSurface, BrowserRenderingMode renderingMode)
    {
        _renderingMode = renderingMode;
        JsSurface = jsSurface;

        Scaling = 1;
        ClientSize = new Size(100, 100);
        RenderSize = new PixelSize(100, 100);
    }

    [UnmanagedCallersOnly]
    private static int Frame(double time, nint userData)
    {
        if (userData == 0
            || !(GCHandle.FromIntPtr(userData).Target is BrowserSurface surface))
        {
            return 0;
        }

        surface.Tick?.Invoke(TimeSpan.FromMilliseconds(time));
        return 1;
    }
    
    public bool IsWebGl => _renderingMode is BrowserRenderingMode.WebGL1 or BrowserRenderingMode.WebGL2;

    public JSObject JsSurface { get; private set; }
    public double Scaling { get; private set; }
    public Size ClientSize { get; private set; }
    public PixelSize RenderSize { get; private set; }

    public bool IsValid => RenderSize.Width > 0 && RenderSize.Height > 0 && Scaling > 0;

    public event Action<TimeSpan>? Tick;
    public event Action? SizeChanged;
    public event Action? ScalingChanged;

    public bool RunsInBackground => false;

    public static BrowserSurface Create(JSObject container, PixelFormat pixelFormat)
    {
        var opts = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        if (opts.RenderingMode is null || !opts.RenderingMode.Any())
        {
            throw new InvalidOperationException($"{nameof(BrowserPlatformOptions)}.{nameof(BrowserPlatformOptions.RenderingMode)} must not be empty or null");
        }

        BrowserSurface? surface = null;
        foreach (var mode in opts.RenderingMode)
        {
            try
            {
                var (jsSurface, jsGlInfo) = CanvasHelper.CreateSurface(container, mode);
                surface = jsGlInfo != null
                    ? new BrowserGlSurface(jsSurface, jsGlInfo, pixelFormat, mode)
                    : new BrowserRasterSurface(jsSurface, pixelFormat, mode);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.BrowserPlatform)?
                    .Log(null,
                        "Creation of BrowserSurface with mode {Mode} failed with an error:\r\n{Exception}",
                        mode, ex);
            }
        }

        if (surface is null)
        {
            throw new InvalidOperationException($"{nameof(BrowserPlatformOptions)}.{nameof(BrowserPlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
        }

        CanvasHelper.RequestAnimationFrame(surface.JsSurface, surface.OnTimerTick);
        CanvasHelper.OnSizeChanged(surface.JsSurface, surface.OnSizeChanged);
        return surface;
    }

    public virtual void Dispose()
    {
        CanvasHelper.Destroy(JsSurface);
        JsSurface.Dispose();
        JsSurface = null!;
        RenderSize = default;
        ClientSize = default;
    }

    private void OnTimerTick()
    {
        if (IsValid)
        {
            // TODO: do we need it?
            Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);

            Tick?.Invoke(_sw.Elapsed);   
        }
    }

    private void OnSizeChanged(int pixelWidth, int pixelHeight, double dpr)
    {
        var oldScaling = Scaling;
        var oldClientSize = ClientSize;
        RenderSize = new PixelSize(pixelWidth, pixelHeight);
        ClientSize = RenderSize.ToSize(dpr);
        Scaling = dpr;
        if (oldClientSize != ClientSize)
            SizeChanged?.Invoke();
        if (Math.Abs(oldScaling - dpr) > 0.0001)
            ScalingChanged?.Invoke();
    }
}
