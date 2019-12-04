﻿using System;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    public class RendererBase
    {
        private static readonly Typeface s_fpsTypeface = new Typeface("Arial");
        private static int s_fontSize = 18;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _framesThisSecond;
        private int _fps;
        private FormattedText _fpsText;
        private TimeSpan _lastFpsUpdate;

        public RendererBase()
        {
            _fpsText = new FormattedText
            {
                Typeface = s_fpsTypeface,
                FontSize = s_fontSize
            };
        }

        protected void RenderFps(IDrawingContextImpl context, Rect clientRect, int? layerCount)
        {
            var now = _stopwatch.Elapsed;
            var elapsed = now - _lastFpsUpdate;

            ++_framesThisSecond;

            if (elapsed.TotalSeconds > 1)
            {
                _fps = (int)(_framesThisSecond / elapsed.TotalSeconds);
                _framesThisSecond = 0;
                _lastFpsUpdate = now;
            }

            if (layerCount.HasValue)
            {
                _fpsText.Text = string.Format("Layers: {0} FPS: {1:000}", layerCount, _fps);
            }
            else
            {
                _fpsText.Text = string.Format("FPS: {0:000}", _fps);
            }

            var size = _fpsText.Bounds.Size;
            var rect = new Rect(clientRect.Right - size.Width, 0, size.Width, size.Height);

            context.Transform = Matrix.Identity;
            context.FillRectangle(Brushes.Black, rect);
            context.DrawText(Brushes.White, rect.TopLeft, _fpsText.PlatformImpl);
        }
    }
}
