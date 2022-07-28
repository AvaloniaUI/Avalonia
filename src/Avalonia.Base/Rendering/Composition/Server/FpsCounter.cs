using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Utilities;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// An FPS counter helper that can draw itself on the render thread
/// </summary>
internal class FpsCounter
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private int _framesThisSecond;
    private int _totalFrames;
    private int _fps;
    private TimeSpan _lastFpsUpdate;
    const int FirstChar = 32;
    const int LastChar = 126;
    // ASCII chars
    private GlyphRun[] _runs = new GlyphRun[LastChar - FirstChar + 1];
    
    public FpsCounter(GlyphTypeface typeface)
    {
        for (var c = FirstChar; c <= LastChar; c++)
        {
            var s = new string((char)c, 1);
            var glyph = typeface.GetGlyph((uint)(s[0]));
            _runs[c - FirstChar] = new GlyphRun(typeface, 18, new ReadOnlySlice<char>(s.AsMemory()), new ushort[] { glyph });
        }
    }

    public void FpsTick() => _framesThisSecond++;

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

        var fpsLine = $"Frame #{_totalFrames:00000000} FPS: {_fps:000} " + aux;
        double width = 0;
        double height = 0;
        foreach (var ch in fpsLine)
        {
            var run = _runs[ch - FirstChar];
            width +=  run.Size.Width;
            height = Math.Max(height, run.Size.Height);
        }

        var rect = new Rect(0, 0, width + 3, height + 3);

        context.DrawRectangle(Brushes.Black, null, rect);

        double offset = 0;
        foreach (var ch in fpsLine)
        {
            var run = _runs[ch - FirstChar];
            context.Transform = Matrix.CreateTranslation(offset, 0);
            context.DrawGlyphRun(Brushes.White, run);
            offset += run.Size.Width;
        }
    }
}
