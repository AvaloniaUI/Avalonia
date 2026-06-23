using System;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// An <see cref="IImage"/> that uses a <see cref="Drawing"/> for content.
    /// </summary>
    public class DrawingImage : AvaloniaObject, IImage, IAffectsRender
    {
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

        /// <summary>
        /// When Viewbox is set, the natural size exposed to Image should reflect the viewbox
        /// so that Image computes destRect (with Stretch policies) based on that region.
        /// </summary>
        /// <returns>the calculated bounds</returns>
        private Rect GetBounds() => Viewbox.HasValue ? Viewbox.Value : Drawing?.GetBounds() ?? default;

        /// <inheritdoc/>
        void IImage.Draw(
            DrawingContext context,
            Rect sourceRect,
            Rect destRect)
        {
            if (Drawing is not { } drawing || 
                sourceRect.Width <= 0 || sourceRect.Height <= 0 ||
                destRect.Width <= 0 || destRect.Height <= 0)
            {
                return;
            }

            var bounds = GetBounds();
            // Use the sourceRect provided by the caller (Image). When ViewBox is set, Size
            // is based on ViewBox and Image will pass a 0-based sourceRect sized to the ViewBox.
            var localSource = sourceRect;

            if (bounds.Size == default)
            {
                return;
            }

            var scale = Matrix.CreateScale(
                destRect.Width / localSource.Width,
                destRect.Height / localSource.Height);
            // Align using the region we are displaying: if Viewbox is set, align to Viewbox
            // (so we account for its X/Y offset); otherwise align to the drawing's bounds.
            var alignBounds = Viewbox ?? (Drawing?.GetBounds() ?? bounds);
            var translate = Matrix.CreateTranslation(
                destRect.X - localSource.X - alignBounds.X,
                destRect.Y - localSource.Y - alignBounds.Y);

            // Always clip. If Viewbox is set (explicit crop), do NOT expand the clip beyond destRect.
            // Otherwise, expand by effect output padding to avoid cutting off effect pixels.
            var clipRect = destRect;
            if (!Viewbox.HasValue)
            {
                var outer = drawing.GetOuterBounds();
                var inner = drawing.GetEffectContentBounds();
                if (!outer.IsEmpty() && !inner.IsEmpty() && inner is { Width: > 0, Height: > 0 })
                {
                    var sx = destRect.Width / localSource.Width;
                    var sy = destRect.Height / localSource.Height;

                    // Expand the destination clip by the positive padding between effect output (outer)
                    // and effect content (inner), scaled to destination pixels.
                    var dxLeft = Math.Max(0, (inner.X - outer.X) * sx);
                    var dyTop = Math.Max(0, (inner.Y - outer.Y) * sy);
                    var dxRight = Math.Max(0, ((outer.X + outer.Width) - (inner.X + inner.Width)) * sx);
                    var dyBottom = Math.Max(0, ((outer.Y + outer.Height) - (inner.Y + inner.Height)) * sy);

                    clipRect = new Rect(
                        clipRect.X - dxLeft,
                        clipRect.Y - dyTop,
                        clipRect.Width + dxLeft + dxRight,
                        clipRect.Height + dyTop + dyBottom);
                }
            }
            using var clipScope = context.PushClip(clipRect);
            using (context.PushTransform(translate * scale))
            {
                drawing.Draw(context);
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DrawingProperty || change.Property == ViewboxProperty)
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
