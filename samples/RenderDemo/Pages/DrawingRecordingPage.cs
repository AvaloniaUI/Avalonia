using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace RenderDemo.Pages
{
    public class DrawingRecordingPage : Control
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private DrawingRecording? _recording;
        private Compositor? _compositor;
        private SolidColorBrush? _animatedBrush;

        public DrawingRecordingPage()
        {
            ClipToBounds = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            Dispatcher.UIThread.InvokeAsync(AnimationTick, DispatcherPriority.Background);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _recording?.Dispose();
            _recording = null;
            _compositor = null;
        }

        private void AnimationTick()
        {
            if (_compositor == null)
                return;

            var t = _stopwatch.Elapsed.TotalSeconds;

            // Animate the brush color through the spectrum
            var r = (byte)(Math.Sin(t * 0.7) * 127 + 128);
            var g = (byte)(Math.Sin(t * 0.7 + 2.094) * 127 + 128);
            var b = (byte)(Math.Sin(t * 0.7 + 4.189) * 127 + 128);

            if (_animatedBrush != null)
            {
                // Update existing brush — change propagates through compositor automatically
                _animatedBrush.Color = Color.FromRgb(r, g, b);
            }

            InvalidateVisual();
            Dispatcher.UIThread.InvokeAsync(AnimationTick, DispatcherPriority.Background);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_compositor == null)
                return;

            var w = Bounds.Width;
            var h = Bounds.Height;
            if (w <= 0 || h <= 0)
                return;

            // Draw label
            var labelY = 10.0;

            // === Left side: Immutable recording (recreated each frame with new colors) ===
            var t = _stopwatch.Elapsed.TotalSeconds;
            var immutableRecording = DrawingRecording.Create(ctx =>
            {
                var size = Math.Min(w / 2 - 40, h - 80) / 4;
                var cx = w / 4;
                var cy = h / 2;

                for (int i = 0; i < 6; i++)
                {
                    var angle = t * 0.5 + i * Math.PI / 3;
                    var x = cx + Math.Cos(angle) * size * 1.5 - size / 2;
                    var y = cy + Math.Sin(angle) * size * 1.5 - size / 2;

                    var cr = (byte)(Math.Sin(t + i * 1.0) * 127 + 128);
                    var cg = (byte)(Math.Sin(t + i * 1.0 + 2.094) * 127 + 128);
                    var cb = (byte)(Math.Sin(t + i * 1.0 + 4.189) * 127 + 128);

                    ctx.DrawRectangle(
                        new ImmutableSolidColorBrush(Color.FromArgb(180, cr, cg, cb)),
                        null,
                        new Rect(x, y, size, size));
                }
            });

            using (immutableRecording)
            {
                context.DrawRecording(immutableRecording);
            }

            // === Right side: Compositor-bound recording (brush animates via compositor) ===
            if (_recording == null)
            {
                _animatedBrush = new SolidColorBrush(Colors.Red);

                _recording = DrawingRecording.Create(_compositor, ctx =>
                {
                    var size = Math.Min(w / 2 - 40, h - 80) / 4;
                    var cx = w * 3 / 4;
                    var cy = h / 2;

                    // Central circle with animated brush
                    ctx.DrawEllipse(_animatedBrush, null, new Rect(cx - size, cy - size, size * 2, size * 2));

                    // Static surrounding shapes
                    for (int i = 0; i < 8; i++)
                    {
                        var angle = i * Math.PI / 4;
                        var x = cx + Math.Cos(angle) * size * 2 - size / 4;
                        var y = cy + Math.Sin(angle) * size * 2 - size / 4;

                        ctx.DrawRectangle(
                            new ImmutableSolidColorBrush(Color.FromArgb(120, 100, 100, 100)),
                            new ImmutablePen(Brushes.White, 1),
                            new Rect(x, y, size / 2, size / 2));
                    }
                });
            }

            context.DrawRecording(_recording);

            // Draw labels
            var immutableLabel = "Immutable (recreated each frame)";
            var compositorLabel = "Compositor-bound (brush animates)";

            var ft1 = new FormattedText(immutableLabel, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, Typeface.Default, 14, Brushes.White);
            var ft2 = new FormattedText(compositorLabel, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, Typeface.Default, 14, Brushes.White);

            context.DrawText(ft1, new Point(w / 4 - ft1.Width / 2, labelY));
            context.DrawText(ft2, new Point(w * 3 / 4 - ft2.Width / 2, labelY));
        }
    }
}
