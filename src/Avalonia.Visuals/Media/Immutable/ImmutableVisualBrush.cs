﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Visuals.Media.Imaging;
using Avalonia.VisualTree;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Paints an area with an <see cref="IVisual"/>.
    /// </summary>
    internal class ImmutableVisualBrush : ImmutableTileBrush, IVisualBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableImageBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        /// <param name="alignmentX">The horizontal alignment of a tile in the destination.</param>
        /// <param name="alignmentY">The vertical alignment of a tile in the destination.</param>
        /// <param name="destinationRect">The rectangle on the destination in which to paint a tile.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="sourceRect">The rectangle of the source image that will be displayed.</param>
        /// <param name="stretch">
        /// How the source rectangle will be stretched to fill the destination rect.
        /// </param>
        /// <param name="tileMode">The tile mode.</param>
        /// <param name="bitmapInterpolationMode">Controls the quality of interpolation.</param>
        public ImmutableVisualBrush(
            IVisual visual,
            AlignmentX alignmentX = AlignmentX.Center,
            AlignmentY alignmentY = AlignmentY.Center,
            RelativeRect? destinationRect = null,
            double opacity = 1,
            RelativeRect? sourceRect = null,
            Stretch stretch = Stretch.Uniform,
            TileMode tileMode = TileMode.None,
            BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default)
            : base(
                  alignmentX,
                  alignmentY,
                  destinationRect ?? RelativeRect.Fill,
                  opacity,
                  sourceRect ?? RelativeRect.Fill,
                  stretch,
                  tileMode,
                  bitmapInterpolationMode)
        {
            Visual = visual;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableVisualBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        public ImmutableVisualBrush(IVisualBrush source)
            : base(source)
        {
            Visual = source.Visual;
        }

        /// <inheritdoc/>
        public IVisual Visual { get; }
    }
}
