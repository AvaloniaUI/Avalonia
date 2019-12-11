// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Controls
{   
    /// <summary>
    /// Displays a <see cref="Bitmap"/> image.
    /// </summary>
    public class Image : Control
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<IBitmap> SourceProperty =
            AvaloniaProperty.Register<Image, IBitmap>(nameof(Source));

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Image, Stretch>(nameof(Stretch), Stretch.Uniform);

        static Image()
        {
            AffectsRender<Image>(SourceProperty, StretchProperty);
            AffectsMeasure<Image>(SourceProperty, StretchProperty);
        }

        /// <summary>
        /// Gets or sets the bitmap image that will be displayed.
        /// </summary>
        public IBitmap Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value controlling how the image will be stretched.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var source = Source;

            if (source != null)
            {
                Rect viewPort = new Rect(Bounds.Size);
                Size sourceSize = new Size(source.PixelSize.Width, source.PixelSize.Height);
                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterRect(new Rect(scaledSize))
                    .Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize)
                    .CenterRect(new Rect(destRect.Size / scale));

                var interpolationMode = RenderOptions.GetBitmapInterpolationMode(this);

                context.DrawImage(source, 1, sourceRect, destRect, interpolationMode);
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var source = Source;
            var result = new Size();

            if (source != null)
            {
                Size sourceSize = new Size(source.PixelSize.Width, source.PixelSize.Height);
                if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                {
                    result = sourceSize;
                }
                else
                {
                    result = Stretch.CalculateSize(availableSize, sourceSize);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var source = Source;

            if (source != null)
            {
                var sourceSize = new Size(source.PixelSize.Width, source.PixelSize.Height);
                var result = Stretch.CalculateSize(finalSize, sourceSize);
                return result;
            }
            else
            {
                return new Size();
            }
        }
    }
}
