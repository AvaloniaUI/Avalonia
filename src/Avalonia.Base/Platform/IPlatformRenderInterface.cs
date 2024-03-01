using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the main platform-specific interface for the rendering subsystem.
    /// </summary>
    [Unstable, PrivateApi]
    public interface IPlatformRenderInterface
    {
        /// <summary>
        /// Creates an ellipse geometry implementation.
        /// </summary>
        /// <param name="rect">The bounds of the ellipse.</param>
        /// <returns>An ellipse geometry.</returns>
        IGeometryImpl CreateEllipseGeometry(Rect rect);

        /// <summary>
        /// Creates a line geometry implementation.
        /// </summary>
        /// <param name="p1">The start of the line.</param>
        /// <param name="p2">The end of the line.</param>
        /// <returns>A line geometry.</returns>
        IGeometryImpl CreateLineGeometry(Point p1, Point p2);

        /// <summary>
        /// Creates a rectangle geometry implementation.
        /// </summary>
        /// <param name="rect">The bounds of the rectangle.</param>
        /// <returns>A rectangle.</returns>
        IGeometryImpl CreateRectangleGeometry(Rect rect);

        /// <summary>
        /// Creates a stream geometry implementation.
        /// </summary>
        /// <returns>An <see cref="IStreamGeometryImpl"/>.</returns>
        IStreamGeometryImpl CreateStreamGeometry();

        /// <summary>
        /// Creates a geometry group implementation.
        /// </summary>
        /// <param name="fillRule">The fill rule.</param>
        /// <param name="children">The geometries to group.</param>
        /// <returns>A combined geometry.</returns>
        IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children);

        /// <summary>
        /// Creates a geometry group implementation.
        /// </summary>
        /// <param name="combineMode">The combine mode</param>
        /// <param name="g1">The first geometry.</param>
        /// <param name="g2">The second geometry.</param>
        /// <returns>A combined geometry.</returns>
        IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2);

        /// <summary>
        /// Created a geometry implementation for the glyph run.
        /// </summary>
        /// <param name="glyphRun">The glyph run to build a geometry from.</param>
        /// <returns>The geometry returned contains the combined geometry of all glyphs in the glyph run.</returns>
        IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun);

        /// <summary>
        /// Creates a render target bitmap implementation.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <returns>An <see cref="IRenderTargetBitmapImpl"/>.</returns>
        IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi);

        /// <summary>
        /// Creates a writeable bitmap implementation.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">Pixel format.</param>
        /// <param name="alphaFormat">Alpha format .</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat);

        /// <summary>
        /// Loads a bitmap implementation from a file..
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(string fileName);

        /// <summary>
        /// Loads a bitmap implementation from a file..
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(Stream stream);

        /// <summary>
        /// Loads a WriteableBitmap implementation from a stream to a specified width maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        /// <summary>
        /// Loads a WriteableBitmap implementation from a stream to a specified height maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
        
        /// <summary>
        /// Loads a WriteableBitmap implementation from a file.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmap(string fileName);

        /// <summary>
        /// Loads a WriteableBitmap implementation from a file.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>        
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IWriteableBitmapImpl LoadWriteableBitmap(Stream stream);

        /// <summary>
        /// Loads a bitmap implementation from a stream to a specified width maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        /// <summary>
        /// Loads a bitmap implementation from a stream to a specified height maintaining aspect ratio.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param> 
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should resizing be required.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);

        /// <summary>
        /// Loads a bitmap implementation from a pixels in memory.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="alphaFormat">The alpha format.</param>
        /// <param name="data">The pointer to source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride);

        /// <summary>
        /// Creates a platform implementation of a glyph run.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The font rendering em size.</param>
        /// <param name="glyphInfos">The list of glyphs.</param>
        /// <param name="baselineOrigin">The baseline origin of the run. Can be null.</param>
        /// <returns>An <see cref="IGlyphRunImpl"/>.</returns>
        IGlyphRunImpl CreateGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin);

        /// <summary>
        /// Creates a backend-specific object using a low-level API graphics context
        /// </summary>
        /// <param name="graphicsApiContext">An underlying low-level graphics context (e. g. wrapped OpenGL context, Vulkan device, D3DDevice, etc)</param>
        /// <returns></returns>
        IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext? graphicsApiContext);
        
        /// <summary>
        /// Gets a value indicating whether the platform directly supports rectangles with rounded corners.
        /// </summary>
        /// <remarks>
        /// Some platform renderers can't directly handle rounded corners on rectangles.
        /// In this case, code that requires rounded corners must generate and retain a geometry instead.
        /// </remarks>
        bool SupportsIndividualRoundRects { get; }

        /// <summary>
        /// Default <see cref="AlphaFormat"/> used on this platform.
        /// </summary>
        public AlphaFormat DefaultAlphaFormat { get; }

        /// <summary>
        /// Default <see cref="PixelFormat"/> used on this platform.
        /// </summary>
        public PixelFormat DefaultPixelFormat { get; }

        bool IsSupportedBitmapPixelFormat(PixelFormat format);
    }

    [Unstable, PrivateApi]
    public interface IPlatformRenderInterfaceContext : IOptionalFeatureProvider, IDisposable
    {
        /// <summary>
        /// Creates a renderer.
        /// </summary>
        /// <param name="surfaces">
        /// The list of native platform surfaces that can be used for output.
        /// </param>
        /// <returns>An <see cref="IRenderTarget"/>.</returns>
        IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces);
        
        /// <summary>
        /// Indicates that the context is no longer usable. This method should be thread-safe
        /// </summary>
        bool IsLost { get; }
        
        /// <summary>
        /// Exposes features that should be available for consumption while context isn't active (e. g. from the UI thread)
        /// </summary>
        IReadOnlyDictionary<Type, object> PublicFeatures { get; }
    }
}
