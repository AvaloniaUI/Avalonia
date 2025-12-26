using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a box shadow which can be attached to an element or control.
    /// </summary>
    public struct BoxShadow
    {
        private readonly static char[] s_Separator = new char[] { ' ', '\t' };
        private const char OpeningParenthesis = '(';
        private const char ClosingParenthesis = ')';

        /// <summary>
        /// Gets or sets the horizontal offset (distance) of the shadow.
        /// </summary>
        /// <remarks>
        /// Positive values place the shadow to the right of the element while
        /// negative values place the shadow to the left.
        /// </remarks>
        public double OffsetX { get; set; }

        /// <summary>
        /// Gets or sets the vertical offset (distance) of the shadow.
        /// </summary>
        /// <remarks>
        /// Positive values place the shadow below the element while
        /// negative values place the shadow above.
        /// </remarks>
        public double OffsetY { get; set; }

        /// <summary>
        /// Gets or sets the blur radius.
        /// This is used to control the amount of blurring.
        /// </summary>
        /// <remarks>
        /// The larger this value, the bigger the blur effect, so the shadow becomes larger and more transparent.
        /// Negative values are not allowed. If not specified, the default (zero) is used and the shadow edge is sharp.
        /// </remarks>
        public double Blur { get; set; }

        /// <summary>
        /// Gets or sets the spread radius.
        /// This is used to control the overall size of the shadow.
        /// </summary>
        /// <remarks>
        /// Positive values will cause the shadow to expand and grow larger, negative values will cause the shadow to shrink.
        /// If not specified, the default (zero) is used and the shadow will be the same size as the element.
        /// </remarks>
        public double Spread { get; set; }

        /// <summary>
        /// Gets or sets the color of the shadow.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shadow is inset and drawn within the element rather than outside of it.
        /// </summary>
        /// <remarks>
        /// Inset changes the shadow to inside the element (as if the content was depressed inside the box).
        /// If false (the default), the shadow is assumed to be a drop shadow (as if the box were raised above the content).
        /// <br/><br/>
        /// Inset shadows are drawn inside the element, above the background (even when it's transparent), but below any content.
        /// </remarks>
        public bool IsInset { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.
        /// </returns>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Bit equality is adequate here")]
        public bool Equals(in BoxShadow other)
        {
            return OffsetX == other.OffsetX
                && OffsetY == other.OffsetY
                && Blur == other.Blur
                && Spread == other.Spread
                && Color.Equals(other.Color)
                && IsInset == other.IsInset;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is BoxShadow other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OffsetX.GetHashCode();
                hashCode = (hashCode * 397) ^ OffsetY.GetHashCode();
                hashCode = (hashCode * 397) ^ Blur.GetHashCode();
                hashCode = (hashCode * 397) ^ Spread.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                hashCode = (hashCode * 397) ^ IsInset.GetHashCode();
                return hashCode;
            }
        }

        struct ArrayReader
        {
            private int _index;
            private readonly string[] _arr;

            public ArrayReader(string[] arr)
            {
                _arr = arr;
                _index = 0;
            }

            public bool TryReadString([MaybeNullWhen(false)] out string s)
            {
                s = null;
                if (_index >= _arr.Length)
                {
                    return false;
                }

                s = _arr[_index];
                _index++;

                return true;
            }

            public string ReadString()
            {
                if (!TryReadString(out var rv))
                {
                    throw new FormatException();
                }

                return rv;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = StringBuilderCache.Acquire();
            ToString(sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal void ToString(StringBuilder sb)
        {
            if (this == default)
            {
                sb.Append("none");
                return;
            }

            if (IsInset)
            {
                sb.Append("inset ");
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", OffsetX);

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", OffsetY);

            if (Blur != 0.0 || Spread != 0.0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", Blur);
            }

            if (Spread != 0.0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", Spread);
            }

            Color.ToString(sb);
        }

        /// <summary>
        /// Parses a <see cref="BoxShadow"/> string.
        /// </summary>
        /// <remarks>
        /// A box shadow may be specified in multiple formats with separate components:
        ///   <list type="bullet">
        ///     <item>Two, three, or four length values.</item>
        ///     <item>A color value.</item>
        ///     <item>An optional inset keyword.</item>
        ///   </list>
        /// If only two length values are given they will be interpreted as <see cref="OffsetX"/> and <see cref="OffsetY"/>.
        /// If a third value is given, it is interpreted as a <see cref="Blur"/>, and if a fourth value is given,
        /// it is interpreted as <see cref="Spread"/>.
        /// </remarks>
        /// <param name="s">The input string to parse.</param>
        /// <returns>A new <see cref="BoxShadow"/></returns>
        public static unsafe BoxShadow Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }

            if (s.Length == 0)
            {
                throw new FormatException();
            }

            var p = StringSplitter.SplitRespectingBrackets(
                s, s_Separator,
                OpeningParenthesis, ClosingParenthesis,
                StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 1 && p[0] == "none")
            {
                return default;
            }

            if (p.Length < 3 || p.Length > 6)
            {
                throw new FormatException();
            }

            bool inset = false;

            var tokenizer = new ArrayReader(p);

            string firstToken = tokenizer.ReadString();
            if (firstToken == "inset")
            {
                inset = true;
                firstToken = tokenizer.ReadString();
            }

            var offsetX = double.Parse(firstToken, CultureInfo.InvariantCulture);
            var offsetY = double.Parse(tokenizer.ReadString(), CultureInfo.InvariantCulture);
            double blur = 0;
            double spread = 0;

            tokenizer.TryReadString(out var token3);
            tokenizer.TryReadString(out var token4);
            tokenizer.TryReadString(out var token5);

            if (token4 != null)
            {
                blur = double.Parse(token3!, CultureInfo.InvariantCulture);
            }

            if (token5 != null)
            {
                spread = double.Parse(token4!, CultureInfo.InvariantCulture);
            }

            var color = Color.Parse(token5 ?? token4 ?? token3!);
            return new BoxShadow
            {
                IsInset = inset,
                OffsetX = offsetX,
                OffsetY = offsetY,
                Blur = blur,
                Spread = spread,
                Color = color
            };
        }

        /// <summary>
        /// Transforms the specified bounding rectangle to account for the shadow's offset, spread, and blur.
        /// </summary>
        /// <param name="rect">The original bounding <see cref="Rect"/> to transform.</param>
        /// <returns>
        /// A new <see cref="Rect"/> that includes the shadow's offset, spread, and blur if the shadow is not inset;
        /// otherwise, the original rectangle.
        /// </returns>
        public Rect TransformBounds(in Rect rect)
            => IsInset ? rect : rect.Translate(new Vector(OffsetX, OffsetY)).Inflate(Spread + Blur);

        /// <summary>
        /// Determines whether two <see cref="BoxShadow"/> values are equal.
        /// </summary>
        /// <param name="left">The first <see cref="BoxShadow"/> to compare.</param>
        /// <param name="right">The second <see cref="BoxShadow"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="BoxShadow"/> values are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(BoxShadow left, BoxShadow right) =>
            left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="BoxShadow"/> values are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="BoxShadow"/> to compare.</param>
        /// <param name="right">The second <see cref="BoxShadow"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="BoxShadow"/> values are not equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(BoxShadow left, BoxShadow right) => 
            !(left == right);
    }
}
