// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Media;
using Perspex.Media.Imaging;

namespace Perspex.Controls
{
    /// <summary>
    /// Displays a <see cref="Bitmap"/> image.
    /// </summary>
    public class Image : Control
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly PerspexProperty<Bitmap> SourceProperty =
            PerspexProperty.Register<Image, Bitmap>(nameof(Source));

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly PerspexProperty<Stretch> StretchProperty =
            PerspexProperty.Register<Image, Stretch>(nameof(Stretch), Stretch.Uniform);
        
        /// <summary>
        /// Gets or sets the bitmap image that will be displayed.
        /// </summary>
        public Bitmap Source
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
        public override void Render(IDrawingContext context)
        {
            Bitmap source = Source;

            if (source != null)
            {
                Rect viewPort = new Rect(Bounds.Size);
                Size sourceSize = new Size(source.PixelWidth, source.PixelHeight);
                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterIn(new Rect(scaledSize))
                    .Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize)
                    .CenterIn(new Rect(destRect.Size / scale));

                context.DrawImage(source, 1, sourceRect, destRect);
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;
            Vector scale = new Vector();

            if (Source != null)
            {
                width = Source.PixelWidth;
                height = Source.PixelHeight;

                if (Width > 0)
                {
                    availableSize = new Size(Width, availableSize.Height);
                }

                if (Height > 0)
                {
                    availableSize = new Size(availableSize.Width, Height);
                }

                scale = Stretch.CalculateScaling(availableSize, new Size(width, height));
            }

            return new Size(width * scale.X, height * scale.Y);
        }
    }
}