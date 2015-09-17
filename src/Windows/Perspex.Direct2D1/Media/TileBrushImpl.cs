// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Layout;
using Perspex.Media;
using SharpDX;
using SharpDX.Direct2D1;

namespace Perspex.Direct2D1.Media
{
    public class TileBrushImpl : BrushImpl
    {
        /// <summary>
        /// Calculates a translate based on a <see cref="TileBrush"/>, a source and destination
        /// rectangle and a scale.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="sourceRect">The source rectangle.</param>
        /// <param name="destinationRect">The destination rectangle.</param>
        /// <param name="scale">The scale factor.</param>
        /// <returns>A vector with the X and Y translate.</returns>
        protected static Vector CalculateTranslate(
            TileBrush brush,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale)
        {
            var x = 0.0;
            var y = 0.0;
            var size = sourceRect.Size * scale;

            switch (brush.AlignmentX)
            {
                case AlignmentX.Center:
                    x += (destinationRect.Width - size.Width) / 2;
                    break;
                case AlignmentX.Right:
                    x += destinationRect.Width - size.Width;
                    break;
            }

            switch (brush.AlignmentY)
            {
                case AlignmentY.Center:
                    y += (destinationRect.Height - size.Height) / 2;
                    break;
                case AlignmentY.Bottom:
                    y += destinationRect.Height - size.Height;
                    break;
            }

            return new Vector(x, y);
        }

        protected static Size2F CalculateIntermediateSize(
            TileMode tileMode,
            Size targetSize,
            Size destinationSize)
        {
            var result = tileMode == TileMode.None ? targetSize : destinationSize;
            return result.ToSharpDX();
        }

        protected static Matrix3x2 CalculateIntermediateTransform(
            TileMode tileMode,
            Rect sourceRect,
            Rect destinationRect,
            Vector scale,
            Vector translate,
            out SharpDX.RectangleF drawRect)
        {
            var transform = Matrix.CreateTranslation(-sourceRect.Position) *
                Matrix.CreateScale(scale) *
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

            drawRect = (dr * -transform).ToSharpDX();

            return transform.ToDirect2D();
        }

        protected static BrushProperties GetBrushProperties(TileBrush brush, Rect destinationRect)
        {
            var tileMode = brush.TileMode;

            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = brush.TileMode != TileMode.None ?
                    SharpDX.Matrix3x2.Translation(
                        (float)destinationRect.X,
                        (float)destinationRect.Y) :
                    SharpDX.Matrix3x2.Identity,
            };
        }

        protected static BitmapBrushProperties GetBitmapBrushProperties(TileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        protected static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        protected static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        public override void Dispose()
        {
            ((BitmapBrush)PlatformBrush)?.Bitmap.Dispose();
            base.Dispose();
        }
    }
}
