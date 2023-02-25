using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Platform;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// An FPS counter helper that can draw itself on the render thread
/// </summary>
internal class FpsCounter
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly DiagnosticTextRenderer _textRenderer;

    private int _framesThisSecond;
    private int _totalFrames;
    private int _fps;
    private TimeSpan _lastFpsUpdate;

    public FpsCounter(DiagnosticTextRenderer textRenderer)
        => _textRenderer = textRenderer;

    public void FpsTick()
        => _framesThisSecond++;

    public void RenderFps(IDrawingContextImpl context, string aux)
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

#if NET6_0_OR_GREATER
        var fpsLine = string.Create(CultureInfo.InvariantCulture, $"Frame #{_totalFrames:00000000} FPS: {_fps:000} {aux}");
#else
        var fpsLine = FormattableString.Invariant($"Frame #{_totalFrames:00000000} FPS: {_fps:000} {aux}");
#endif

        var size = _textRenderer.MeasureAsciiText(fpsLine.AsSpan());
        var rect = new Rect(0.0, 0.0, size.Width + 3.0, size.Height + 3.0);

        context.DrawRectangle(Brushes.Black, null, rect);

        _textRenderer.DrawAsciiText(context, fpsLine.AsSpan(), Brushes.White);
    }

    public void Reset()
    {
        _framesThisSecond = 0;
        _totalFrames = 0;
        _fps = 0;
    }
}
