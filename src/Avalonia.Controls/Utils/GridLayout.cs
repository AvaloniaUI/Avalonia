using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace Avalonia.Controls.Utils
{
    // We have three kind of unit:
    //     - * means Star  unit. It can be affected by min and max pixel length.
    //     - A means Auto  unit. It can be affected by min/max pixel length and desired pixel length.
    //     - P means Pixel unit. It is fixed and can't be affected by any other values.
    // Notice that some child stands not only one column/row and this affects desired length.
    // Desired length behaviors like the min pixel length but:
    //     - This can only be determined after the Measure.
    // 
    // This is an example indicates how this class stores data.
    // +-----------------------------------------------------------+
    // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
    // +-----------------------------------------------------------+
    // | min | min |     |           | min |     |  min max  |
    //                   |<-   desired   ->|
    // 
    // During the measuring procedure:
    //     - * wants as much as possible space in range of min and max.
    //     - A wants as less as possible space in range of min/desired and max.
    //     - P wants a fix-size space.
    // But during the arranging procedure:
    //     - * behaviors the same.
    //     - A wants as much as possible space in range of min/desired and max.
    //     - P behaviors the same.
    // 
    /// <summary>
    /// Contains algorithms that can help to measure and arrange a Grid.
    /// </summary>
    internal class GridLayout
    {
        internal GridLayout(ColumnDefinitions columns)
        {
            _conventions = columns.Select(x => new LengthConvention(x.Width, x.MinWidth, x.MaxWidth)).ToList();
        }

        internal GridLayout(RowDefinitions rows)
        {
            _conventions = rows.Select(x => new LengthConvention(x.Height, x.MinHeight, x.MaxHeight)).ToList();
        }

        private const double LayoutTolerance = 1.0 / 256.0;
        private readonly List<LengthConvention> _conventions;
        private readonly List<AdditionalLengthConvention> _additionalConventions = new List<AdditionalLengthConvention>();

        /// <summary>
        /// Some elements are not in a single grid cell, they have multiple column/row spans,
        /// and these elements may affects the grid layout especially the measure procedure.<para/>
        /// Append these elements into the convention list can help to layout them correctly through their desired size.
        /// Only a small subset of grid children need to be measured before layout starts and they are called via the <paramref name="getDesiredLength"/> callback.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="getDesiredLength"></param>
        internal void AppendMeasureConventions<T>(IDictionary<T, (int index, int span)> source,
            Func<T, double> getDesiredLength)
        {
            // M1/6. Find all the Auto length columns/rows.
            // Only these columns/rows' layout can be affected by the children desired size.
            var found = new Dictionary<T, (int index, int span)>();
            for (var i = 0; i < _conventions.Count; i++)
            {
                var index = i;
                var convention = _conventions[index];
                if (convention.Length.IsAuto)
                {
                    foreach (var pair in source.Where(x =>
                        x.Value.index <= index && index < x.Value.index + x.Value.span))
                    {
                        found[pair.Key] = pair.Value;
                    }
                }
            }

            // Append these layout into the additional convention list.
            foreach (var pair in found)
            {
                var t = pair.Key;
                var (index, span) = pair.Value;
                var desiredLength = getDesiredLength(t);
                if (Math.Abs(desiredLength) > LayoutTolerance)
                {
                    _additionalConventions.Add(new AdditionalLengthConvention(index, span, desiredLength));
                }
            }
        }

        internal MeasureResult Measure(double containerLength)
        {
            // Initial.
            var conventions = _conventions.Select(x => x.Clone()).ToList();
            var starCount = conventions.Where(x => x.Length.IsStar).Sum(x => x.Length.Value);
            var aggregatedLength = 0.0;
            double starUnitLength;

            // M2/6. Exclude all the pixel lengths, so that we can calculate the star lengths.
            aggregatedLength += conventions.Where(x => x.Length.IsAbsolute).Sum(x => x.Length.Value);

            // M3/6. Exclude all the * lengths that have reached min value.
            var shouldTestStarMin = true;
            while (shouldTestStarMin)
            {
                var @fixed = false;
                starUnitLength = (containerLength - aggregatedLength) / starCount;
                foreach (var convention in conventions.Where(x => x.Length.IsStar))
                {
                    var (star, min) = (convention.Length.Value, convention.MinLength);
                    var starLength = star * starUnitLength;
                    if (starLength < min)
                    {
                        convention.Fix(min);
                        starLength = min;
                        aggregatedLength += starLength;
                        starCount -= star;
                        @fixed = true;
                        break;
                    }
                }

                shouldTestStarMin = @fixed;
            }

            // M4/6. Exclude all the Auto lengths that have not-zero desired size.
            var shouldTestAuto = true;
            while (shouldTestAuto)
            {
                var @fixed = false;
                starUnitLength = (containerLength - aggregatedLength) / starCount;
                for (var i = 0; i < conventions.Count; i++)
                {
                    var convention = conventions[i];
                    if (!convention.Length.IsAuto)
                    {
                        continue;
                    }

                    var index = i;
                    var more = 0.0;
                    foreach (var additional in _additionalConventions)
                    {
                        // If the additional conventions contains the Auto column/row, try to determine the Auto column/row length.
                        if (additional.Index <= index && index < additional.Index + additional.Span)
                        {
                            var starUnit = starUnitLength;
                            var min = Enumerable.Range(additional.Index, additional.Span)
                                .Select(x =>
                                {
                                    var c = conventions[x];
                                    if (c.Length.IsAbsolute) return c.Length.Value;
                                    if (c.Length.IsStar) return c.Length.Value * starUnit;
                                    return 0.0;
                                }).Sum();
                            more = Math.Max(additional.Min - min, more);
                        }
                    }

                    convention.Fix(more);
                    aggregatedLength += more;
                    @fixed = true;
                    break;
                }

                shouldTestAuto = @fixed;
            }

            // M5/6. Determine the desired length of the grid for current contaienr length. Its value stores in desiredLength.
            // But if the container has infinite length, the grid desired length is stored in greedyDesiredLength.
            var desiredLength = containerLength - aggregatedLength >= 0.0 ? aggregatedLength : containerLength;
            var greedyDesiredLength = aggregatedLength;

            // M6/6. Expand all the left stars. These stars have no conventions or only have max value so they can be expanded from zero to constrant.
            var dynamicConvention = ExpandStars(conventions, containerLength);
            Clip(dynamicConvention, containerLength);

            // Stores the measuring result.
            return new MeasureResult(containerLength, desiredLength, greedyDesiredLength,
                conventions, dynamicConvention);
        }

        public ArrangeResult Arrange(double finalLength, MeasureResult measure)
        {
            // If the arrange final length does not equal to the measure length, we should measure again.
            if (finalLength - measure.ContainerLength > LayoutTolerance)
            {
                // If the final length is larger, we will rerun the whole measure.
                measure = Measure(finalLength);
            }
            else if (finalLength - measure.ContainerLength < -LayoutTolerance)
            {
                // If the final length is smaller, we measure the M6/6 procedure only.
                var dynamicConvention = ExpandStars(measure.LeanLengthList, finalLength);
                measure = new MeasureResult(finalLength, measure.DesiredLength, measure.GreedyDesiredLength,
                    measure.LeanLengthList, dynamicConvention);
            }

            return new ArrangeResult(measure.LengthList);
        }

        [Pure]
        private static List<double> ExpandStars(IEnumerable<LengthConvention> conventions, double constraint)
        {
            // Initial.
            var dynamicConvention = conventions.Select(x => x.Clone()).ToList();
            constraint -= dynamicConvention.Where(x => x.Length.IsAbsolute).Sum(x => x.Length.Value);
            var starUnitLength = 0.0;

            // M6/6.
            if (constraint >= 0)
            {
                var starCount = dynamicConvention.Where(x => x.Length.IsStar).Sum(x => x.Length.Value);

                var shouldTestStarMax = true;
                while (shouldTestStarMax)
                {
                    var @fixed = false;
                    starUnitLength = constraint / starCount;
                    foreach (var convention in dynamicConvention.Where(x =>
                        x.Length.IsStar && !double.IsPositiveInfinity(x.MaxLength)))
                    {
                        var (star, max) = (convention.Length.Value, convention.MaxLength);
                        var starLength = star * starUnitLength;
                        if (starLength > max)
                        {
                            convention.Fix(max);
                            starLength = max;
                            constraint -= starLength;
                            starCount -= star;
                            @fixed = true;
                            break;
                        }
                    }

                    shouldTestStarMax = @fixed;
                }
            }

            Debug.Assert(dynamicConvention.All(x => !x.Length.IsAuto));

            var starUnit = starUnitLength;
            var result = dynamicConvention.Select(x =>
            {
                if (x.Length.IsStar)
                {
                    return double.IsInfinity(starUnit) ? double.PositiveInfinity : starUnit * x.Length.Value;
                }

                return x.Length.Value;
            }).ToList();

            return result;
        }

        private static void Clip(IList<double> lengthList, double constraint)
        {
            if (double.IsInfinity(constraint))
            {
                return;
            }
            var measureLength = 0.0;
            for (var i = 0; i < lengthList.Count; i++)
            {
                var length = lengthList[i];
                if (constraint - measureLength > length)
                {
                    measureLength += length;
                }
                else
                {
                    lengthList[i] = constraint - measureLength;
                    measureLength = constraint;
                }
            }
        }

        internal class LengthConvention : ICloneable
        {
            public LengthConvention(GridLength length, double minLength, double maxLength)
            {
                Length = length;
                MinLength = minLength;
                MaxLength = maxLength;
                if (length.IsAbsolute)
                {
                    _isFixed = true;
                }
            }

            internal GridLength Length { get; private set; }
            internal double MinLength { get; }
            internal double MaxLength { get; }

            public void Fix(double pixel)
            {
                if (_isFixed)
                {
                    throw new InvalidOperationException("Cannot fix the length convention if it is fixed.");
                }

                Length = new GridLength(pixel);
                _isFixed = true;
            }

            private bool _isFixed;

            object ICloneable.Clone() => Clone();

            internal LengthConvention Clone() => new LengthConvention(Length, MinLength, MaxLength);
        }

        internal struct AdditionalLengthConvention
        {
            public int Index { get; }
            public int Span { get; }
            public double Min { get; }

            public AdditionalLengthConvention(int index, int span, double min)
            {
                Index = index;
                Span = span;
                Min = min;
            }
        }

        internal class MeasureResult
        {
            internal MeasureResult(double containerLength, double desiredLength, double greedyDesiredLength,
                IReadOnlyList<LengthConvention> leanConventions, IReadOnlyList<double> expandedConventions)
            {
                ContainerLength = containerLength;
                DesiredLength = desiredLength;
                GreedyDesiredLength = greedyDesiredLength;
                LeanLengthList = leanConventions;
                LengthList = expandedConventions;
            }

            public double ContainerLength { get; }
            public double DesiredLength { get; }
            public double GreedyDesiredLength { get; }
            public IReadOnlyList<LengthConvention> LeanLengthList { get; }
            public IReadOnlyList<double> LengthList { get; }
        }

        internal class ArrangeResult
        {
            public ArrangeResult(IReadOnlyList<double> lengthList)
            {
                LengthList = lengthList;
            }

            public IReadOnlyList<double> LengthList { get; }
        }
    }
}
