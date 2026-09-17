using SkiaSharp;

namespace Avalonia.Skia;

/// <summary>
/// A Skia geometry defined by stroke and fill paths.
/// </summary>
internal sealed class SimpleGeometryImpl(SKPath strokePath, SKPath? fillPath, Rect? bounds = null)
    : GeometryImpl
{
    /// <inheritdoc />
    public override SKPath StrokePath { get; } = strokePath;

    /// <inheritdoc />
    public override SKPath? FillPath { get; } = fillPath;

    /// <inheritdoc />
    public override Rect Bounds { get; } = bounds ?? strokePath.TightBounds.ToAvaloniaRect();
}
