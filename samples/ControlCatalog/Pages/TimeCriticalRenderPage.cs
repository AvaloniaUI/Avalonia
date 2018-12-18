using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages
{
    public class TimeCriticalRenderPage : Control, IRenderTimeCriticalVisual
    {
        private int _frame;
        private TimeSpan _lastFps;
        private int _lastFpsFrame;
        private double _fps;
        public bool HasNewFrame => true;
        Stopwatch _st = Stopwatch.StartNew();

        private Typeface _typeface = Typeface.Default;

        public void ThreadSafeRender(DrawingContext context, Size logicalSize, double scaling)
        {
            var nowTs = _st.Elapsed;
            var now = DateTime.Now;
            var fpsTimeDiff = (nowTs - _lastFps).TotalSeconds;
            if (fpsTimeDiff > 1)
            {
                _fps = (_frame - _lastFpsFrame) / fpsTimeDiff;
                _lastFpsFrame = _frame;
                _lastFps = nowTs;
            }

            var text = $"Frame: {_frame}\nFPS: {_fps}\nNow: {now}";
            text += $"\nTransform{context.CurrentTransform}";
            text += $"\nContainer Transform{context.CurrentContainerTransform}";
            var fmt = new FormattedText()
            {
                Text = text,
                Typeface = _typeface
            };
            var back = new ImmutableSolidColorBrush(Colors.LightGray);
            var textBrush = new ImmutableSolidColorBrush(Colors.Black);
            context.FillRectangle(back, new Rect(logicalSize));
            context.DrawText(textBrush, new Point(5, 5), fmt);
            _frame++;
        }
    }
}
