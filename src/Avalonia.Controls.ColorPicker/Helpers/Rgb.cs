// Portions of this source file are adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Contains and allows modification of Red, Green and Blue components.
    /// </summary>
    /// <remarks>
    ///   The is a specialized struct optimized for performance and memory:
    ///   <list type="bullet">
    ///     <item>This is not a read-only struct like <see cref="Color"/> and allows editing the fields</item>
    ///     <item>Removes the alpha component unnecessary in core calculations</item>
    ///     <item>Normalizes RGB components in the range of 0..1 to simplify calculations.</item>
    ///     <item>No component bounds checks or clamping is done.</item>
    ///   </list>
    /// </remarks>
    internal struct Rgb
    {
        /// <summary>
        /// The Red component in the range from 0..1.
        /// </summary>
        public double R;

        /// <summary>
        /// The Green component in the range from 0..1.
        /// </summary>
        public double G;

        /// <summary>
        /// The Blue component in the range from 0..1.
        /// </summary>
        public double B;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rgb"/> struct.
        /// </summary>
        /// <param name="r">The Red component in the range from 0..1.</param>
        /// <param name="g">The Green component in the range from 0..1.</param>
        /// <param name="b">The Blue component in the range from 0..1.</param>
        public Rgb(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rgb"/> struct.
        /// </summary>
        /// <param name="color">An existing <see cref="Color"/> to convert to <see cref="Rgb"/>.</param>
        public Rgb(Color color)
        {
            R = color.R / 255.0;
            G = color.G / 255.0;
            B = color.B / 255.0;
        }

        /// <summary>
        /// Converts this <see cref="Rgb"/> struct into a standard <see cref="Color"/>.
        /// </summary>
        /// <param name="alpha">The Alpha component in the range from 0..1.</param>
        /// <returns>A new <see cref="Color"/> representing this <see cref="Rgb"/> struct.</returns>
        public Color ToColor(double alpha = 1.0)
        {
            return Color.FromArgb(
                (byte)MathUtilities.Clamp(alpha * 255.0, 0x00, 0xFF),
                (byte)MathUtilities.Clamp(R * 255.0, 0x00, 0xFF),
                (byte)MathUtilities.Clamp(G * 255.0, 0x00, 0xFF),
                (byte)MathUtilities.Clamp(B * 255.0, 0x00, 0xFF));
        }

        /// <summary>
        /// Returns the <see cref="Hsv"/> color model equivalent of this <see cref="Rgb"/> color.
        /// </summary>
        /// <returns>The <see cref="Hsv"/> equivalent color.</returns>
        public Hsv ToHsv()
        {
            // Instantiating an HsvColor is unfortunately necessary to use existing conversions
            // Clamping must be done here as it isn't done in the conversion method (internal-use only)
            HsvColor hsvColor = Color.ToHsv(
                MathUtilities.Clamp(R, 0.0, 1.0),
                MathUtilities.Clamp(G, 0.0, 1.0),
                MathUtilities.Clamp(B, 0.0, 1.0));

            return new Hsv(hsvColor);
        }
    }
}
