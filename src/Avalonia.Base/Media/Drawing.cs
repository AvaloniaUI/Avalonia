using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Abstract class that describes a 2-D drawing.
    /// </summary>
    public abstract class Drawing : AvaloniaObject
    {
        internal Drawing()
        {
        }

        /// <summary>
        /// Raised when the drawing changes in a way that requires its consumer to re-record it: a referenced resource
        /// is replaced (e.g. a new brush, pen, geometry or child), or a value that is baked into the recorded drawing
        /// commands changes (e.g. a transform, opacity or effect). Mutations _within_ a referenced compositor-aware
        /// resource (a brush's color, a geometry's shape) are not signaled here: the compositor propagates those on its own.
        /// </summary>
        internal event EventHandler? Invalidated;

        /// <summary>
        /// Draws this drawing to the given <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public void Draw(DrawingContext context) => DrawCore(context);

        internal abstract void DrawCore(DrawingContext context);

        /// <summary>
        /// Gets the drawing's bounding rectangle.
        /// </summary>
        public abstract Rect GetBounds();

        private protected void RaiseInvalidated()
            => Invalidated?.Invoke(this, EventArgs.Empty);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // Every property of a drawing is baked into the recorded drawing commands or selects which resource is used,
            // so any change requires a re-record. Keep subscriptions to nested values whose _own_ changes are also baked
            // (and thus can't be propagated by the compositor) in sync.
            UpdateValueSubscription(change.OldValue, subscribe: false);
            UpdateValueSubscription(change.NewValue, subscribe: true);

            RaiseInvalidated();
        }

        private void UpdateValueSubscription(object? value, bool subscribe)
        {
            switch (value)
            {
                // A transform's matrix is baked into the recorded commands, so its value changes (not just its
                // replacement) require a re-record.
                case Transform transform:
                    if (subscribe)
                        transform.Changed += ValueInvalidated;
                    else
                        transform.Changed -= ValueInvalidated;
                    break;

                // Effects are baked as immutable, and nested image sources (e.g. a DrawingImage) are inlined,
                // so their visual changes require a re-record too.
                case IAffectsRender affectsRender:
                    if (subscribe)
                        affectsRender.Invalidated += ValueInvalidated;
                    else
                        affectsRender.Invalidated -= ValueInvalidated;
                    break;
            }

            void ValueInvalidated(object? sender, EventArgs e)
                => RaiseInvalidated();
        }
    }
}
