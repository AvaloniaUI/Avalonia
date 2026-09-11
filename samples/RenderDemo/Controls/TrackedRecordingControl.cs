using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;

namespace RenderDemo.Controls
{
    /// <summary>
    /// One compositor-bound <see cref="DrawingRecording"/>, recorded once. The
    /// fill brush and the ring pen are mutable and captured by reference, so
    /// changing them per frame repaints the recording through change tracking:
    /// the recording is not rebuilt and this control's Render method is not
    /// called again.
    /// </summary>
    public sealed class TrackedRecordingControl : RecordingDemoControl
    {
        private readonly SolidColorBrush _fill = new(Colors.Red);
        private readonly Pen _ring = new(new ImmutableSolidColorBrush(Color.FromRgb(30, 41, 59)), 2);
        private DrawingRecording? _recording;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            if (compositor is null)
                return;

            // Compositor-bound: mutable resources are registered with the compositor
            // instead of being snapshotted, so later changes are tracked.
            _recording = DrawingRecording.Create(compositor, ctx =>
            {
                ctx.DrawEllipse(_fill, _ring, new Point(0, 0), 60, 60);

                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var x = Math.Cos(angle) * 110;
                    var y = Math.Sin(angle) * 110;
                    var t = (byte)(i * 30);

                    ctx.DrawRectangle(
                        new ImmutableSolidColorBrush(Color.FromRgb(t, (byte)(255 - t), 200)),
                        new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(30, 41, 59)), 1),
                        new Rect(x - 16, y - 16, 32, 32));
                }
            });
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _recording?.Dispose();
            _recording = null;
        }

        protected override void OnFrame(double seconds)
        {
            var r = (byte)(Math.Sin(seconds * 0.7) * 127 + 128);
            var g = (byte)(Math.Sin(seconds * 0.7 + 2.094) * 127 + 128);
            var b = (byte)(Math.Sin(seconds * 0.7 + 4.189) * 127 + 128);
            _fill.Color = Color.FromRgb(r, g, b);
            _ring.Thickness = 2 + 6 * (Math.Sin(seconds * 1.5) + 1) / 2;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_recording is null)
                return;

            context.DrawRecording(_recording, Matrix.CreateTranslation(Bounds.Width / 2, Bounds.Height / 2));
        }
    }
}
