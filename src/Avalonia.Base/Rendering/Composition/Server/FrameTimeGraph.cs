using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Represents a simple time graph for diagnostics purpose, used to show layout and render times.
/// </summary>
internal sealed class FrameTimeGraph
{
    private const double HeaderPadding = 2.0;

    private readonly IPlatformRenderInterface _renderInterface;
    private readonly ImmutableSolidColorBrush _borderBrush;
    private readonly ImmutablePen _graphPen;
    private readonly double[] _frameValues;
    private readonly Size _size;
    private readonly Size _headerSize;
    private readonly Size _graphSize;
    private readonly double _defaultMaxY;
    private readonly string _title;
    private readonly DiagnosticTextRenderer _textRenderer;

    private int _startFrameIndex;
    private int _frameCount;

    public Size Size
        => _size;

    public FrameTimeGraph(int maxFrames, Size size, double defaultMaxY, string title,
        DiagnosticTextRenderer textRenderer)
    {
        Debug.Assert(maxFrames >= 1);
        Debug.Assert(size.Width > 0.0);
        Debug.Assert(size.Height > 0.0);

        _renderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        _borderBrush = new ImmutableSolidColorBrush(0x80808080);
        _graphPen = new ImmutablePen(Brushes.Blue);
        _frameValues = new double[maxFrames];
        _size = size;
        _headerSize = new Size(size.Width, textRenderer.GetMaxHeight() + HeaderPadding * 2.0);
        _graphSize = new Size(size.Width, size.Height - _headerSize.Height);
        _defaultMaxY = defaultMaxY;
        _title = title;
        _textRenderer = textRenderer;
    }

    public void AddFrameValue(double value)
    {
        if (_frameCount < _frameValues.Length)
        {
            _frameValues[_startFrameIndex + _frameCount] = value;
            ++_frameCount;
        }
        else
        {
            // overwrite oldest value
            _frameValues[_startFrameIndex] = value;
            if (++_startFrameIndex == _frameValues.Length)
            {
                _startFrameIndex = 0;
            }
        }
    }

    public void Reset()
    {
        _startFrameIndex = 0;
        _frameCount = 0;
    }

    public void Render(IDrawingContextImpl context)
    {
        var originalTransform = context.Transform;
        context.PushClip(new Rect(_size));

        context.DrawRectangle(_borderBrush, null, new RoundedRect(new Rect(_size)));
        context.DrawRectangle(_borderBrush, null, new RoundedRect(new Rect(_headerSize)));

        context.Transform = originalTransform * Matrix.CreateTranslation(HeaderPadding, HeaderPadding);
        _textRenderer.DrawAsciiText(context, _title.AsSpan(), Brushes.Black);

        if (_frameCount > 0)
        {
            var (min, avg, max) = GetYValues();

            DrawLabelledValue(context, "Min", min, originalTransform, _headerSize.Width * 0.19);
            DrawLabelledValue(context, "Avg", avg, originalTransform, _headerSize.Width * 0.46);
            DrawLabelledValue(context, "Max", max, originalTransform, _headerSize.Width * 0.73);

            context.Transform = originalTransform * Matrix.CreateTranslation(0.0, _headerSize.Height);
            context.DrawGeometry(null, _graphPen, BuildGraphGeometry(Math.Max(max, _defaultMaxY)));
        }

        context.Transform = originalTransform;
        context.PopClip();
    }

    private void DrawLabelledValue(IDrawingContextImpl context, string label, double value, in Matrix originalTransform,
        double left)
    {
        context.Transform = originalTransform * Matrix.CreateTranslation(left + HeaderPadding, HeaderPadding);

        var brush = value <= _defaultMaxY ? Brushes.Black : Brushes.Red;

#if NET6_0_OR_GREATER
        Span<char> buffer = stackalloc char[24];
        buffer.TryWrite(CultureInfo.InvariantCulture, $"{label}: {value,5:F2}ms", out var charsWritten);
        _textRenderer.DrawAsciiText(context, buffer.Slice(0, charsWritten), brush);
#else
        var text = FormattableString.Invariant($"{label}: {value,5:F2}ms");
        _textRenderer.DrawAsciiText(context, text.AsSpan(), brush);
#endif
    }

    private IStreamGeometryImpl BuildGraphGeometry(double maxY)
    {
        Debug.Assert(_frameCount > 0);

        var graphGeometry = _renderInterface.CreateStreamGeometry();
        using var geometryContext = graphGeometry.Open();

        var xRatio = _graphSize.Width / _frameValues.Length;
        var yRatio = _graphSize.Height / maxY;

        geometryContext.BeginFigure(new Point(0.0, _graphSize.Height - GetFrameValue(0) * yRatio), false);

        for (var i = 1; i < _frameCount; ++i)
        {
            var x = Math.Round(i * xRatio);
            var y = _graphSize.Height - GetFrameValue(i) * yRatio;
            geometryContext.LineTo(new Point(x, y));
        }

        geometryContext.EndFigure(false);
        return graphGeometry;
    }

    private (double Min, double Average, double Max) GetYValues()
    {
        Debug.Assert(_frameCount > 0);

        var min = double.MaxValue;
        var max = double.MinValue;
        var total = 0.0;

        for (var i = 0; i < _frameCount; ++i)
        {
            var y = GetFrameValue(i);

            total += y;

            if (y < min)
            {
                min = y;
            }

            if (y > max)
            {
                max = y;
            }
        }

        return (min, total / _frameCount, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetFrameValue(int frameOffset)
        => _frameValues[(_startFrameIndex + frameOffset) % _frameValues.Length];
}
