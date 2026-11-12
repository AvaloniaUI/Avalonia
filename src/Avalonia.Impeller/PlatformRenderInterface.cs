using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Metal;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Vulkan;

namespace Avalonia.Impeller;

/// <summary>
/// The Impeller render interface. Geometry, bitmaps and glyph runs are Avalonia.Skia's CPU-side objects.
/// </summary>
internal class PlatformRenderInterface : IPlatformRenderInterface
{
    private readonly Skia.PlatformRenderInterface _skia = new();
    private readonly ImpellerOptions _options;

    public PlatformRenderInterface(ImpellerOptions options) => _options = options;

    public PixelFormat DefaultPixelFormat => _skia.DefaultPixelFormat;
    public AlphaFormat DefaultAlphaFormat => _skia.DefaultAlphaFormat;

    public bool SupportsIndividualRoundRects => true;

    public bool SupportsRegions => false;

    public IPlatformRenderInterfaceRegion CreateRegion() =>
        throw new NotSupportedException("The Impeller backend doesn't support regions.");

    public bool IsSupportedBitmapPixelFormat(PixelFormat format) => _skia.IsSupportedBitmapPixelFormat(format);

    public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext? graphicsApiContext) =>
        graphicsApiContext switch
        {
            IGlContext gl => ImpellerGlBackendContext.Create(gl),
            IMetalDevice metal => ImpellerMetalBackendContext.Create(metal),
            IVulkanPlatformGraphicsContext vulkan => ImpellerVulkanBackendContext.Create(vulkan, _options),
            null => throw new NotSupportedException("The Impeller backend needs a GPU graphics context."),
            _ => throw new NotSupportedException(
                $"The Impeller backend doesn't support {graphicsApiContext.GetType()} graphics contexts.")
        };

    public IGeometryImpl CreateEllipseGeometry(Rect rect) => _skia.CreateEllipseGeometry(rect);

    public IGeometryImpl CreateLineGeometry(Point p1, Point p2) => _skia.CreateLineGeometry(p1, p2);

    public IGeometryImpl CreateRectangleGeometry(Rect rect) => _skia.CreateRectangleGeometry(rect);

    public IStreamGeometryBuilder CreateStreamGeometryBuilder(IGeometryImpl? source = null) =>
        _skia.CreateStreamGeometryBuilder(source);

    public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children) =>
        _skia.CreateGeometryGroup(fillRule, children);

    public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2) =>
        _skia.CreateCombinedGeometry(combineMode, g1, g2);

    public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun) => _skia.BuildGlyphRunGeometry(glyphRun);

    public IGlyphRunImpl CreateGlyphRun(GlyphTypeface glyphTypeface, double fontRenderingEmSize,
        IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin) =>
        _skia.CreateGlyphRun(glyphTypeface, fontRenderingEmSize, glyphInfos, baselineOrigin);

    public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
    {
        if (size.Width < 1 || size.Height < 1)
            throw new ArgumentException("Width and height can't be less than 1.", nameof(size));
        return new ImpellerRenderTargetBitmap(size, dpi);
    }

    public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format,
        AlphaFormat alphaFormat) => _skia.CreateWriteableBitmap(size, dpi, format, alphaFormat);

    public IBitmapImpl LoadBitmap(string fileName) => _skia.LoadBitmap(fileName);

    public IBitmapImpl LoadBitmap(Stream stream) => _skia.LoadBitmap(stream);

    public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _skia.LoadWriteableBitmapToWidth(stream, width, interpolationMode);

    public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _skia.LoadWriteableBitmapToHeight(stream, height, interpolationMode);

    public IWriteableBitmapImpl LoadWriteableBitmap(string fileName) => _skia.LoadWriteableBitmap(fileName);

    public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream) => _skia.LoadWriteableBitmap(stream);

    public IBitmapImpl LoadBitmapToWidth(Stream stream, int width,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _skia.LoadBitmapToWidth(stream, width, interpolationMode);

    public IBitmapImpl LoadBitmapToHeight(Stream stream, int height,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _skia.LoadBitmapToHeight(stream, height, interpolationMode);

    public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality) =>
        _skia.ResizeBitmap(bitmapImpl, destinationSize, interpolationMode);

    public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size,
        Vector dpi, int stride) => _skia.LoadBitmap(format, alphaFormat, data, size, dpi, stride);
}
