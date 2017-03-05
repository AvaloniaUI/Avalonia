// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.RenderHelpers
{
    internal class TileBrushImplHelper
    {
        public Size IntermediateSize { get; }
        public Rect DestinationRect { get; }
        private readonly TileMode _tileMode;
        private readonly Rect _sourceRect;
        private readonly Vector _scale;
        private readonly Vector _translate;
        private readonly Size _imageSize;
        private readonly IVisualBrush _visualBrush;
        private readonly IImageBrush _imageBrush;
        private readonly Matrix _transform;
        private readonly Rect _drawRect;

        public bool IsValid { get; }
        
        public TileBrushImplHelper(ITileBrush brush, Size targetSize)
        {
            _imageBrush = brush as IImageBrush;
            _visualBrush = brush as IVisualBrush;
            if (_imageBrush != null)
            {
                if (_imageBrush.Source == null)
                    return;
                _imageSize = new Size(_imageBrush.Source.PixelWidth, _imageBrush.Source.PixelHeight);
                IsValid = true;
            }
            else if (_visualBrush != null)
            {
                var control = _visualBrush.Visual as IControl;

                if (control != null)
                {
                    EnsureInitialized(control);
                    _imageSize = control.Bounds.Size;
                    IsValid = true;
                }
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
                    using (ctx.PushPostTransform(Matrix.CreateTranslation(-_visualBrush.Visual.Bounds.Position)))
                    {
                        ImmediateRenderer.Render(_visualBrush.Visual, ctx);
                    }
                }
            }
        }


        /// <summary>
        /// Calculates a translate based on an <see cref="ITileBrush"/>, a source and destination
        /// rectangle and a scale.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="sourceRect">The source rectangle.</param>
        /// <param name="destinationRect">The destination rectangle.</param>
        /// <param name="scale">The _scale factor.</param>
        /// <returns>A vector with the X and Y _translate.</returns>

        public static Vector CalculateTranslate(
            ITileBrush brush,
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

        public static void EnsureInitialized(IVisual visual)
        {
            var control = visual as IControl;

            if (control != null)
            {
                foreach (var i in control.GetSelfAndVisualDescendents())
                {
                    var c = i as IControl;

                    if (c?.IsInitialized == false)
                    {
                        var init = c as ISupportInitialize;

                        if (init != null)
                        {
                            init.BeginInit();
                            init.EndInit();
                        }
                    }
                }

                if (!control.IsArrangeValid)
                {
                    control.Measure(Size.Infinity);
                    control.Arrange(new Rect(control.DesiredSize));
                }
            }
        }

        private static Size CalculateIntermediateSize(
            TileMode tileMode,
            Size targetSize,
            Size destinationSize) => tileMode == TileMode.None ? targetSize : destinationSize;
    }
}