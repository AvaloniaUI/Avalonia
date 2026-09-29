using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// The family and style name of one instance of a variable font.
    /// </summary>
    /// <param name="FamilyName">
    /// The family the instance belongs to, such as "Inter Variable Display".
    /// </param>
    /// <param name="SubfamilyName">The style within that family, such as "Bold Italic".</param>
    internal readonly record struct FontInstanceNames(string FamilyName, string SubfamilyName);

    /// <summary>
    /// Composes the family and style names of a variable font's instances from its STAT table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This follows the weight/width/slope (WWS) family model the OpenType STAT specification and
    /// DirectWrite use. For every STAT design axis the axis value table describing the instance's
    /// coordinate is selected: format 4 tables first (every record must match; the table with the
    /// most records wins and claims its axes), then per remaining axis a format 1 or 3 value equal
    /// to the coordinate, or a format 2 range containing it (the nearest nominal value wins).
    /// Tables flagged as older sibling font attributes describe other fonts and are ignored. An
    /// axis the font does not vary sits at the value of its first axis value table.
    /// </para>
    /// <para>
    /// The selected names are ordered by the axis ordering of the STAT design axes, and names
    /// flagged elidable are dropped. Names of values on axes other than <c>wght</c>, <c>wdth</c>,
    /// <c>ital</c> and <c>slnt</c> (optical size, custom axes) are appended to the typographic
    /// family name to form the family, the weight, width and slope names form the style. A
    /// format 4 name counts as a style name only when all its axes are among those four. When no
    /// style name remains, the style is the elided fallback name.
    /// </para>
    /// <para>
    /// Without a STAT table, or with one that names no axis values, an instance keeps the
    /// typographic family name and takes its style from the fvar named instance at its
    /// coordinates; a position that is no named instance then has no names.
    /// </para>
    /// </remarks>
    internal static class VariableFontNaming
    {
        // STAT and fvar store values as 16.16 Fixed; this absorbs float rounding of equal values.
        private const float ValueTolerance = 1f / 1024f;

        private const string DefaultStyleName = "Regular";

        /// <summary>
        /// Composes the names of the instance at <paramref name="userCoordinates"/>.
        /// </summary>
        /// <param name="stat">The font's STAT table, or <c>null</c> when it has none.</param>
        /// <param name="axes">The fvar axes of the font.</param>
        /// <param name="instances">The fvar named instances of the font.</param>
        /// <param name="typographicFamilyName">
        /// The family name the composed family starts from: name ID 16, or name ID 1 when the font
        /// has no ID 16.
        /// </param>
        /// <param name="userCoordinates">
        /// User-space coordinates of the instance; axes missing from it are at their default.
        /// </param>
        /// <param name="getName">Resolves a <c>name</c> table ID; <c>null</c> when the ID is missing.</param>
        /// <param name="names">The composed names.</param>
        /// <returns><c>false</c> when the font has no STAT names and the position is no named instance.</returns>
        public static bool TryGetInstanceNames(
            StatTable? stat,
            IReadOnlyList<FontVariationAxis> axes,
            IReadOnlyList<FontVariationInstance> instances,
            string typographicFamilyName,
            IReadOnlyDictionary<OpenTypeTag, float> userCoordinates,
            Func<ushort, string?> getName,
            out FontInstanceNames names)
        {
            if (stat is null || stat.AxisValues.IsEmpty)
            {
                return TryGetNamedInstanceNames(
                    axes, instances, typographicFamilyName, userCoordinates, getName, out names);
            }

            var designAxes = stat.DesignAxes;

            Span<float> coordinates = designAxes.Length <= 16
                ? stackalloc float[designAxes.Length]
                : new float[designAxes.Length];

            for (var i = 0; i < designAxes.Length; i++)
            {
                coordinates[i] = GetCoordinate(stat, i, axes, userCoordinates);
            }

            // One slot per design axis, holding the axis value table that names it.
            var selected = new StatAxisValue?[designAxes.Length];

            SelectCombinations(stat, coordinates, selected);
            SelectSingleValues(stat, coordinates, selected);

            var parts = new List<(int Ordering, int AxisIndex, bool IsStyle, string Name)>(designAxes.Length);

            for (var i = 0; i < selected.Length; i++)
            {
                var value = selected[i];

                // A format 4 table fills the slots of all its axes; take it once, at its first axis.
                if (value is null || value.IsElidable || value.Format == 4 && FirstSlotOf(value, selected) != i)
                {
                    continue;
                }

                var name = getName(value.ValueNameId);

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var ordering = int.MaxValue;
                var isStyle = true;

                foreach (var record in value.Records)
                {
                    ordering = Math.Min(ordering, designAxes[record.AxisIndex].Ordering);
                    isStyle &= IsStyleAxis(designAxes[record.AxisIndex].Tag);
                }

                parts.Add((ordering, i, isStyle, name!));
            }

            parts.Sort((x, y) => x.Ordering != y.Ordering
                ? x.Ordering.CompareTo(y.Ordering)
                : x.AxisIndex.CompareTo(y.AxisIndex));

            var family = new StringBuilder(typographicFamilyName);
            var style = new StringBuilder();

            foreach (var part in parts)
            {
                var builder = part.IsStyle ? style : family;

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(part.Name);
            }

            var subfamilyName = style.Length > 0
                ? style.ToString()
                : getName(stat.ElidedFallbackNameId) is { Length: > 0 } fallback ? fallback : DefaultStyleName;

            names = new FontInstanceNames(family.ToString(), subfamilyName);

            return true;
        }

        private static bool TryGetNamedInstanceNames(
            IReadOnlyList<FontVariationAxis> axes,
            IReadOnlyList<FontVariationInstance> instances,
            string typographicFamilyName,
            IReadOnlyDictionary<OpenTypeTag, float> userCoordinates,
            Func<ushort, string?> getName,
            out FontInstanceNames names)
        {
            foreach (var instance in instances)
            {
                if (!IsAt(instance, axes, userCoordinates))
                {
                    continue;
                }

                var subfamilyName = instance.Name.Length > 0 ? instance.Name : getName(instance.SubfamilyNameId);

                if (string.IsNullOrEmpty(subfamilyName))
                {
                    continue;
                }

                names = new FontInstanceNames(typographicFamilyName, subfamilyName!);

                return true;
            }

            names = default;

            return false;
        }

        private static bool IsAt(
            FontVariationInstance instance,
            IReadOnlyList<FontVariationAxis> axes,
            IReadOnlyDictionary<OpenTypeTag, float> userCoordinates)
        {
            foreach (var axis in axes)
            {
                var requested = userCoordinates.TryGetValue(axis.Tag, out var value) ? value : axis.DefaultValue;
                var declared = instance.Coordinates.TryGetValue(axis.Tag, out var coordinate)
                    ? coordinate
                    : axis.DefaultValue;

                if (!AreEqual(requested, declared))
                {
                    return false;
                }
            }

            return true;
        }

        private static float GetCoordinate(
            StatTable stat,
            int designAxisIndex,
            IReadOnlyList<FontVariationAxis> axes,
            IReadOnlyDictionary<OpenTypeTag, float> userCoordinates)
        {
            var tag = stat.DesignAxes[designAxisIndex].Tag;

            foreach (var axis in axes)
            {
                if (axis.Tag == tag)
                {
                    return userCoordinates.TryGetValue(tag, out var value) ? value : axis.DefaultValue;
                }
            }

            // STAT also describes axes along which the family varies across fonts but this font
            // does not; the font's own value is then the one its table names.
            foreach (var value in stat.AxisValues)
            {
                if (value.Format != 4 && !value.IsOlderSiblingFontAttribute &&
                    value.Records[0].AxisIndex == designAxisIndex)
                {
                    return value.Records[0].Value;
                }
            }

            return float.NaN;
        }

        private static void SelectCombinations(
            StatTable stat,
            ReadOnlySpan<float> coordinates,
            StatAxisValue?[] selected)
        {
            while (true)
            {
                StatAxisValue? best = null;

                foreach (var value in stat.AxisValues)
                {
                    if (value.Format != 4 || value.IsOlderSiblingFontAttribute ||
                        best is not null && value.Records.Length <= best.Records.Length)
                    {
                        continue;
                    }

                    var matches = true;

                    foreach (var record in value.Records)
                    {
                        if (selected[record.AxisIndex] is not null ||
                            !AreEqual(coordinates[record.AxisIndex], record.Value))
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        best = value;
                    }
                }

                if (best is null)
                {
                    return;
                }

                foreach (var record in best.Records)
                {
                    selected[record.AxisIndex] = best;
                }
            }
        }

        private static void SelectSingleValues(
            StatTable stat,
            ReadOnlySpan<float> coordinates,
            StatAxisValue?[] selected)
        {
            for (var axisIndex = 0; axisIndex < selected.Length; axisIndex++)
            {
                if (selected[axisIndex] is not null)
                {
                    continue;
                }

                var coordinate = coordinates[axisIndex];
                StatAxisValue? best = null;
                var bestDistance = float.PositiveInfinity;

                foreach (var value in stat.AxisValues)
                {
                    if (value.Format == 4 || value.IsOlderSiblingFontAttribute ||
                        value.Records[0].AxisIndex != axisIndex)
                    {
                        continue;
                    }

                    var nominal = value.Records[0].Value;
                    var distance = MathF.Abs(nominal - coordinate);

                    var matches = AreEqual(nominal, coordinate) ||
                                  value.Format == 2 &&
                                  coordinate >= value.RangeMinValue - ValueTolerance &&
                                  coordinate <= value.RangeMaxValue + ValueTolerance;

                    if (matches && distance < bestDistance)
                    {
                        best = value;
                        bestDistance = distance;
                    }
                }

                selected[axisIndex] = best;
            }
        }

        private static int FirstSlotOf(StatAxisValue value, StatAxisValue?[] selected)
        {
            for (var i = 0; i < selected.Length; i++)
            {
                if (ReferenceEquals(selected[i], value))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsStyleAxis(OpenTypeTag tag)
            => tag == FvarAxisTags.Weight || tag == FvarAxisTags.Width ||
               tag == FvarAxisTags.Italic || tag == FvarAxisTags.Slant;

        // NaN (an axis without a known value) never matches.
        private static bool AreEqual(float x, float y) => MathF.Abs(x - y) <= ValueTolerance;
    }
}
