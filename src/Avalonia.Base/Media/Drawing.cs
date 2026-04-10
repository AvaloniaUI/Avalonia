using System;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Abstract class that describes a 2-D drawing.
    /// </summary>
    public abstract class Drawing : AvaloniaObject, IAffectsRender
    {
        internal static readonly WeakEvent<IAffectsRender, EventArgs> InvalidatedWeakEvent =
            WeakEvent.Register<IAffectsRender>(
                (s, h) => s.Invalidated += h,
                (s, h) => s.Invalidated -= h);

        private EventHandler? _invalidated;
        private TargetWeakEventSubscriber<Drawing, EventArgs>? _affectsRenderWeakSubscriber;

        event EventHandler? IAffectsRender.Invalidated
        {
            add => _invalidated += value;
            remove => _invalidated -= value;
        }

        internal Drawing()
        {
        }

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

        /// <summary>
        /// Marks properties as affecting the drawing's visual representation.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a drawing's static constructor, any change to the
        /// property will cause the <see cref="IAffectsRender.Invalidated"/> event to be raised.
        /// For property values implementing <see cref="IAffectsRender"/>, their invalidations
        /// are propagated through the drawing's own event using weak subscriptions.
        /// </remarks>
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : Drawing
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => (e.Sender as T)?.RaiseInvalidated());

            var invalidateAndSubscribeObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e =>
                {
                    if (e.Sender is not T sender)
                        return;

                    if (e.OldValue is IAffectsRender oldValue && sender._affectsRenderWeakSubscriber is not null)
                        InvalidatedWeakEvent.Unsubscribe(oldValue, sender._affectsRenderWeakSubscriber);

                    if (e.NewValue is IAffectsRender newValue)
                    {
                        sender._affectsRenderWeakSubscriber ??= new TargetWeakEventSubscriber<Drawing, EventArgs>(
                            sender, static (target, _, _, _) => target.RaiseInvalidated());
                        InvalidatedWeakEvent.Subscribe(newValue, sender._affectsRenderWeakSubscriber);
                    }

                    sender.RaiseInvalidated();
                });

            foreach (var property in properties)
            {
                if (property.CanValueAffectRender())
                    property.Changed.Subscribe(invalidateAndSubscribeObserver);
                else
                    property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Raises the <see cref="IAffectsRender.Invalidated"/> event.
        /// </summary>
        protected void RaiseInvalidated() => _invalidated?.Invoke(this, EventArgs.Empty);
    }
}
