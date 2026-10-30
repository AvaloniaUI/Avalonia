using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;

namespace RenderDemo.Controls
{
    /// <summary>
    /// One immutable <see cref="DrawingRecording"/>, replayed at many transforms
    /// every frame through DrawRecording(recording, matrix) - the shape of an icon
    /// set, an SVG use site or a color glyph. The glyph's draw calls run exactly
    /// once, at record time; each instance on screen is a single DrawRecording
    /// node referencing the same stream.
    /// </summary>
    public sealed class RecordingReplayControl : RecordingDemoControl
    {
        private const int Instances = 16;
        private const double GlyphRadius = 24;

        private DrawingRecording? _glyph;
        private double _time;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // Immutable: no compositor involved, brushes and pens are snapshotted at record time.
            _glyph = DrawingRecording.Create(RecordGlyph);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _glyph?.Dispose();
            _glyph = null;
        }

        protected override void OnFrame(double seconds)
        {
            _time = seconds;
            InvalidateVisual();
        }

        // A small emblem authored around the origin: a disc, a ring and four spokes.
        private static void RecordGlyph(DrawingContext ctx)
        {
            var r = GlyphRadius;
            var inner = r * 0.6;

            ctx.DrawEllipse(new ImmutableSolidColorBrush(Color.FromArgb(220, 250, 204, 21)), null,
                new Point(0, 0), r, r);
            ctx.DrawEllipse(null, new ImmutablePen(Brushes.White, 2), new Point(0, 0), inner, inner);

            var spoke = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(30, 41, 59)), 3);
            for (var i = 0; i < 4; i++)
            {
                var a = i * Math.PI / 4;
                var dx = Math.Cos(a) * inner;
                var dy = Math.Sin(a) * inner;
                ctx.DrawLine(spoke, new Point(dx, dy), new Point(-dx, -dy));
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_glyph is null)
                return;

            var cx = Bounds.Width / 2;
            var cy = Bounds.Height / 2;
            var orbit = Math.Max(0, Math.Min(cx, cy) - GlyphRadius * 2);

            for (var i = 0; i < Instances; i++)
            {
                var phase = _time * 0.4 + i * 2 * Math.PI / Instances;
                var scale = 0.7 + 0.3 * Math.Sin(_time * 2 + i);
                var transform = Matrix.CreateScale(scale, scale)
                    * Matrix.CreateRotation(phase * 3)
                    * Matrix.CreateTranslation(cx + Math.Cos(phase) * orbit, cy + Math.Sin(phase) * orbit);

                context.DrawRecording(_glyph, transform);
            }
        }
    }
}
