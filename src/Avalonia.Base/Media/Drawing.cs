using System;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Abstract class that describes a 2-D drawing.
    /// </summary>
    public abstract class Drawing : AvaloniaObject
    {
        private static readonly WeakEvent<IAffectsRender, EventArgs> s_renderResourceInvalidatedWeakEvent =
            WeakEvent.Register<IAffectsRender>(
                (source, handler) => source.Invalidated += handler,
                (source, handler) => source.Invalidated -= handler);

        internal static readonly WeakEvent<Drawing, EventArgs> InvalidatedWeakEvent =
            WeakEvent.Register<Drawing>(
                (source, handler) => source.Invalidated += handler,
                (source, handler) => source.Invalidated -= handler);

        private EventHandler? _invalidated;
        private TargetWeakEventSubscriber<Drawing, EventArgs>? _affectsRenderWeakSubscriber;

        internal Drawing()
        {
        }

        internal event EventHandler? Invalidated
        {
            add => _invalidated += value;
            remove => _invalidated -= value;
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
        /// Registers properties that mutate this drawing's rendered content.
        /// </summary>
        /// <remarks>
        /// This is an internal invalidation-graph hook for the drawing tree, not a public
        /// rendering-resource contract. Values that implement <see cref="IAffectsRender"/>
        /// are observed weakly so nested media resources can invalidate the owning drawing
        /// without pinning shared instances.
        /// </remarks>
        protected static void AffectsDrawingContent<T>(params AvaloniaProperty[] properties)
            where T : Drawing
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => (e.Sender as T)?.RaiseInvalidated());

            var invalidateAndSubscribeObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e =>
                {
                    if (e.Sender is not T sender)
                    {
                        return;
                    }

                    if (e.OldValue is IAffectsRender oldValue && sender._affectsRenderWeakSubscriber is not null)
                    {
                        s_renderResourceInvalidatedWeakEvent.Unsubscribe(oldValue, sender._affectsRenderWeakSubscriber);
                    }

                    if (e.NewValue is IAffectsRender newValue)
                    {
                        sender._affectsRenderWeakSubscriber ??=
                            new TargetWeakEventSubscriber<Drawing, EventArgs>(
                                sender,
                                static (target, _, _, _) => target.RaiseInvalidated());
                        s_renderResourceInvalidatedWeakEvent.Subscribe(newValue, sender._affectsRenderWeakSubscriber);
                    }

                    sender.RaiseInvalidated();
                });

            foreach (var property in properties)
            {
                if (property.CanValueAffectRender())
                {
                    property.Changed.Subscribe(invalidateAndSubscribeObserver);
                }
                else
                {
                    property.Changed.Subscribe(invalidateObserver);
                }
            }
        }

        protected void RaiseInvalidated() => _invalidated?.Invoke(this, EventArgs.Empty);
    }
}
