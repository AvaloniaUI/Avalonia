using System;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Unique text formatting properties that are used by the <see cref="TextFormatter"/>.
    /// </summary>
    public readonly struct TextFormat : IEquatable<TextFormat>
    {
        public TextFormat(Typeface typeface, double fontRenderingEmSize)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            FontMetrics = new FontMetrics(typeface, fontRenderingEmSize);
        }

        /// <summary>
        /// Gets the typeface.
        /// </summary>
        /// <value>
        /// The typeface.
        /// </value>
        public Typeface Typeface { get; }

        /// <summary>
        /// Gets the font rendering em size.
        /// </summary>
        /// <value>
        /// The em rendering size of the font.
        /// </value>
        public double FontRenderingEmSize { get; }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        /// <value>
        /// The metrics of the font.
        /// </value>
        public FontMetrics FontMetrics { get; }

        public static bool operator ==(TextFormat self, TextFormat other)
        {
            return self.Equals(other);
        }

        public static bool operator !=(TextFormat self, TextFormat other)
        {
            return !(self == other);
        }

        public bool Equals(TextFormat other)
        {
            return Typeface.Equals(other.Typeface) && FontRenderingEmSize.Equals(other.FontRenderingEmSize);
        }

        public override bool Equals(object obj)
        {
            return obj is TextFormat other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Typeface != null ? Typeface.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ FontRenderingEmSize.GetHashCode();
                return hashCode;
            }
        }
    }
}
