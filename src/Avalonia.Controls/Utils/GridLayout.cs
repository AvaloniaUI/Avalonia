using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Avalonia.Controls.Utils
{
    internal class GridLayout
    {
        internal GridLayout(LengthDefinitions lengths)
        {
            _lengths = lengths;
        }

        private readonly LengthDefinitions _lengths;

        /// <summary>
        /// Try to calculate the lengths that will be used to measure the children.
        /// If the <paramref name="containerLength"/> is not enough, we'll even not compress the measure length.
        /// </summary>
        /// <param name="containerLength">The container length, width or height.</param>
        /// <returns>The lengths that can be used to measure the children.</returns>
        [Pure]
        internal List<double> Measure(double containerLength)
        {
            var lengths = _lengths.Clone();

            // Exclude all the pixel lengths, so that we can calculate the star lengths.
            containerLength -= lengths
                .Where(x => x.Length.IsAbsolute)
                .Aggregate(0.0, (sum, add) => sum + add.Length.Value);

            // Aggregate the star count, so that we can determine the length of each star unit.
            var starCount = lengths
                .Where(x => x.Length.IsStar)
                .Aggregate(0.0, (sum, add) => sum + add.Length.Value);
            // There is no need to care the (starCount == 0). If this happens, we'll ignore all the stars.
            var starUnitLength = containerLength / starCount;

            // If there is no stars, just return all pixels.
            if (Equals(starCount, 0.0))
            {
                return lengths.Select(x => x.Length.IsAuto ? double.PositiveInfinity : x.Length.Value).ToList();
            }

            // ---
            // Warning! The code below will start to change the lengths item value.
            // ---

            // Exclude the star unit if its min/max length range does not contain the calculated star length.
            var intermediateStarLengths = lengths.Where(x => x.Length.IsStar).ToList();
            // Indicate whether all star lengths are in range of min and max or not.
            var allInRange = false;
            while (!allInRange)
            {
                foreach (var length in intermediateStarLengths)
                {
                    // Find out if there is any length out of min to max.
                    var (star, min, max) = (length.Length.Value, length.MinLength, length.MaxLength);
                    var starLength = star * starUnitLength;
                    if (starLength < min || starLength > max)
                    {
                        // If the star length is out of min to max, change it to a pixel unit.
                        if (starLength < min)
                        {
                            length.Update(min);
                            starLength = min;
                        }
                        else if (starLength > max)
                        {
                            length.Update(max);
                            starLength = max;
                        }

                        // Update the rest star length info.
                        intermediateStarLengths.Remove(length);
                        containerLength -= starLength;
                        starCount -= star;
                        starUnitLength = containerLength / starCount;
                        break;
                    }
                }

                // All lengths are in range, so that we have enough lengths to measure children.
                allInRange = true;
                foreach (var length in intermediateStarLengths)
                {
                    length.Update(length.Length.Value * starUnitLength);
                }
            }

            // Return the modified lengths as measuring lengths.
            return lengths.Select(x =>
                x.Length.GridUnitType == GridUnitType.Auto
                    ? double.PositiveInfinity
                    : x.Length.Value).ToList();
        }

        internal class LengthDefinitions : IEnumerable<LengthDefinition>, ICloneable
        {
            private readonly List<LengthDefinition> _lengths;

            private LengthDefinitions(IEnumerable<LengthDefinition> lengths)
            {
                _lengths = lengths.ToList();
            }

            public IEnumerator<LengthDefinition> GetEnumerator() => _lengths.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            object ICloneable.Clone() => Clone();

            public LengthDefinitions Clone() => new LengthDefinitions(
                _lengths.Select(x => new LengthDefinition(x.Length, x.MinLength, x.MaxLength)));

            public static implicit operator LengthDefinitions(RowDefinitions rows)
                => new LengthDefinitions(rows.Select(x => (LengthDefinition) x));

            public static implicit operator LengthDefinitions(ColumnDefinitions rows)
                => new LengthDefinitions(rows.Select(x => (LengthDefinition)x));
        }

        internal class LengthDefinition
        {
            internal LengthDefinition(GridLength length, double minLength, double maxLength)
            {
                Length = length;
                MinLength = minLength;
                MaxLength = maxLength;
            }

            internal GridLength Length { get; private set; }
            internal double MinLength { get; }
            internal double MaxLength { get; }

            public static implicit operator LengthDefinition(RowDefinition row)
                => new LengthDefinition(row.Height, row.MinHeight, row.MaxHeight);

            public static implicit operator LengthDefinition(ColumnDefinition row)
                => new LengthDefinition(row.Width, row.MinWidth, row.MaxWidth);

            public void Update(double pixel)
            {
                Length = new GridLength(pixel);
            }
        }
    }
}
