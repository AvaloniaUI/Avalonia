using System;
using System.Diagnostics;
using Avalonia.Media;

namespace Avalonia.Rendering
{
    public class RendererBase
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _framesThisSecond;
        private int _fps;
        private TimeSpan _lastFpsUpdate;

        protected void RenderFps(IDrawingContextImpl context, Rect clientRect, bool incrementFrameCount)
        {
            var now = _stopwatch.Elapsed;
            var elapsed = now - _lastFpsUpdate;

            if (incrementFrameCount)
            {
                ++_framesThisSecond;
            }

            if (elapsed.TotalSeconds > 1)
            {
                _fps = (int)(_framesThisSecond / elapsed.TotalSeconds);
                _framesThisSecond = 0;
                _lastFpsUpdate = now;
            }

            var txt = new FormattedText(
                string.Format("FPS: {0:000}", _fps),
                "Arial", 18,
                Size.Infinity,
                FontStyle.Normal,
                TextAlignment.Left,
                FontWeight.Normal,
                TextWrapping.NoWrap);
            var size = txt.Measure();
            var rect = new Rect(clientRect.Right - size.Width, 0, size.Width, size.Height);

            context.Transform = Matrix.Identity;
            context.FillRectangle(Brushes.Black, rect);
            context.DrawText(Brushes.White, rect.TopLeft, txt.PlatformImpl);
        }
    }
}
