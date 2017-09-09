// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Shapes;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays an image (either <see cref="IBitmap"/> or <see cref="IDrawing"/>).
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

        private Matrix _transform = Matrix.Identity;

        static Image()
        {
            AffectsRender(SourceProperty);
            AffectsRender(StretchProperty);
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
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var source = Source;

            if (source is IBitmap bitmap)
            {
                RenderBitmap(context, bitmap);
            }
            else if (source is IDrawing drawing)
            {
                RenderDrawing(context, drawing);
            }
        }

        private void RenderBitmap(DrawingContext context, IBitmap bitmap)
        {
            Rect viewPort = new Rect(Bounds.Size);
            Size sourceSize = new Size(bitmap.PixelWidth, bitmap.PixelHeight);
            Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize);
            Size scaledSize = sourceSize * scale;
            Rect destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);
            Rect sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            context.DrawImage(bitmap, 1, sourceRect, destRect);
        }

        private void RenderDrawing(DrawingContext context, IDrawing drawing)
        {
            using (context.PushPreTransform(_transform))
            using (context.PushClip(Bounds))
            {
                drawing.Draw(context);
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

            if (source is IBitmap bitmap)
            {
                return MeasureBitmap(availableSize, bitmap);
            }
            else if (source is IDrawing drawing)
            {
                return MeasureDrawing(availableSize, drawing);
            }
            else
            {
                return new Size();
            }
        }

        private Size MeasureBitmap(Size availableSize, IBitmap bitmap)
        {
            Size sourceSize = new Size(bitmap.PixelWidth, bitmap.PixelHeight);

            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
            {
                return sourceSize;
            }
            else
            {
                return Stretch.CalculateSize(availableSize, sourceSize);
            }
        }

        private Size MeasureDrawing(Size availableSize, IDrawing drawing)
        {
            var (size, transform) = Shape.CalculateSizeAndTransform(availableSize, drawing.GetBounds(), Stretch);

            _transform = transform;

            return size;
        }
    }
}