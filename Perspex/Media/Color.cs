// -----------------------------------------------------------------------
// <copyright file="Color.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// An ARGB color.
    /// </summary>
    public struct Color
    {
        /// <summary>
        /// Gets or sets the Alpha component of the color.
        /// </summary>
        public byte A { get; set; }

        /// <summary>
        /// Gets or sets the Red component of the color.
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// Gets or sets the Green component of the color.
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// Gets or sets the Blue component of the color.
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// Creates a <see cref="Color"/> from alpha, red, green and blue components.
        /// </summary>
        /// <param name="a">The alpha component.</param>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The color.</returns>
        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return new Color
            {
                A = a,
                R = r,
                G = g,
                B = b,
            };
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from red, green and blue components.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The color.</returns>
        public static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color
            {
                A = 0xff,
                R = r,
                G = g,
                B = b,
            };
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from an integer.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>The color.</returns>
        public static Color FromUInt32(uint value)
        {
            return new Color
            {
                A = (byte)((value >> 24) & 0xff),
                R = (byte)((value >> 16) & 0xff),
                G = (byte)((value >> 8) & 0xff),
                B = (byte)(value & 0xff),
            };
        }

        /// <summary>
        /// Returns the string representation of the color.
        /// </summary>
        /// <returns>
        /// The string representation of the color.
        /// </returns>
        public override string ToString()
        {
            uint rgb = ((uint)this.A << 24) | ((uint)this.R << 16) | ((uint)this.G << 8) | (uint)this.B;
            return string.Format("#{0:x8}", rgb);
        }

        /// <summary>
        /// Returns the integer representation of the color.
        /// </summary>
        /// <returns>
        /// The integer representation of the color.
        /// </returns>
        public uint ToUint32()
        {
            return ((uint)this.A << 24) | ((uint)this.R << 16) | ((uint)this.G << 8) | (uint)this.B;
        }
    }
}
