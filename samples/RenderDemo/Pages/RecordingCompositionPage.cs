using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace RenderDemo.Pages
{
    /// <summary>
    /// Hosts a compositor-bound <see cref="DrawingRecording"/> as a
    /// <see cref="CompositionRecordingVisual"/> child visual that spins entirely on
    /// the render thread. The drawing is recorded once and the rotation is sent to
    /// the compositor once; after that the UI thread does no per-frame work — the
    /// server evaluates the animation every frame. The centre disc is filled with a
    /// composition gradient whose stop colors animate on the render thread too:
    /// the recording repaints because its render data observes the brush, without
    /// any re-recording.
    ///
    /// Everything here is wired by hand from the public primitives so the moving
    /// parts are visible. Contrast the neighbouring DrawingRecording page, which
    /// replays a recording through DrawingContext.DrawRecording every frame.
    /// </summary>
    public class RecordingCompositionPage : Control
    {
        // The drawing is authored around this point; the visual rotates about it.
        private const double Center = 150;

        private DrawingRecording? _recording;
        private CompositionRecordingVisual? _visual;
        private CompositionLinearGradientBrush? _gradient;
        private CompositionGradientStop? _stopA;
        private CompositionGradientStop? _stopB;

        public RecordingCompositionPage()
        {
            ClipToBounds = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            if (compositor is null || _visual?.Compositor == compositor)
                return;

            // A composition brush is captured by reference and read live at
            // replay time, so its animated values repaint the recording with no
            // UI-thread work and no re-recording - the recording's server render
            // data observes the brush. The stop colors cross-fade on the render
            // thread while the ring spins. Composition objects have no public
            // dispose, so the brush and its stops are created once per
            // compositor and reused across attach cycles.
            if (_gradient is null || _gradient.Compositor != compositor)
            {
                _stopA = compositor.CreateGradientStop(0, Color.FromRgb(244, 114, 182));
                _stopB = compositor.CreateGradientStop(1, Color.FromRgb(37, 99, 235));
                _gradient = compositor.CreateLinearGradientBrush();
                _gradient.StartPoint = RelativePoint.TopLeft;
                _gradient.EndPoint = RelativePoint.BottomRight;
                _gradient.GradientStops.Add(_stopA);
                _gradient.GradientStops.Add(_stopB);
            }

            var cycleA = compositor.CreateColorKeyFrameAnimation();
            cycleA.InsertKeyFrame(0f, Color.FromRgb(244, 114, 182));
            cycleA.InsertKeyFrame(0.5f, Color.FromRgb(37, 99, 235));
            cycleA.InsertKeyFrame(1f, Color.FromRgb(244, 114, 182));
            cycleA.Duration = TimeSpan.FromSeconds(6);
            cycleA.IterationBehavior = AnimationIterationBehavior.Forever;
            _stopA!.StartAnimation("Color", cycleA);

            var cycleB = compositor.CreateColorKeyFrameAnimation();
            cycleB.InsertKeyFrame(0f, Color.FromRgb(37, 99, 235));
            cycleB.InsertKeyFrame(0.5f, Color.FromRgb(244, 114, 182));
            cycleB.InsertKeyFrame(1f, Color.FromRgb(37, 99, 235));
            cycleB.Duration = TimeSpan.FromSeconds(6);
            cycleB.IterationBehavior = AnimationIterationBehavior.Forever;
            _stopB!.StartAnimation("Color", cycleB);

            // Record the drawing once. Compositor-bound so the recording can be
            // carried to the render thread by reference and hosted by a visual.
            _recording = DrawingRecording.Create(compositor, ctx =>
            {
                ctx.DrawEllipse(_gradient, null,
                    new Rect(Center - 60, Center - 60, 120, 120));

                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var x = Center + Math.Cos(angle) * 90;
                    var y = Center + Math.Sin(angle) * 90;
                    var t = (byte)(i * 30);

                    ctx.DrawRectangle(
                        new ImmutableSolidColorBrush(Color.FromRgb(t, (byte)(255 - t), 200)),
                        new ImmutablePen(Brushes.White, 2),
                        new Rect(x - 22, y - 22, 44, 44));
                }
            });

            _visual = compositor.CreateRecordingVisual();
            _visual.Recording = _recording;
            _visual.CenterPoint = new((float)Center, (float)Center, 0f);
            UpdateOffset();

            // Parent it under this control's own composition visual. The recording
            // renders behind any children (there are none here).
            ElementComposition.SetElementChildVisual(this, _visual);

            // One animation, sent once. RotationAngle is a render-thread-animated
            // property, so the spin runs on the compositor with no UI-thread ticks.
            var spin = compositor.CreateScalarKeyFrameAnimation();
            spin.InsertKeyFrame(0f, 0f);
            spin.InsertKeyFrame(1f, (float)(Math.PI * 2));
            spin.Duration = TimeSpan.FromSeconds(8);
            spin.IterationBehavior = AnimationIterationBehavior.Forever;
            _visual.StartAnimation("RotationAngle", spin);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            ElementComposition.SetElementChildVisual(this, null);
            _visual = null;
            _recording?.Dispose();
            _recording = null;
            // The brush outlives the recording (no public dispose); just stop
            // its animations while nothing displays it.
            _stopA?.StopAnimation("Color");
            _stopB?.StopAnimation("Color");
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty)
                UpdateOffset();
        }

        // Centre the authored drawing within the page. Offset is a static translate
        // that composes with the animated rotation.
        private void UpdateOffset()
        {
            if (_visual != null)
                _visual.Offset = new(
                    (float)(Bounds.Width / 2 - Center),
                    (float)(Bounds.Height / 2 - Center),
                    0f);
        }
    }
}
