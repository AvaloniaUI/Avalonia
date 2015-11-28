using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Perspex;
using Perspex.Media;
using Perspex.Rendering;
using Perspex.Layout;
using Perspex.Media.Imaging;
using Perspex.Platform;


namespace Perspex.RenderHelpers
{
    class TileBrushImplHelper
    {
        public Size IntermediateSize { get; }
        public Rect DestinationRect { get; }
        private readonly TileMode _tileMode;
        private readonly Rect _sourceRect;
        private readonly Vector _scale;
        private readonly Vector _translate;
        private readonly Size _imageSize;
        private readonly VisualBrush _visualBrush;
        private readonly ImageBrush _imageBrush;
        private readonly Matrix _transform;
        private readonly Rect _drawRect;

        public bool IsValid { get; }


        public TileBrushImplHelper(TileBrush brush, Size targetSize)
        {
            _imageBrush = brush as ImageBrush;
            _visualBrush = brush as VisualBrush;
            if (_imageBrush != null)
            {
                if (_imageBrush.Source == null)
                    return;
                _imageSize = new Size(_imageBrush.Source.PixelWidth, _imageBrush.Source.PixelHeight);
                IsValid = true;
            }
            else if (_visualBrush != null)
            {
                var visual = _visualBrush.Visual;
                if (visual == null)
                    return;
                var layoutable = visual as ILayoutable;

                if (layoutable?.IsArrangeValid == false)
                {
                    layoutable.Measure(Size.Infinity);
                    layoutable.Arrange(new Rect(layoutable.DesiredSize));
                }
                //I have no idea why are we using layoutable after `as` cast, but it was in original VisualBrush code by @grokys
                _imageSize = layoutable.Bounds.Size;
                IsValid = true;
            }
            else
                return;

            _tileMode = brush.TileMode;
            _sourceRect = brush.SourceRect.ToPixels(_imageSize);
            DestinationRect = brush.DestinationRect.ToPixels(targetSize);
            _scale = brush.Stretch.CalculateScaling(DestinationRect.Size, _sourceRect.Size);
            _translate = CalculateTranslate(brush, _sourceRect, DestinationRect, _scale);
            IntermediateSize = CalculateIntermediateSize(_tileMode, targetSize, DestinationRect.Size);
            _transform = CalculateIntermediateTransform(
                _tileMode,
                _sourceRect,
                DestinationRect,
                _scale,
                _translate,
                out _drawRect);
        }

        public bool NeedsIntermediateSurface
        {
            get
            {
                if (_imageBrush == null)
                    return true;
                if (_transform != Matrix.Identity)
                    return true;
                if (_sourceRect.Position != default(Point))
                    return true;
                if ((int) _sourceRect.Width != _imageBrush.Source.PixelWidth ||
                    (int) _sourceRect.Height != _imageBrush.Source.PixelHeight)
                    return true;
                return false;
            }
        }

        public T GetDirect<T>() => (T) _imageBrush?.Source.PlatformImpl;

        public void DrawIntermediate(DrawingContext ctx)
        {
            using (ctx.PushClip(_drawRect))
            using (ctx.PushPostTransform(_transform))
            {
                if (_imageBrush != null)
                {
                    var bmpRc = new Rect(0, 0, _imageBrush.Source.PixelWidth, _imageBrush.Source.PixelHeight);
                    ctx.DrawImage(_imageBrush.Source, 1, bmpRc, bmpRc);
                }
                else if (_visualBrush != null)
                {
                    ctx.FillRectangle(Brushes.Black, new Rect(new Point(0, 0), IntermediateSize));
                    ctx.Render(_visualBrush.Visual);
                }
            }
        }


        /// <summary>
        /// Calculates a _translate based on a <see cref="TileBrush"/>, a source and destination
        /// rectangle and a _scale.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="sourceRect">The source rectangle.</param>
        /// <param name="destinationRect">The destination rectangle.</param>
        /// <param name="scale">The _scale factor.</param>
        /// <returns>A vector with the X and Y _translate.</returns>

        public static Vector CalculateTranslate(
            TileBrush brush,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale)
        {
            var x = 0.0;
            var y = 0.0;
            var size = sourceRect.Size*scale;

            switch (brush.AlignmentX)
            {
                case AlignmentX.Center:
                    x += (destinationRect.Width - size.Width)/2;
                    break;
                case AlignmentX.Right:
                    x += destinationRect.Width - size.Width;
                    break;
            }

            switch (brush.AlignmentY)
            {
                case AlignmentY.Center:
                    y += (destinationRect.Height - size.Height)/2;
                    break;
                case AlignmentY.Bottom:
                    y += destinationRect.Height - size.Height;
                    break;
            }

            return new Vector(x, y);
        }


        public static Matrix CalculateIntermediateTransform(
            TileMode tileMode,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale,
            Vector translate,
            out Rect drawRect)
        {
            var transform = Matrix.CreateTranslation(-sourceRect.Position)*
                            Matrix.CreateScale(scale)*
                            Matrix.CreateTranslation(translate);
            Rect dr;

            if (tileMode == TileMode.None)
            {
                dr = destinationRect;
                transform *= Matrix.CreateTranslation(destinationRect.Position);
            }
            else
            {
                dr = new Rect(destinationRect.Size);
            }

            drawRect = dr;

            return transform;
        }




        static Size CalculateIntermediateSize(
            TileMode tileMode,
            Size targetSize,
            Size destinationSize) => tileMode == TileMode.None ? targetSize : destinationSize;

    }
}