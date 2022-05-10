using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class FpsCounter
{
    private readonly GlyphTypeface _typeface;
    private readonly bool _useManualFpsCounting;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private int _framesThisSecond;
    private int _fps;
    private TimeSpan _lastFpsUpdate;
    private GlyphRun[] _runs = new GlyphRun[10];
    
    public FpsCounter(GlyphTypeface typeface, bool useManualFpsCounting = false)
    {
        for (var c = 0; c <= 9; c++)
        {
            var s = c.ToString();
            var glyph = typeface.GetGlyph((uint)(s[0]));
            _runs[c] = new GlyphRun(typeface, 18, new ReadOnlySlice<char>(s.AsMemory()), new ushort[] { glyph });
        }
        _typeface = typeface;
        _useManualFpsCounting = useManualFpsCounting;
    }

    public void FpsTick() => _framesThisSecond++;

    public void RenderFps(IDrawingContextImpl context)
    {
        var now = _stopwatch.Elapsed;
        var elapsed = now - _lastFpsUpdate;

        if (!_useManualFpsCounting)
            ++_framesThisSecond;

        if (elapsed.TotalSeconds > 1)
        {
            _fps = (int)(_framesThisSecond / elapsed.TotalSeconds);
            _framesThisSecond = 0;
            _lastFpsUpdate = now;
        }

        var fpsLine = _fps.ToString("000");
        double width = 0;
        double height = 0;
        foreach (var ch in fpsLine)
        {
            var run = _runs[ch - '0'];
            width +=  run.Size.Width;
            height = Math.Max(height, run.Size.Height);
        }

        var rect = new Rect(0, 0, width + 3, height + 3);

        context.DrawRectangle(Brushes.Black, null, rect);

        double offset = 0;
        foreach (var ch in fpsLine)
        {
            var run = _runs[ch - '0'];
            context.Transform = Matrix.CreateTranslation(offset, 0);
            context.DrawGlyphRun(Brushes.White, run);
            offset += run.Size.Width;
        }
    }
}