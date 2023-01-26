using System;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Represents a range of text returned by an <see cref="ITextProvider"/>.
    /// </summary>
    public readonly struct TextRange : IEquatable<TextRange>
    {
        /// <summary>
        /// Instantiates a new <see cref="TextRange"/> instance with the specified start index and
        /// length.
        /// </summary>
        /// <param name="start">The inclusive start index of the range.</param>
        /// <param name="length">The length of the range.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="length"/> was negative.
        /// </exception>
        public TextRange(int start, int length)
        {
            if (length < 0)
                throw new ArgumentException("Length may not be negative", nameof(length));
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Gets the inclusive start index of the range.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the exclusive end index of the range.
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// Gets the length of the range.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets an empty <see cref="TextRange"/>.
        /// </summary>
        public static TextRange Empty => new(0, 0);

        /// <summary>
        /// Creates a new <see cref="TextRange"/> from an inclusive start and end index.
        /// </summary>
        /// <param name="start">The inclusive start index of the range.</param>
        /// <param name="end">The inclusive end index of the range.</param>
        /// <returns></returns>
        public static TextRange FromInclusiveStartEnd(int start, int end)
        {
            var s = Math.Min(start, end);
            var e = Math.Max(start, end);
            return new(s, e - s);
        }

        public override bool Equals(object? obj) => obj is TextRange range && Equals(range);
        public bool Equals(TextRange other) => Start == other.Start && Length == other.Length;

        public override int GetHashCode()
        {
            var hashCode = -1730557556;
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(TextRange left, TextRange right) => left.Equals(right);
        public static bool operator !=(TextRange left, TextRange right) => !(left == right);
    }
}
