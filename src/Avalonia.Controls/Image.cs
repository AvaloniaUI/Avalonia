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
        public static readonly StyledProperty<IImage> SourceProperty =
            AvaloniaProperty.Register<Image, IImage>(nameof(Source));

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Image, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<Image, StretchDirection>(
                nameof(StretchDirection),
                StretchDirection.Both);

        static Image()
        {
            AffectsRender<Image>(SourceProperty, StretchProperty);
            AffectsMeasure<Image>(SourceProperty, StretchProperty);
        }

        /// <summary>
        /// Gets or sets the image that will be displayed.
        /// </summary>
        public IImage Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value controlling how the image will be stretched.
        /// </summary>
        public Stretch Stretch
        {
            get { return GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction the image will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get { return GetValue(StretchDirectionProperty); }
            set { SetValue(StretchDirectionProperty, value); }
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
                Size sourceSize = source.Size;
                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterRect(new Rect(scaledSize))
                    .Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize)
                    .CenterRect(new Rect(destRect.Size / scale));

                var interpolationMode = RenderOptions.GetBitmapInterpolationMode(this);

                context.DrawImage(source, sourceRect, destRect, interpolationMode);
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
                result = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
            }

            return result;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var source = Source;

            if (source != null)
            {
                var sourceSize = source.Size;
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
