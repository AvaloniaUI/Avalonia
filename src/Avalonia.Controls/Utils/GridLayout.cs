using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Layout;
using JetBrains.Annotations;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Contains algorithms that can help to measure and arrange a Grid.
    /// </summary>
    internal class GridLayout
    {
        /// <summary>
        /// Initialize a new <see cref="GridLayout"/> instance from the column definitions.
        /// <see cref="GridLayout"/> will forget that the layout data comes from the columns.
        /// </summary>
        internal GridLayout([NotNull] ColumnDefinitions columns)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            _conventions = columns.Count == 0
                ? new List<LengthConvention> { new LengthConvention() }
                : columns.Select(x => new LengthConvention(x.Width, x.MinWidth, x.MaxWidth)).ToList();
        }

        /// <summary>
        /// Initialize a new <see cref="GridLayout"/> instance from the row definitions.
        /// <see cref="GridLayout"/> will forget that the layout data comes from the rows.
        /// </summary>
        internal GridLayout([NotNull] RowDefinitions rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            _conventions = rows.Count == 0
                ? new List<LengthConvention> { new LengthConvention() }
                : rows.Select(x => new LengthConvention(x.Height, x.MinHeight, x.MaxHeight)).ToList();
        }

        /// <summary>
        /// Gets the layout tolerance. If any length offset is less than this value, we will treat them the same.
        /// </summary>
        private const double LayoutTolerance = 1.0 / 256.0;

        /// <summary>
        /// Gets all the length conventions that come from column/row definitions.
        /// These conventions provide limitations of each grid cell.
        /// </summary>
        [NotNull]
        private readonly List<LengthConvention> _conventions;

        /// <summary>
        /// Gets all the length conventions that come from the grid children.
        /// </summary>
        [NotNull]
        private readonly List<AdditionalLengthConvention> _additionalConventions =
            new List<AdditionalLengthConvention>();

        /// <summary>
        /// Some elements are not only in a single grid cell, they have one or more column/row spans,
        /// and these elements may affect the grid layout especially the measuring procedure.<para/>
        /// Append these elements into the convention list can help to layout them correctly through
        /// their desired size. Only a small subset of grid children need to be measured before layout
        /// starts and they will be called via the<paramref name="getDesiredLength"/> callback.
        /// </summary>
        /// <typeparam name="T">The grid children type.</typeparam>
        /// <param name="source">
        /// Contains the safe column/row index and its span.
        /// Notice that we will not verify whether the range is in the column/row count,
        /// so you should get the safe column/row info first.
        /// </param>
        /// <param name="getDesiredLength">
        /// This callback will be called if the <see cref="GridLayout"/> thinks that a child should be
        /// measured first. Usually, these are the children that have the * or Auto length.
        /// </param>
        internal void AppendMeasureConventions<T>([NotNull] IDictionary<T, (int index, int span)> source,
            [NotNull] Func<T, double> getDesiredLength)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (getDesiredLength == null) throw new ArgumentNullException(nameof(getDesiredLength));

            // M1/7. Find all the Auto and * length columns/rows.
            // Only these columns/rows' layout can be affected by the children desired size.
            // 
            // Find all columns/rows that has the length Auto or *. We'll measure the children in advance.
            // Only these kind of columns/rows will affects the Grid layout.
            // Please note:
            // - The columns/rows of Auto length will definitely be affected by the children size;
            // - However, only the Grid.DesiredSize can be affected by the *-length columns/rows unless the Grid has very much space (Infinitely).

            //               +-----------------------------------------------------------+
            //               |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            //               +-----------------------------------------------------------+
            // _conventions: | min | max |     |           | min |     |  min max  | max |
            // _additionalC:                   |<-   desired   ->|     |< desired >|
            // _additionalC:       |< desired >|           |<-        desired          ->|

            // 寻找所有行列范围中包含 Auto 和 * 的元素，使用全部可用尺寸提前测量。
            // 因为只有这部分元素的布局才会被 Grid 的子元素尺寸影响。
            // 请注意：
            // - Auto 长度的行列必定会受到子元素布局影响，会影响到行列的布局长度；
            // - 而对于 * 长度，一般只有 Grid.DesiredSize 会受到子元素布局影响，只有在 Grid 布局空间充足时行列长度才会被子元素布局影响。

            // Find all the Auto and * length columns/rows.
            var found = new Dictionary<T, (int index, int span)>();
            for (var i = 0; i < _conventions.Count; i++)
            {
                var index = i;
                var convention = _conventions[index];
                if (convention.Length.IsAuto || convention.Length.IsStar)
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

        /// <summary>
        /// Run measure procedure according to the <paramref name="containerLength"/> and gets the <see cref="MeasureResult"/>.
        /// </summary>
        /// <param name="containerLength">
        /// The container length. Usually, it is the constraint of the <see cref="Layoutable.MeasureOverride"/> method.
        /// </param>
        /// <returns>
        /// The measured result that containing the desired size and all the column/row length.
        /// </returns>
        [NotNull, Pure]
        internal MeasureResult Measure(double containerLength)
        {
            // Prepare all the variables that this method needs to use.
            var conventions = _conventions.Select(x => x.Clone()).ToList();
            var starCount = conventions.Where(x => x.Length.IsStar).Sum(x => x.Length.Value);
            var aggregatedLength = 0.0;
            double starUnitLength;

            // M2/7. Aggregate all the pixel lengths. Then we can get the rest length by `containerLength - aggregatedLength`.
            // We mark the aggregated length as "fix" because we can completely determine their values. Same as below.
            //
            // +-----------------------------------------------------------+
            // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            //                   |#fix#|           |#fix#|
            //
            // 将全部的固定像素长度的行列长度累加。这样，containerLength - aggregatedLength 便能得到剩余长度。
            // 我们会将所有能够确定下长度的行列标记为 fix。下同。
            // 请注意：
            // - 我们并没有直接从 containerLength 一直减下去，而是使用 aggregatedLength 进行累加，是因为无穷大相减得到的是 NaN，不利于后续计算。

            aggregatedLength += conventions.Where(x => x.Length.IsAbsolute).Sum(x => x.Length.Value);

            // M3/7. Fix all the * lengths that have reached the minimum.
            //
            // +-----------------------------------------------------------+
            // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            // | min | max |     |           | min |     |  min max  | max |
            //                   | fix |     |#fix#| fix |

            var shouldTestStarMin = true;
            while (shouldTestStarMin)
            {
                // Calculate the unit * length to estimate the length of each column/row that has * length.
                // Under this estimated length, look for if there is a minimum value that has a length less than its constraint.
                // If there is such a *, then fix the size of this cell, and then loop it again until there is no * that can be constrained by the minimum value.
                //
                // 计算单位 * 的长度，以便预估出每一个 * 行列的长度。
                // 在此预估的长度下，从前往后寻找是否存在某个 * 长度已经小于其约束的最小值。
                // 如果发现存在这样的 *，那么将此单元格的尺寸固定下来（Fix），然后循环重来，直至再也没有能被最小值约束的 *。
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

            // M4/7. Fix all the Auto lengths that the children on its column/row have a zero or non-zero length.
            //
            // +-----------------------------------------------------------+
            // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            // | min | max |     |           | min |     |  min max  | max |
            //       |#fix#|     | fix |#fix#| fix | fix |

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

                    var more = ApplyAdditionalConventionsForAuto(conventions, i, starUnitLength);
                    convention.Fix(more);
                    aggregatedLength += more;
                    @fixed = true;
                    break;
                }

                shouldTestAuto = @fixed;
            }

            // M5/7. Expand the stars according to the additional conventions (usually the child desired length).
            // We can't fix this kind of length, so we just mark them as desired (des).
            //
            // +-----------------------------------------------------------+
            // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            // | min | max |     |           | min |     |  min max  | max |
            // |#des#| fix |#des#| fix | fix | fix | fix |   #des#   |#des#|

            var desiredStarMin = AggregateAdditionalConventionsForStars(conventions);
            aggregatedLength += desiredStarMin;

            // M6/7. Determine the desired length of the grid for current container length. Its value stores in desiredLength.
            // Assume if the container has infinite length, the grid desired length is stored in greedyDesiredLength.
            //
            // +-----------------------------------------------------------+
            // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            // | min | max |     |           | min |     |  min max  | max |
            // |#des#| fix |#des#| fix | fix | fix | fix |   #des#   |#des#|
            // Note: This table will be stored as the intermediate result into the MeasureResult and it will be reused by Arrange procedure.
            // 
            // desiredLength = Math.Max(0.0, des + fix + des + fix + fix + fix + fix + des + des)
            // greedyDesiredLength = des + fix + des + fix + fix + fix + fix + des + des

            var desiredLength = containerLength - aggregatedLength >= 0.0 ? aggregatedLength : containerLength;
            var greedyDesiredLength = aggregatedLength;

            // M7/7. Expand all the rest stars. These stars have no conventions or only have
            // max value they can be expanded from zero to constraint.
            //
            // +-----------------------------------------------------------+
            // |  *  |  A  |  *  |  P  |  A  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            // | min | max |     |           | min |     |  min max  | max |
            // |#fix#| fix |#fix#| fix | fix | fix | fix |   #fix#   |#fix#|
            // Note: This table will be stored as the final result into the MeasureResult.

            var dynamicConvention = ExpandStars(conventions, containerLength);
            Clip(dynamicConvention, containerLength);

            // Returns the measuring result.
            return new MeasureResult(containerLength, desiredLength, greedyDesiredLength,
                conventions, dynamicConvention);
        }

        /// <summary>
        /// Run arrange procedure according to the <paramref name="measure"/> and gets the <see cref="ArrangeResult"/>.
        /// </summary>
        /// <param name="finalLength">
        /// The container length. Usually, it is the finalSize of the <see cref="Layoutable.ArrangeOverride"/> method.
        /// </param>
        /// <param name="measure">
        /// The result that the measuring procedure returns. If it is null, a new measure procedure will run.
        /// </param>
        /// <returns>
        /// The measured result that containing the desired size and all the column/row length.
        /// </returns>
        [NotNull, Pure]
        public ArrangeResult Arrange(double finalLength, [CanBeNull] MeasureResult measure)
        {
            measure = measure ?? Measure(finalLength);

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

        /// <summary>
        /// Use the <see cref="_additionalConventions"/> to calculate the fixed length of the Auto column/row.
        /// </summary>
        /// <param name="conventions">The convention list that has same length fixed.</param>
        /// <param name="index">The column/row index that should be fixed.</param>
        /// <param name="starUnitLength">The unit * length for the current rest length.</param>
        /// <returns>The final length of the Auto length column/row.</returns>
        [Pure]
        private double ApplyAdditionalConventionsForAuto(IReadOnlyList<LengthConvention> conventions,
            int index, double starUnitLength)
        {
            // 1. Calculate all the * length with starUnitLength.
            // 2. Exclude all the fixed length and all the * length.
            // 3. Compare the rest of the desired length and the convention.
            // +-----------------+
            // |  *  |  A  |  *  |
            // +-----------------+
            // | exl |     | exl |
            // |< desired >|
            //       |< desired >|

            var more = 0.0;
            foreach (var additional in _additionalConventions)
            {
                // If the additional conventions contains the Auto column/row, try to determine the Auto column/row length.
                if (additional.Index <= index && index < additional.Index + additional.Span)
                {
                    var min = Enumerable.Range(additional.Index, additional.Span)
                        .Select(x =>
                        {
                            var c = conventions[x];
                            if (c.Length.IsAbsolute) return c.Length.Value;
                            if (c.Length.IsStar) return c.Length.Value * starUnitLength;
                            return 0.0;
                        }).Sum();
                    more = Math.Max(additional.Min - min, more);
                }
            }

            return Math.Min(conventions[index].MaxLength, more);
        }

        /// <summary>
        /// Calculate the total desired length of all the * length.
        /// Bug Warning:
        /// - The behavior of this method is undefined! Different UI Frameworks have different behaviors.
        /// - We ignore all the span columns/rows and just take single cells into consideration.
        /// </summary>
        /// <param name="conventions">All the conventions that have almost been fixed except the rest *.</param>
        /// <returns>The total desired length of all the * length.</returns>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double AggregateAdditionalConventionsForStars(
            IReadOnlyList<LengthConvention> conventions)
        {
            // Note: The comment in this method is just a draft.
            // +-----------------------------------------------------------+
            // |  *  |  P  |  *  |  P  |  P  |  *  |  P  |     *     |  *  |
            // +-----------------------------------------------------------+
            // |<-      x      ->|                 |<-         z         ->|
            //       |<-         y         ->|
            // conveniences 是上面看到的那个列表，所有能够确定的 A、P 和 * 都已经转换成了 P；剩下的 * 只有 Max 是没确定的。
            // _additionalConventions 是上面的 x、y、z…… 集合，只有最小值是可用的。
            // 需要返回所有标记为 * 的方格的累加和的最小值。

            // Before we determine the behavior of this method, we just aggregate the one-span * columns.
            return _additionalConventions
                .Where(x => x.Span == 1 && conventions[x.Index].Length.IsStar)
                .Sum(x => x.Min);
        }

        /// <summary>
        /// This method implement the last procedure (M7/7) of measure.
        /// It expand all the * length to the fixed length according to the <paramref name="constraint"/>.
        /// </summary>
        /// <param name="conventions">All the conventions that have almost been fixed except the rest *.</param>
        /// <param name="constraint">The container length.</param>
        /// <returns>The final pixel length list.</returns>
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

        /// <summary>
        /// If the container length is not infinity. It may be not enough to contain all the columns/rows.
        /// We should clip the columns/rows that have been out of the container bounds.
        /// Note: This method may change the items value of <paramref name="lengthList"/>.
        /// </summary>
        /// <param name="lengthList">All the column width list or the row height list with fixed pixel length.</param>
        /// <param name="constraint">the container length. It can be positive infinity.</param>
        private static void Clip([NotNull] IList<double> lengthList, double constraint)
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

        /// <summary>
        /// Contains the convention of each column/row.
        /// This is mostly the same as <see cref="RowDefinition"/> or <see cref="ColumnDefinition"/>.
        /// We use this because we can treat the column and the row the same.
        /// </summary>
        internal class LengthConvention : ICloneable
        {
            /// <summary>
            /// Initialize a new instance of <see cref="LengthConvention"/>.
            /// </summary>
            public LengthConvention()
            {
                Length = new GridLength(1.0, GridUnitType.Star);
                MinLength = 0.0;
                MaxLength = double.PositiveInfinity;
            }

            /// <summary>
            /// Initialize a new instance of <see cref="LengthConvention"/>.
            /// </summary>
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

            /// <summary>
            /// Gets the <see cref="GridLength"/> of a column or a row.
            /// </summary>
            internal GridLength Length { get; private set; }

            /// <summary>
            /// Gets the minimum convention for a column or a row.
            /// </summary>
            internal double MinLength { get; }

            /// <summary>
            /// Gets the maximum convention for a column or a row.
            /// </summary>
            internal double MaxLength { get; }

            /// <summary>
            /// Fix the <see cref="LengthConvention"/>. If all columns/rows are fixed,
            /// we can get the double pixel list of all columns/row.
            /// </summary>
            /// <param name="pixel">
            /// The pixel length that should be used to fix the convention.
            /// </param>
            /// <exception cref="InvalidOperationException">
            /// If the convention is pixel length, this exception will throw.
            /// </exception>
            public void Fix(double pixel)
            {
                if (_isFixed)
                {
                    throw new InvalidOperationException("Cannot fix the length convention if it is fixed.");
                }

                Length = new GridLength(pixel);
                _isFixed = true;
            }

            /// <summary>
            /// Gets a value that indicates whether this convention is fixed.
            /// </summary>
            private bool _isFixed;

            /// <inheritdoc />
            object ICloneable.Clone() => Clone();

            /// <summary>
            /// Get a deep copy of this convention list.
            /// We need this because we want to store some intermediate states.
            /// </summary>
            internal LengthConvention Clone() => new LengthConvention(Length, MinLength, MaxLength);
        }

        /// <summary>
        /// Contains the convention that comes from the grid children.
        /// Some child span multiple columns or rows, so even a simple column/row can have multiple conventions.
        /// </summary>
        internal struct AdditionalLengthConvention
        {
            /// <summary>
            /// Initialize a new instance of <see cref="AdditionalLengthConvention"/>.
            /// </summary>
            public AdditionalLengthConvention(int index, int span, double min)
            {
                Index = index;
                Span = span;
                Min = min;
            }

            /// <summary>
            /// Gets the start index of this additional convention.
            /// </summary>
            public int Index { get; }

            /// <summary>
            /// Gets the span of this additional convention.
            /// </summary>
            public int Span { get; }

            /// <summary>
            /// Gets the minimum length of this additional convention.
            /// This value is usually provided by the child's desired length.
            /// </summary>
            public double Min { get; }
        }

        /// <summary>
        /// Stores the result of the measuring procedure.
        /// This result can be used to measure children and assign the desired size.
        /// Passing this result to <see cref="Arrange"/> can reduce calculation.
        /// </summary>
        [DebuggerDisplay("{" + nameof(LengthList) + ",nq}")]
        internal class MeasureResult
        {
            /// <summary>
            /// Initialize a new instance of <see cref="MeasureResult"/>.
            /// </summary>
            internal MeasureResult(double containerLength, double desiredLength, double greedyDesiredLength,
                IReadOnlyList<LengthConvention> leanConventions, IReadOnlyList<double> expandedConventions)
            {
                ContainerLength = containerLength;
                DesiredLength = desiredLength;
                GreedyDesiredLength = greedyDesiredLength;
                LeanLengthList = leanConventions;
                LengthList = expandedConventions;
            }

            /// <summary>
            /// Gets the container length for this result.
            /// This property exists because a measure result is related to it.
            /// </summary>
            public double ContainerLength { get; }

            /// <summary>
            /// Gets the desired length of this result.
            /// Just return this value as the desired size in <see cref="Layoutable.MeasureOverride"/>.
            /// </summary>
            public double DesiredLength { get; }

            /// <summary>
            /// Gets the desired length if the container has infinite length.
            /// </summary>
            public double GreedyDesiredLength { get; }

            /// <summary>
            /// Contains the column/row calculation intermediate result.
            /// This value is used by <see cref="Arrange"/> for reducing repeat calculation.
            /// </summary>
            public IReadOnlyList<LengthConvention> LeanLengthList { get; }

            /// <summary>
            /// Gets the length list for each column/row.
            /// </summary>
            public IReadOnlyList<double> LengthList { get; }
        }

        /// <summary>
        /// Stores the result of the measuring procedure.
        /// This result can be used to arrange children and assign the render size.
        /// </summary>
        [DebuggerDisplay("{" + nameof(LengthList) + ",nq}")]
        internal class ArrangeResult
        {
            /// <summary>
            /// Initialize a new instance of <see cref="ArrangeResult"/>.
            /// </summary>
            internal ArrangeResult(IReadOnlyList<double> lengthList)
            {
                LengthList = lengthList;
            }

            /// <summary>
            /// Gets the length list for each column/row.
            /// </summary>
            public IReadOnlyList<double> LengthList { get; }
        }
    }
}
