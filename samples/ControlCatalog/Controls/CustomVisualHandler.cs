using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Math = System.Math;

namespace ControlCatalog.Controls
{
    internal class CustomVisualHandler : CompositionCustomVisualHandler
    {
        private TimeSpan _animationElapsed;
        private TimeSpan? _lastServerTime;
        private bool _running;
        private bool _preciseDirtyRects;

        public static readonly object StopMessage = new(),
            StartMessage = new(),
            UsePreciseDirtyRects = new(),
            UseNonPreciseDirtyRects = new();

        private List<(Point center, double size, ImmutableSolidColorBrush brush)> _ellipses = new();

        void UpdateRects()
        {
            if (_running)
            {
                if (_lastServerTime.HasValue)
                    _animationElapsed += (CompositionNow - _lastServerTime.Value);
                _lastServerTime = CompositionNow;
            }

            _ellipses.Clear();

            const int cnt = 20;
            var maxPointSizeX = EffectiveSize.X / (cnt * 1.6);
            var maxPointSizeY = EffectiveSize.Y / 4;
            var pointSize = Math.Min(maxPointSizeX, maxPointSizeY);
            var animationLength = TimeSpan.FromSeconds(4);
            var animationStage = _animationElapsed.TotalSeconds / animationLength.TotalSeconds;

            var sinOffset = Math.Cos(_animationElapsed.TotalSeconds) * 1.5;

            for (var c = 0; c < cnt; c++)
            {
                var stage = (animationStage + (double)c / cnt) % 1;
                var colorStage =
                    (animationStage + (Math.Sin(_animationElapsed.TotalSeconds * 2) + 1) / 2 + (double)c / cnt) % 1;
                var posX = (EffectiveSize.X + pointSize * 3) * stage - pointSize;
                var posY = (EffectiveSize.Y - pointSize) * (1 + Math.Sin(stage * 3.14 * 3 + sinOffset)) / 2 + pointSize / 2;
                var opacity = Math.Sin(stage * 3.14);

                _ellipses.Add((new Point(posX, posY), pointSize / 2, new ImmutableSolidColorBrush(Color.FromArgb(
                    255,
                    (byte)(255 - 255 * colorStage),
                    (byte)(255 * Math.Abs(0.5 - colorStage) * 2),
                    (byte)(255 * colorStage)
                ), opacity)));
            }
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            if (_ellipses.Count == 0)
                UpdateRects();

            foreach (var e in _ellipses)
                drawingContext.DrawEllipse(e.brush, null, e.center, e.size, e.size);
        }

        public override void OnMessage(object message)
        {
            if (message == StartMessage)
            {
                _running = true;
                _lastServerTime = null;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == StopMessage)
                _running = false;
            else if (message == UsePreciseDirtyRects)
                _preciseDirtyRects = true;
            else if (message == UseNonPreciseDirtyRects)
                _preciseDirtyRects = false;
        }

        void InvalidateCurrentEllipseRects()
        {
            foreach (var e in _ellipses)
                Invalidate(new Rect(e.center.X - e.size, e.center.Y - e.size, e.size * 2, e.size * 2));
        }

        public override void OnAnimationFrameUpdate()
        {
            if (_running)
            {
                if (_preciseDirtyRects)
                    InvalidateCurrentEllipseRects();
                else
                    Invalidate();
                UpdateRects();
                if (_preciseDirtyRects)
                    InvalidateCurrentEllipseRects();
                RegisterForNextAnimationFrameUpdate();
            }
        }
    }
}
