// Portions of this source file are adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Contains and allows modification of Hue, Saturation and Value components.
    /// </summary>
    /// <remarks>
    ///   The is a specialized struct optimized for performance and memory:
    ///   <list type="bullet">
    ///     <item>This is not a read-only struct like <see cref="HsvColor"/> and allows editing the fields</item>
    ///     <item>Removes the alpha component unnecessary in core calculations</item>
    ///     <item>No component bounds checks or clamping is done.</item>
    ///   </list>
    /// </remarks>
    internal struct Hsv
    {
        /// <summary>
        /// The Hue component in the range from 0..359.
        /// </summary>
        public double H;

        /// <summary>
        /// The Saturation component in the range from 0..1.
        /// </summary>
        public double S;

        /// <summary>
        /// The Value component in the range from 0..1.
        /// </summary>
        public double V;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hsv"/> struct.
        /// </summary>
        /// <param name="h">The Hue component in the range from 0..360.</param>
        /// <param name="s">The Saturation component in the range from 0..1.</param>
        /// <param name="v">The Value component in the range from 0..1.</param>
        public Hsv(double h, double s, double v)
        {
            H = h;
            S = s;
            V = v;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hsv"/> struct.
        /// </summary>
        /// <param name="hsvColor">An existing <see cref="HsvColor"/> to convert to <see cref="Hsv"/>.</param>
        public Hsv(HsvColor hsvColor)
        {
            H = hsvColor.H;
            S = hsvColor.S;
            V = hsvColor.V;
        }

        /// <summary>
        /// Converts this <see cref="Hsv"/> struct into a standard <see cref="HsvColor"/>.
        /// </summary>
        /// <param name="alpha">The Alpha component in the range from 0..1.</param>
        /// <returns>A new <see cref="HsvColor"/> representing this <see cref="Hsv"/> struct.</returns>
        public HsvColor ToHsvColor(double alpha = 1.0)
        {
            // Clamping is done automatically in the constructor
            return HsvColor.FromAhsv(alpha, H, S, V);
        }

        /// <summary>
        /// Returns the <see cref="Rgb"/> color model equivalent of this <see cref="Hsv"/> color.
        /// </summary>
        /// <returns>The <see cref="Rgb"/> equivalent color.</returns>
        public Rgb ToRgb()
        {
            // Instantiating a Color is unfortunately necessary to use existing conversions
            // Clamping is done internally in the conversion method
            Color color = HsvColor.ToRgb(H, S, V);

            return new Rgb(color);
        }
    }
}
