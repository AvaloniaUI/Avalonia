using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Flags of a STAT axis value table.
    /// </summary>
    [Flags]
    internal enum StatAxisValueFlags : ushort
    {
        None = 0,

        /// <summary>
        /// The axis value describes other fonts of the family, released before this font had
        /// values for the axis. It is not an attribute of this font.
        /// </summary>
        OlderSiblingFontAttribute = 0x0001,

        /// <summary>
        /// The value name is omitted when names are composed, as "Regular" usually is.
        /// </summary>
        ElidableAxisValueName = 0x0002,
    }

    /// <summary>
    /// A design axis record of the STAT table.
    /// </summary>
    /// <param name="Tag">The axis tag. The axis need not be one of the fvar axes.</param>
    /// <param name="NameId">The <c>name</c> table ID of the axis name.</param>
    /// <param name="Ordering">
    /// The position of the axis's value names when a style name is composed; lower comes first.
    /// </param>
    internal readonly record struct StatDesignAxis(OpenTypeTag Tag, ushort NameId, ushort Ordering);

    /// <summary>
    /// One (design axis, value) pair of a STAT axis value table.
    /// </summary>
    /// <param name="AxisIndex">Zero-based index into <see cref="StatTable.DesignAxes"/>.</param>
    /// <param name="Value">The user-space axis value.</param>
    internal readonly record struct StatAxisValueRecord(int AxisIndex, float Value);

    /// <summary>
    /// An axis value table of the STAT table: a name for a value, or a range of values, of one
    /// design axis (formats 1 to 3), or for a combination of values on several axes (format 4).
    /// </summary>
    internal sealed class StatAxisValue
    {
        internal StatAxisValue(
            int format,
            StatAxisValueFlags flags,
            ushort valueNameId,
            ImmutableArray<StatAxisValueRecord> records,
            float rangeMinValue,
            float rangeMaxValue,
            float? linkedValue)
        {
            Format = format;
            Flags = flags;
            ValueNameId = valueNameId;
            Records = records;
            RangeMinValue = rangeMinValue;
            RangeMaxValue = rangeMaxValue;
            LinkedValue = linkedValue;
        }

        /// <summary>
        /// Gets the table format: 1 (single value), 2 (range), 3 (value with a linked style-link
        /// value) or 4 (a combination of values on several axes).
        /// </summary>
        public int Format { get; }

        public StatAxisValueFlags Flags { get; }

        /// <summary>
        /// Gets the <c>name</c> table ID of the value name, such as "Bold" or "Display".
        /// </summary>
        public ushort ValueNameId { get; }

        /// <summary>
        /// Gets the axis values the table names. Formats 1 to 3 hold one record whose value is the
        /// nominal value; format 4 holds one record per axis of the combination.
        /// </summary>
        public ImmutableArray<StatAxisValueRecord> Records { get; }

        /// <summary>
        /// Gets the smallest value of the named range: the nominal value for formats 1 and 3, NaN for
        /// format 4.
        /// </summary>
        public float RangeMinValue { get; }

        /// <summary>
        /// Gets the largest value of the named range: the nominal value for formats 1 and 3, NaN for
        /// format 4.
        /// </summary>
        public float RangeMaxValue { get; }

        /// <summary>
        /// Gets the style-linked value of a format 3 table, such as Bold (700) for Regular (400);
        /// <c>null</c> for the other formats.
        /// </summary>
        public float? LinkedValue { get; }

        public bool IsElidable => (Flags & StatAxisValueFlags.ElidableAxisValueName) != 0;

        public bool IsOlderSiblingFontAttribute => (Flags & StatAxisValueFlags.OlderSiblingFontAttribute) != 0;
    }

    /// <summary>
    /// Parses the OpenType 'STAT' (style attributes) table: the design axes of a font family and
    /// the names of values on them, from which the names of a variable font's instances are
    /// composed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Versions 1.0, 1.1 (adds the elided fallback name ID) and 1.2 (adds axis value format 4)
    /// are read; later minor versions are read as 1.2. A table whose offsets or counts point
    /// outside the table, or whose axis value records reference a missing design axis, is
    /// rejected as a whole. Axis value tables of an unknown format are skipped.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>.
    /// </para>
    /// </remarks>
    internal sealed class StatTable
    {
        internal const string TableName = "STAT";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const ushort SupportedMajorVersion = 1;

        // Version 1.0 ends before elidedFallbackNameID.
        private const int HeaderSizeV10 = 18;
        private const int HeaderSizeV11 = 20;

        // tag + axisNameID + axisOrdering. Larger records are allowed for forward compatibility.
        private const int MinimumDesignAxisSize = 8;

        // Version 1.0 has no elidedFallbackNameID; name ID 2 names the default style then.
        private const ushort DefaultElidedFallbackNameId = 2;

        private StatTable(
            ImmutableArray<StatDesignAxis> designAxes,
            ImmutableArray<StatAxisValue> axisValues,
            ushort elidedFallbackNameId)
        {
            DesignAxes = designAxes;
            AxisValues = axisValues;
            ElidedFallbackNameId = elidedFallbackNameId;
        }

        /// <summary>
        /// Gets the design axes in table order, which the axis indices of the axis value records
        /// refer to.
        /// </summary>
        public ImmutableArray<StatDesignAxis> DesignAxes { get; }

        /// <summary>
        /// Gets the axis value tables in table order, skipping tables of unknown formats.
        /// </summary>
        public ImmutableArray<StatAxisValue> AxisValues { get; }

        /// <summary>
        /// Gets the <c>name</c> table ID of the style name used when every value name is elided.
        /// </summary>
        public ushort ElidedFallbackNameId { get; }

        public static bool TryLoad(GlyphTypeface glyphTypeface, [NotNullWhen(true)] out StatTable? statTable)
        {
            statTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            statTable = TryParse(data.Span);

            return statTable is not null;
        }

        /// <summary>
        /// Parses STAT table data.
        /// </summary>
        /// <returns>The parsed table, or <c>null</c> for an unsupported version or malformed data.</returns>
        internal static StatTable? TryParse(ReadOnlySpan<byte> span)
        {
            try
            {
                return Parse(span);
            }
            catch (InvalidOperationException)
            {
                // Explicit checks below cover the documented structure; the reader throws for
                // anything they miss, and STAT is optional, so that still only drops the table.
                return null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static StatTable? Parse(ReadOnlySpan<byte> span)
        {
            if (span.Length < HeaderSizeV10)
            {
                return null;
            }

            var reader = new BigEndianBinaryReader(span);

            var majorVersion = reader.ReadUInt16();
            var minorVersion = reader.ReadUInt16();

            if (majorVersion != SupportedMajorVersion)
            {
                return null;
            }

            var designAxisSize = reader.ReadUInt16();
            var designAxisCount = reader.ReadUInt16();
            var designAxesOffset = reader.ReadOffset32();
            var axisValueCount = reader.ReadUInt16();
            var offsetToAxisValueOffsets = reader.ReadOffset32();

            var elidedFallbackNameId = DefaultElidedFallbackNameId;

            if (minorVersion >= 1)
            {
                if (span.Length < HeaderSizeV11)
                {
                    return null;
                }

                elidedFallbackNameId = reader.ReadUInt16();
            }

            var designAxes = ImmutableArray<StatDesignAxis>.Empty;

            if (designAxisCount > 0)
            {
                if (designAxisSize < MinimumDesignAxisSize ||
                    !IsInRange(span, designAxesOffset, (long)designAxisCount * designAxisSize))
                {
                    return null;
                }

                var axesBuilder = ImmutableArray.CreateBuilder<StatDesignAxis>(designAxisCount);

                for (var i = 0; i < designAxisCount; i++)
                {
                    reader.Seek((int)designAxesOffset + i * designAxisSize);

                    var tag = new OpenTypeTag(reader.ReadUInt32());
                    var axisNameId = reader.ReadUInt16();
                    var axisOrdering = reader.ReadUInt16();

                    axesBuilder.Add(new StatDesignAxis(tag, axisNameId, axisOrdering));
                }

                designAxes = axesBuilder.MoveToImmutable();
            }

            var axisValues = ImmutableArray<StatAxisValue>.Empty;

            if (axisValueCount > 0)
            {
                // Axis value table offsets are relative to the start of the offsets array.
                if (!IsInRange(span, offsetToAxisValueOffsets, axisValueCount * 2L))
                {
                    return null;
                }

                var valuesBuilder = ImmutableArray.CreateBuilder<StatAxisValue>(axisValueCount);
                var arrayStart = (int)offsetToAxisValueOffsets;

                for (var i = 0; i < axisValueCount; i++)
                {
                    reader.Seek(arrayStart + i * 2);

                    var tableOffset = (long)arrayStart + reader.ReadOffset16();

                    if (!TryReadAxisValue(span, tableOffset, designAxisCount, out var axisValue, out var isKnownFormat))
                    {
                        if (isKnownFormat)
                        {
                            return null;
                        }

                        continue;
                    }

                    valuesBuilder.Add(axisValue);
                }

                axisValues = valuesBuilder.ToImmutable();
            }

            return new StatTable(designAxes, axisValues, elidedFallbackNameId);
        }

        private static bool TryReadAxisValue(
            ReadOnlySpan<byte> span,
            long offset,
            int designAxisCount,
            [NotNullWhen(true)] out StatAxisValue? axisValue,
            out bool isKnownFormat)
        {
            axisValue = null;
            isKnownFormat = true;

            if (!IsInRange(span, offset, 2))
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(span);
            reader.Seek((int)offset);

            var format = reader.ReadUInt16();

            switch (format)
            {
                case 1:
                case 2:
                case 3:
                {
                    // format, axisIndex, flags, valueNameID, then 1, 3 or 2 Fixed values.
                    var size = format switch { 1 => 12, 2 => 20, _ => 16 };

                    if (!IsInRange(span, offset, size))
                    {
                        return false;
                    }

                    var axisIndex = reader.ReadUInt16();
                    var flags = (StatAxisValueFlags)reader.ReadUInt16();
                    var valueNameId = reader.ReadUInt16();
                    var value = reader.ReadFixed();

                    if (axisIndex >= designAxisCount)
                    {
                        return false;
                    }

                    var rangeMin = value;
                    var rangeMax = value;
                    float? linkedValue = null;

                    if (format == 2)
                    {
                        rangeMin = reader.ReadFixed();
                        rangeMax = reader.ReadFixed();
                    }
                    else if (format == 3)
                    {
                        linkedValue = reader.ReadFixed();
                    }

                    axisValue = new StatAxisValue(
                        format,
                        flags,
                        valueNameId,
                        ImmutableArray.Create(new StatAxisValueRecord(axisIndex, value)),
                        rangeMin,
                        rangeMax,
                        linkedValue);

                    return true;
                }
                case 4:
                {
                    // format, axisCount, flags, valueNameID, then axisCount (axisIndex, Fixed) records.
                    if (!IsInRange(span, offset, 8))
                    {
                        return false;
                    }

                    var axisCount = reader.ReadUInt16();
                    var flags = (StatAxisValueFlags)reader.ReadUInt16();
                    var valueNameId = reader.ReadUInt16();

                    if (axisCount == 0 || !IsInRange(span, offset + 8, axisCount * 6L))
                    {
                        return false;
                    }

                    var records = ImmutableArray.CreateBuilder<StatAxisValueRecord>(axisCount);

                    for (var i = 0; i < axisCount; i++)
                    {
                        var axisIndex = reader.ReadUInt16();
                        var value = reader.ReadFixed();

                        if (axisIndex >= designAxisCount)
                        {
                            return false;
                        }

                        records.Add(new StatAxisValueRecord(axisIndex, value));
                    }

                    axisValue = new StatAxisValue(
                        format,
                        flags,
                        valueNameId,
                        records.MoveToImmutable(),
                        float.NaN,
                        float.NaN,
                        linkedValue: null);

                    return true;
                }
                default:
                    isKnownFormat = false;
                    return false;
            }
        }

        private static bool IsInRange(ReadOnlySpan<byte> span, long offset, long length)
            => offset >= 0 && length >= 0 && offset + length <= span.Length;
    }
}
