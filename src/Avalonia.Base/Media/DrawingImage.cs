using System;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// An <see cref="IImage"/> that uses a <see cref="Drawing"/> for content.
    /// </summary>
    public class DrawingImage : AvaloniaObject, IImage, IAffectsRender
    {
        private Utilities.TargetWeakEventSubscriber<DrawingImage, EventArgs>? _drawingInvalidatedSubscriber;

        public DrawingImage()
        {
        }

        public DrawingImage(Drawing drawing)
        {
            Drawing = drawing;
        }
        /// <summary>
        /// Defines the <see cref="Drawing"/> property.
        /// </summary>
        public static readonly StyledProperty<Drawing?> DrawingProperty =
            AvaloniaProperty.Register<DrawingImage, Drawing?>(nameof(Drawing));

        /// <summary>
        /// Defines the <see cref="Viewbox"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect?> ViewboxProperty =
            AvaloniaProperty.Register<DrawingImage, Rect?>(nameof(Viewbox));

        /// <inheritdoc/>
        public event EventHandler? Invalidated;

        /// <summary>
        /// Gets or sets the drawing content.
        /// </summary>
        [Content]
        public Drawing? Drawing
        {
            get => GetValue(DrawingProperty);
            set => SetValue(DrawingProperty, value);
        }

        /// <summary>
        /// Gets or sets a rectangular region of <see cref="Drawing"/>, in device independent pixels, to display 
        /// when rendering this image.
        /// </summary>
        /// <remarks>
        /// This value can be used to display only part of <see cref="Drawing"/>, or to surround it with empty 
        /// space. If null, <see cref="Drawing"/> will provide its own viewbox.
        /// </remarks>
        /// <seealso cref="Drawing.GetBounds"/>
        public Rect? Viewbox
        {
            get => GetValue(ViewboxProperty);
            set => SetValue(ViewboxProperty, value);
        }

        /// <inheritdoc/>
        public Size Size => GetBounds().Size;

        private Rect GetBounds() => Viewbox ?? Drawing?.GetBounds() ?? default;

        /// <inheritdoc/>
        void IImage.Draw(
            DrawingContext context,
            Rect sourceRect,
            Rect destRect)
        {
            if (Drawing is not { } drawing || sourceRect.Size == default || destRect.Size == default)
            {
                return;
            }

            var bounds = GetBounds();

            if (bounds.Size == default)
            {
                return;
            }

            var scale = Matrix.CreateScale(
                destRect.Width / sourceRect.Width,
                destRect.Height / sourceRect.Height);
            var translate = Matrix.CreateTranslation(
                -sourceRect.X + destRect.X - bounds.X,
                -sourceRect.Y + destRect.Y - bounds.Y);

            using (context.PushClip(destRect))
            using (context.PushTransform(translate * scale))
            {
                drawing.Draw(context);
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DrawingProperty)
            {
                var (oldDrawing, newDrawing) = change.GetOldAndNewValue<Drawing?>();

                if (oldDrawing is not null && _drawingInvalidatedSubscriber is not null)
                {
                    Drawing.InvalidatedWeakEvent.Unsubscribe(oldDrawing, _drawingInvalidatedSubscriber);
                }

                if (newDrawing is not null)
                {
                    _drawingInvalidatedSubscriber ??=
                        new Utilities.TargetWeakEventSubscriber<DrawingImage, EventArgs>(
                            this,
                            static (target, _, _, _) => target.RaiseInvalidated(EventArgs.Empty));
                    Drawing.InvalidatedWeakEvent.Subscribe(newDrawing, _drawingInvalidatedSubscriber);
                }

                RaiseInvalidated(EventArgs.Empty);
            }
            else if (change.Property == ViewboxProperty)
            {
                RaiseInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);
    }
}
