using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Metal;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace RendererBench.Timing;

/// <summary>
/// Forwards to the real backend and times every frame drawn into a window render target.
/// </summary>
internal sealed class TimingRenderInterface : IPlatformRenderInterface
{
    private readonly IPlatformRenderInterface _inner;
    private readonly bool _uncapped;

    private TimingRenderInterface(IPlatformRenderInterface inner, bool uncapped)
    {
        _inner = inner;
        _uncapped = uncapped;
    }

    public static string? GlVendor { get; private set; }
    public static string? GlRenderer { get; private set; }
    public static string? GlVersion { get; private set; }
    public static string GpuTimingMethod { get; private set; } = "none";

    public static void Install(bool uncapped)
    {
        var inner = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>()
            .ToConstant(new TimingRenderInterface(inner, uncapped));
    }

    public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext? graphicsApiContext)
    {
        var context = _inner.CreateBackendContext(graphicsApiContext);

        // Impeller's offscreen worker casts the context to its concrete type; it never draws to a window.
        if (Thread.CurrentThread.Name == "Impeller offscreen")
            return context;

        IGpuTimer? gpu = null;
        if (graphicsApiContext is IGlContext gl)
        {
            GlVendor = gl.GlInterface.Vendor;
            GlRenderer = gl.GlInterface.Renderer;
            GlVersion = gl.GlInterface.Version;
            gpu = GpuTimer.Create(gl);
            GpuTimingMethod = gpu.Method;
        }
        else if (graphicsApiContext is IMetalDevice metal)
        {
            GlVendor = "Apple";
            GlRenderer = MetalDeviceName(metal.Device);
            GlVersion = "Metal";
            gpu = MetalGpuTimer.Create(metal);
            MemoryProbe.SetMetalDevice(metal.Device);
            GpuTimingMethod = gpu.Method;
        }

        return new TimingBackendContext(context, graphicsApiContext, gpu, _uncapped);
    }

    private static string MetalDeviceName(IntPtr device)
    {
        var name = objc_msgSend(device, sel_registerName("name"));
        return Marshal.PtrToStringUTF8(objc_msgSend(name, sel_registerName("UTF8String"))) ?? "unknown";
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);

    public IGeometryImpl CreateEllipseGeometry(Rect rect) => _inner.CreateEllipseGeometry(rect);

    public IGeometryImpl CreateLineGeometry(Point p1, Point p2) => _inner.CreateLineGeometry(p1, p2);

    public IGeometryImpl CreateRectangleGeometry(Rect rect) => _inner.CreateRectangleGeometry(rect);

    public IStreamGeometryBuilder CreateStreamGeometryBuilder(IGeometryImpl? source = null) =>
        _inner.CreateStreamGeometryBuilder(source);

    public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children) =>
        _inner.CreateGeometryGroup(fillRule, children);

    public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1,
        IGeometryImpl g2) => _inner.CreateCombinedGeometry(combineMode, g1, g2);

    public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun) => _inner.BuildGlyphRunGeometry(glyphRun);

    public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi) =>
        _inner.CreateRenderTargetBitmap(size, dpi);

    public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format,
        AlphaFormat alphaFormat) => _inner.CreateWriteableBitmap(size, dpi, format, alphaFormat);

    public IBitmapImpl LoadBitmap(string fileName) => _inner.LoadBitmap(fileName);

    public IBitmapImpl LoadBitmap(Stream stream) => _inner.LoadBitmap(stream);

    public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _inner.LoadWriteableBitmapToWidth(stream, width, interpolationMode);

    public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _inner.LoadWriteableBitmapToHeight(stream, height, interpolationMode);

    public IWriteableBitmapImpl LoadWriteableBitmap(string fileName) => _inner.LoadWriteableBitmap(fileName);

    public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream) => _inner.LoadWriteableBitmap(stream);

    public IBitmapImpl LoadBitmapToWidth(Stream stream, int width,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _inner.LoadBitmapToWidth(stream, width, interpolationMode);

    public IBitmapImpl LoadBitmapToHeight(Stream stream, int height,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _inner.LoadBitmapToHeight(stream, height, interpolationMode);

    public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _inner.ResizeBitmap(bitmapImpl, destinationSize, interpolationMode);

    public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size,
        Vector dpi, int stride) => _inner.LoadBitmap(format, alphaFormat, data, size, dpi, stride);

    public IGlyphRunImpl CreateGlyphRun(GlyphTypeface glyphTypeface, double fontRenderingEmSize,
        IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin) =>
        _inner.CreateGlyphRun(glyphTypeface, fontRenderingEmSize, glyphInfos, baselineOrigin);

    public bool SupportsIndividualRoundRects => _inner.SupportsIndividualRoundRects;

    public AlphaFormat DefaultAlphaFormat => _inner.DefaultAlphaFormat;

    public PixelFormat DefaultPixelFormat => _inner.DefaultPixelFormat;

    public bool IsSupportedBitmapPixelFormat(PixelFormat format) => _inner.IsSupportedBitmapPixelFormat(format);

    public bool SupportsRegions => _inner.SupportsRegions;

    public IPlatformRenderInterfaceRegion CreateRegion() => _inner.CreateRegion();
}

