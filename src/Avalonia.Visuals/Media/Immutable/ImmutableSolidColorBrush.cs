// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public readonly struct ImmutableSolidColorBrush : ISolidColorBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        public ImmutableSolidColorBrush(Color color, double opacity = 1)
        {
            Color = color;
            Opacity = opacity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        public ImmutableSolidColorBrush(uint color)
            : this(Color.FromUInt32(color))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        public ImmutableSolidColorBrush(ISolidColorBrush source)
            : this(source.Color, source.Opacity)
        {
        }

        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets the opacity of the brush.
        /// </summary>
        public double Opacity { get; }

        /// <summary>
        /// Returns a string representation of the brush.
        /// </summary>
        /// <returns>A string representation of the brush.</returns>
        public override string ToString()
        {
            return Color.ToString();
        }
    }
}
