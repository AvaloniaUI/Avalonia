// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Media.Mutable
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    /// <remarks>
    /// This is a mutable version of the normal immutable <see cref="Perspex.Media.SolidColorBrush"/>
    /// for use in XAML. XAML really needs support for immutable data...
    /// </remarks>
    public class SolidColorBrush : Brush, ISolidColorBrush
    {
        public static readonly DirectProperty<SolidColorBrush, Color> ColorProperty =
            PerspexProperty.RegisterDirect<SolidColorBrush, Color>(
                "Color",
                o => o.Color,
                (o, v) => o.Color = v);

        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        public Color Color { get; set; }

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