internal sealed class TimingBackendContext : IPlatformRenderInterfaceContext
{
    private readonly IPlatformRenderInterfaceContext _inner;
    private readonly IPlatformGraphicsContext? _graphics;
    private readonly IGpuTimer? _gpu;
    private readonly bool _uncapped;

    public TimingBackendContext(IPlatformRenderInterfaceContext inner, IPlatformGraphicsContext? graphics,
        IGpuTimer? gpu, bool uncapped)
    {
        _inner = inner;
        _graphics = graphics;
        _gpu = gpu;
        _uncapped = uncapped;
    }

    public IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) =>
        new TimingRenderTarget(_inner.CreateRenderTarget(surfaces.Select(s =>
            s is IMetalPlatformSurface metal ? new TimingMetalSurface(metal) : s).ToList()), _graphics, _gpu, _uncapped);

    public IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling,
        bool enableTextAntialiasing) =>
        _inner.CreateOffscreenRenderTarget(pixelSize, scaling, enableTextAntialiasing);

    public bool IsLost => _inner.IsLost;

    public IReadOnlyDictionary<Type, object> PublicFeatures => _inner.PublicFeatures;

    public PixelSize? MaxOffscreenRenderTargetPixelSize => _inner.MaxOffscreenRenderTargetPixelSize;

    public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) =>
        _inner.IsReadyToCreateRenderTarget(surfaces);

    public object? TryGetFeature(Type featureType) => _inner.TryGetFeature(featureType);

    public void Dispose() => _inner.Dispose();
}

internal sealed class TimingRenderTarget : IRenderTarget
{
    private readonly IRenderTarget _inner;
    private readonly IPlatformGraphicsContext? _graphics;
    private readonly IGpuTimer? _gpu;
    private readonly bool _uncapped;
    private bool _swapIntervalSet;

    public TimingRenderTarget(IRenderTarget inner, IPlatformGraphicsContext? graphics, IGpuTimer? gpu,
        bool uncapped)
    {
        _inner = inner;
        _graphics = graphics;
        _gpu = gpu;
        _uncapped = uncapped;
    }

    public RenderTargetProperties Properties => _inner.Properties;

    public PlatformRenderTargetState PlatformRenderTargetState => _inner.PlatformRenderTargetState;

    public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
        out RenderTargetDrawingContextProperties properties)
    {
        var lease = _graphics?.EnsureCurrent();
        var frameId = FrameRecorder.Instance.BeginFrame();
        var start = Stopwatch.GetTimestamp();
        IDrawingContextImpl inner;
        try
        {
            _gpu?.Begin(frameId);
            inner = _inner.CreateDrawingContext(sceneInfo, out properties);
        }
        catch
        {
            _gpu?.End();
            lease?.Dispose();
            throw;
        }

        if (_uncapped && !_swapIntervalSet && _graphics is EglContext egl)
        {
            egl.EglInterface.SwapInterval(egl.Display.Handle, 0);
            _swapIntervalSet = true;
        }

        return new TimingDrawingContext(inner, drawEnd =>
        {
            double? gpuMs;
            try
            {
                inner.Dispose();
            }
            finally
            {
                var presentEnd = Stopwatch.GetTimestamp();
                gpuMs = _gpu?.End();
                lease?.Dispose();
                FrameRecorder.Instance.EndFrame(frameId, start, drawEnd, presentEnd, gpuMs);
            }
        });
    }

    public void Dispose() => _inner.Dispose();
}
