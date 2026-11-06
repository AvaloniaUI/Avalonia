using System.Collections.Generic;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Builds STAT table data in memory: the header, then the design axis records, then the axis
    /// value offsets array, then the axis value tables.
    /// </summary>
    internal sealed class StatTableBuilder
    {
        private readonly List<(string Tag, int NameId, int Ordering)> _axes = new();
        private readonly List<byte[]> _values = new();

        public int MinorVersion { get; set; } = 2;

        public int DesignAxisSize { get; set; } = 8;

        public int ElidedFallbackNameId { get; set; } = 2;

        public StatTableBuilder Axis(string tag, int nameId, int ordering)
        {
            _axes.Add((tag, nameId, ordering));
            return this;
        }

        public StatTableBuilder Value(byte[] axisValueTable)
        {
            _values.Add(axisValueTable);
            return this;
        }

        public StatTableBuilder Format1(int axisIndex, int flags, int nameId, double value)
            => Value(new BigEndianBuffer()
                .UInt16(1).UInt16(axisIndex).UInt16(flags).UInt16(nameId).Fixed(value)
                .ToArray());

        public StatTableBuilder Format2(
            int axisIndex, int flags, int nameId, double nominal, double rangeMin, double rangeMax)
            => Value(new BigEndianBuffer()
                .UInt16(2).UInt16(axisIndex).UInt16(flags).UInt16(nameId)
                .Fixed(nominal).Fixed(rangeMin).Fixed(rangeMax)
                .ToArray());

        public StatTableBuilder Format3(int axisIndex, int flags, int nameId, double value, double linkedValue)
            => Value(new BigEndianBuffer()
                .UInt16(3).UInt16(axisIndex).UInt16(flags).UInt16(nameId).Fixed(value).Fixed(linkedValue)
                .ToArray());

        public StatTableBuilder Format4(int flags, int nameId, params (int AxisIndex, double Value)[] records)
        {
            var buffer = new BigEndianBuffer().UInt16(4).UInt16(records.Length).UInt16(flags).UInt16(nameId);

            foreach (var (axisIndex, value) in records)
            {
                buffer.UInt16(axisIndex).Fixed(value);
            }

            return Value(buffer.ToArray());
        }

        public byte[] Build()
        {
            var buffer = new BigEndianBuffer();

            buffer.UInt16(1).UInt16(MinorVersion).UInt16(DesignAxisSize).UInt16(_axes.Count);
            var designAxesOffset = buffer.ReserveOffset32();
            buffer.UInt16(_values.Count);
            var axisValueOffsetsOffset = buffer.ReserveOffset32();

            if (MinorVersion >= 1)
            {
                buffer.UInt16(ElidedFallbackNameId);
            }

            buffer.PatchUInt32(designAxesOffset, (uint)buffer.Position);

            foreach (var (tag, nameId, ordering) in _axes)
            {
                buffer.Tag(tag).UInt16(nameId).UInt16(ordering).Zeros(DesignAxisSize - 8);
            }

            var arrayStart = buffer.Position;
            buffer.PatchUInt32(axisValueOffsetsOffset, (uint)arrayStart);

            var offsets = new int[_values.Count];

            for (var i = 0; i < _values.Count; i++)
            {
                offsets[i] = buffer.ReserveOffset16();
            }

            for (var i = 0; i < _values.Count; i++)
            {
                buffer.PatchUInt16(offsets[i], buffer.Position - arrayStart);
                buffer.Bytes(_values[i]);
            }

            return buffer.ToArray();
        }
    }
}
