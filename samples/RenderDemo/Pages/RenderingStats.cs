using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace RenderDemo.Pages
{
    internal class RenderingStats
    {
        private Queue<double> _frameTimes = new Queue<double>();
        private Queue<double> _frameDurations = new Queue<double>();
        private Stopwatch _start = Stopwatch.StartNew();
        private int StatsFrameCount = 60;
        private int frames = 0;
        private double last;

        public RenderingStats()
        {
            _frameTimes.Enqueue(0);
            _frameDurations.Enqueue(1);
        }

        public IDisposable Frame()
        {
            var w = System.Diagnostics.Stopwatch.StartNew();
            last = _start.ElapsedMilliseconds;
            _frameTimes.Enqueue(last);
            frames++;

            return System.Reactive.Disposables.Disposable.Create(() =>
            {
                w.Stop();
                _frameDurations.Enqueue(w.ElapsedMilliseconds);
                if (_frameDurations.Count > StatsFrameCount)
                {
                    _frameTimes.Dequeue();
                    _frameDurations.Dequeue();
                }
            });
        }

        private string f(double d)
        {
            return d.ToString("0.00");
        }

        public override string ToString()
        {
            var dur = (last - _frameTimes.Peek()) / _frameTimes.Count;
            return $"total:{frames} stats for last {_frameTimes.Count} frames, rendered fps: {f(1000.0 / dur)}, time for render: {f(_frameDurations.Average())} ms";
        }

        public void Render(DrawingContext context, double x = 10, double y = 10)
        {
            Render(context.PlatformImpl, x, y);
        }

        public void Render(Avalonia.Platform.IDrawingContextImpl context, double x = 10, double y = 10)
        {
            var t = new FormattedText() { Text = ToString(), FontSize = 12, Typeface = Typeface.Default };
            context.DrawRectangle(Brushes.Black, null, t.Bounds.Translate(new Vector(x, y)));
            context.DrawText(Brushes.White, new Point(x, y), t.PlatformImpl);
        }
    }
}
