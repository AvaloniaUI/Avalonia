// -----------------------------------------------------------------------
// <copyright file="Image.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Media;
    using Perspex.Media.Imaging;

    public class Image : Control
    {
        public static readonly PerspexProperty<Bitmap> SourceProperty =
            PerspexProperty.Register<Image, Bitmap>("Source");

        public static readonly PerspexProperty<Stretch> StretchProperty =
            PerspexProperty.Register<Image, Stretch>("Stretch", Stretch.Uniform);

        public Bitmap Source
        {
            get { return this.GetValue(SourceProperty); }
            set { this.SetValue(SourceProperty, value); }
        }

        public Stretch Stretch 
        { 
            get { return (Stretch)this.GetValue(StretchProperty); }
            set { this.SetValue(StretchProperty, value); }
        }

        public override void Render(IDrawingContext drawingContext)
        {
            Bitmap source = this.Source;

            if (source != null)
            {
                Rect viewPort = new Rect(this.ActualSize);
                Size sourceSize = new Size(source.PixelWidth, source.PixelHeight);
                Vector scale = CalculateScaling(this.ActualSize, sourceSize, this.Stretch);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterIn(new Rect(scaledSize))
                    .Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize)
                    .CenterIn(new Rect(destRect.Size / scale));

                drawingContext.DrawImage(source, 1, sourceRect, destRect);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;
            Vector scale = new Vector();

            if (this.Source != null)
            {
                width = this.Source.PixelWidth;
                height = this.Source.PixelHeight;

                if (this.Width > 0)
                {
                    availableSize = new Size(this.Width, availableSize.Height);
                }

                if (this.Height > 0)
                {
                    availableSize = new Size(availableSize.Width, this.Height);
                }

                scale = CalculateScaling(availableSize, new Size(width, height), this.Stretch);
            }

            return new Size(width * scale.X, height * scale.Y);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        private static Vector CalculateScaling(Size availableSize, Size imageSize, Stretch stretch)
        {
            double scaleX = 1;
            double scaleY = 1;

            if (stretch != Stretch.None)
            {
                scaleX = availableSize.Width / imageSize.Width;
                scaleY = availableSize.Height / imageSize.Height;

                switch (stretch)
                {
                    case Stretch.Uniform:
                        scaleX = scaleY = Math.Min(scaleX, scaleY);
                        break;
                    case Stretch.UniformToFill:
                        scaleX = scaleY = Math.Max(scaleX, scaleY);
                        break;
                }
            }

            return new Vector(scaleX, scaleY);
        }
    }
}
