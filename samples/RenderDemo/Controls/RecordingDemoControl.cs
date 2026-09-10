using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RenderDemo.Controls
{
    /// <summary>
    /// Base for the DrawingRecording demo surfaces: runs an animation-frame loop
    /// while attached and reports how often Render ran, so the page can show
    /// whether a change reached the screen by re-rendering the control or through
    /// compositor change tracking.
    /// </summary>
    public abstract class RecordingDemoControl : Control
    {
        /// <summary>
        /// Defines the <see cref="RenderCount"/> property.
        /// </summary>
        public static readonly DirectProperty<RecordingDemoControl, int> RenderCountProperty =
            AvaloniaProperty.RegisterDirect<RecordingDemoControl, int>(nameof(RenderCount), o => o.RenderCount);

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private TopLevel? _topLevel;
        private int _renderCount;
        private int _renders;

        /// <summary>
        /// Number of times <see cref="Render"/> has run, published once per animation frame.
        /// </summary>
        public int RenderCount
        {
            get => _renderCount;
            private set => SetAndRaise(RenderCountProperty, ref _renderCount, value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _topLevel = TopLevel.GetTopLevel(this);
            _topLevel?.RequestAnimationFrame(OnAnimationFrame);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            // Dropping the top level ends the loop: the pending callback sees null and does not re-request.
            _topLevel = null;
        }

        private void OnAnimationFrame(TimeSpan _)
        {
            if (_topLevel is null)
                return;

            OnFrame(_clock.Elapsed.TotalSeconds);
            RenderCount = _renders;
            _topLevel.RequestAnimationFrame(OnAnimationFrame);
        }

        /// <summary>
        /// Called once per animation frame with the time since the control was created.
        /// </summary>
        protected abstract void OnFrame(double seconds);

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            _renders++;
        }
    }
}
