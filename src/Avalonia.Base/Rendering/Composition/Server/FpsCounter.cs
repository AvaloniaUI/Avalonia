using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Utilities;


namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// An FPS counter helper that can draw itself on the render thread.
/// The labels are shaped once and the changing numbers are drawn in a fixed digit grid,
/// so a frame neither formats nor measures any text.
/// </summary>
internal class FpsCounter
{
    private const int FrameDigits = 8;
    private const int FpsDigits = 3;
    private const int VisualDigits = 5;

    private static readonly ImmutableSolidColorBrush s_layerBrush = new(Colors.Black, 0.5);

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly DiagnosticTextRenderer _textRenderer;
    private readonly DiagnosticTextRenderer.ShapedText _frameLabel;
    private readonly DiagnosticTextRenderer.ShapedText _fpsLabel;
    private readonly DiagnosticTextRenderer.ShapedText _visitedLabel;
    private readonly DiagnosticTextRenderer.ShapedText _renderedLabel;
    private readonly double _digitsWidth;
    private readonly double _lineHeight;

    private DiagnosticTextRenderer.ShapedText _memoryText;
    private ulong _managedBytes;
    private ulong _nativeBytes;
    private double _textWidth;

    private int _framesThisSecond;
    private int _totalFrames;
    private int _fps;
    private TimeSpan _lastFpsUpdate;

    public FpsCounter(DiagnosticTextRenderer textRenderer)
    {
        _textRenderer = textRenderer;
        _frameLabel = textRenderer.Shape("Frame #");
        _fpsLabel = textRenderer.Shape(" FPS: ");
        _visitedLabel = textRenderer.Shape(" V:");
        _renderedLabel = textRenderer.Shape(" R:");
        _digitsWidth = (FrameDigits + FpsDigits + 2 * VisualDigits) * textRenderer.DigitCellWidth;
        _lineHeight = textRenderer.GetMaxHeight();
        _memoryText = CreateMemoryText();
        UpdateTextWidth();
    }

    public void FpsTick()
        => _framesThisSecond++;

    public Rect? RenderFps(ImmediateDrawingContext context, ulong managedBytes, ulong nativeBytes,
        int visitedVisuals, int renderedVisuals, bool hasLayer, Rect? oldRect)
    {
        var now = _stopwatch.Elapsed;
        var elapsed = now - _lastFpsUpdate;

        ++_framesThisSecond;
        ++_totalFrames;

        if (elapsed.TotalSeconds > 1)
        {
            _fps = (int)(_framesThisSecond / elapsed.TotalSeconds);
            _framesThisSecond = 0;
            _lastFpsUpdate = now;
        }

        if (managedBytes != _managedBytes || nativeBytes != _nativeBytes)
        {
            _managedBytes = managedBytes;
            _nativeBytes = nativeBytes;
            _memoryText.Dispose();
            _memoryText = CreateMemoryText();
            UpdateTextWidth();
        }

        var rect = new Rect(0.0, 0.0, _textWidth + 3.0, _lineHeight + 3.0);
        if (hasLayer && oldRect.HasValue)
            rect = rect.Union(oldRect.Value);

        context.DrawRectangle(hasLayer ? s_layerBrush : Brushes.Black, null, rect);

        var x = 0.0;
        DrawLabel(context, _frameLabel, ref x);
        DrawNumber(context, _totalFrames, FrameDigits, ref x);
        DrawLabel(context, _fpsLabel, ref x);
        DrawNumber(context, _fps, FpsDigits, ref x);
        DrawLabel(context, _memoryText, ref x);
        DrawLabel(context, _visitedLabel, ref x);
        DrawNumber(context, visitedVisuals, VisualDigits, ref x);
        DrawLabel(context, _renderedLabel, ref x);
        DrawNumber(context, renderedVisuals, VisualDigits, ref x);

        return rect;
    }

    private void DrawLabel(ImmediateDrawingContext context, DiagnosticTextRenderer.ShapedText text, ref double x)
    {
        using (context.PushPreTransform(Matrix.CreateTranslation(x, 0.0)))
            _textRenderer.DrawShapedText(context, text, Brushes.White);

        x += text.Width;
    }

    private void DrawNumber(ImmediateDrawingContext context, int value, int digitCount, ref double x)
    {
        using (context.PushPreTransform(Matrix.CreateTranslation(x, 0.0)))
            _textRenderer.DrawDigits(context, value, digitCount, Brushes.White);

        x += digitCount * _textRenderer.DigitCellWidth;
    }

    private DiagnosticTextRenderer.ShapedText CreateMemoryText()
        => _textRenderer.Shape(string.Create(CultureInfo.InvariantCulture,
            $" M:{ByteSizeHelper.ToString(_managedBytes, false)} / N:{ByteSizeHelper.ToString(_nativeBytes, false)}"));

    private void UpdateTextWidth()
        => _textWidth = _frameLabel.Width + _fpsLabel.Width + _visitedLabel.Width + _renderedLabel.Width
                        + _memoryText.Width + _digitsWidth;

    public void Reset()
    {
        _framesThisSecond = 0;
        _totalFrames = 0;
        _fps = 0;
    }
}
