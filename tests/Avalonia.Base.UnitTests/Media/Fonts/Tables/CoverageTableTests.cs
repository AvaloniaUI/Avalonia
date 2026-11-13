using System;
using Avalonia.Media.Fonts.Tables;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class CoverageTableTests
    {
        private static byte[] Format1(params int[] glyphs)
        {
            var buffer = new BigEndianBuffer().UInt16(1).UInt16(glyphs.Length);

            foreach (var glyph in glyphs)
            {
                buffer.UInt16(glyph);
            }

            return buffer.ToArray();
        }

        private static byte[] Format2(params (int Start, int End, int StartIndex)[] ranges)
        {
            var buffer = new BigEndianBuffer().UInt16(2).UInt16(ranges.Length);

            foreach (var (start, end, startIndex) in ranges)
            {
                buffer.UInt16(start).UInt16(end).UInt16(startIndex);
            }

            return buffer.ToArray();
        }

        private static CoverageTable Load(byte[] data)
        {
            Assert.True(CoverageTable.TryLoad(data, 0, out var coverage));

            return coverage!;
        }

        [Fact]
        public void Format1_Maps_Glyphs_To_Their_Position()
        {
            var coverage = Load(Format1(3, 7, 20, 65535));

            Assert.Equal(4, coverage.GlyphCount);
            Assert.Equal(0, coverage.GetCoverageIndex(3));
            Assert.Equal(1, coverage.GetCoverageIndex(7));
            Assert.Equal(2, coverage.GetCoverageIndex(20));
            Assert.Equal(3, coverage.GetCoverageIndex(65535));
        }

        [Fact]
        public void Format1_Misses_Uncovered_Glyphs()
        {
            var coverage = Load(Format1(3, 7, 20));

            Assert.Equal(-1, coverage.GetCoverageIndex(0));
            Assert.Equal(-1, coverage.GetCoverageIndex(5));
            Assert.Equal(-1, coverage.GetCoverageIndex(21));
        }

        [Fact]
        public void Format2_Maps_Glyphs_From_The_Range_Start_Index()
        {
            var coverage = Load(Format2((10, 12, 0), (20, 20, 3), (30, 39, 4)));

            Assert.Equal(14, coverage.GlyphCount);
            Assert.Equal(0, coverage.GetCoverageIndex(10));
            Assert.Equal(2, coverage.GetCoverageIndex(12));
            Assert.Equal(3, coverage.GetCoverageIndex(20));
            Assert.Equal(4, coverage.GetCoverageIndex(30));
            Assert.Equal(13, coverage.GetCoverageIndex(39));
        }

        [Fact]
        public void Format2_Misses_Glyphs_Between_And_Outside_Ranges()
        {
            var coverage = Load(Format2((10, 12, 0), (20, 20, 3)));

            Assert.Equal(-1, coverage.GetCoverageIndex(9));
            Assert.Equal(-1, coverage.GetCoverageIndex(13));
            Assert.Equal(-1, coverage.GetCoverageIndex(19));
            Assert.Equal(-1, coverage.GetCoverageIndex(21));
        }

        [Fact]
        public void Empty_Tables_Cover_Nothing()
        {
            Assert.Equal(-1, Load(Format1()).GetCoverageIndex(0));
            Assert.Equal(-1, Load(Format2()).GetCoverageIndex(0));
        }

        [Fact]
        public void Table_Is_Read_At_Its_Offset_In_The_Parent()
        {
            var data = new BigEndianBuffer().Zeros(6).Bytes(Format1(5, 9)).ToArray();

            Assert.True(CoverageTable.TryLoad(data, 6, out var coverage));
            Assert.Equal(1, coverage!.GetCoverageIndex(9));
        }

        [Fact]
        public void Unsorted_Format1_Glyphs_Are_Rejected()
        {
            Assert.False(CoverageTable.TryLoad(Format1(7, 3), 0, out _));
            Assert.False(CoverageTable.TryLoad(Format1(3, 3), 0, out _));
        }

        [Fact]
        public void Unsorted_Overlapping_Or_Inverted_Ranges_Are_Rejected()
        {
            Assert.False(CoverageTable.TryLoad(Format2((20, 25, 0), (10, 12, 6)), 0, out _));
            Assert.False(CoverageTable.TryLoad(Format2((10, 20, 0), (20, 25, 11)), 0, out _));
            Assert.False(CoverageTable.TryLoad(Format2((12, 10, 0)), 0, out _));
        }

        [Fact]
        public void Unknown_Format_Is_Rejected()
        {
            Assert.False(CoverageTable.TryLoad(new BigEndianBuffer().UInt16(3).UInt16(0).ToArray(), 0, out _));
        }

        [Fact]
        public void Truncated_Tables_Are_Rejected()
        {
            var format1 = Format1(3, 7, 20);
            var format2 = Format2((10, 12, 0), (20, 20, 3));

            Assert.False(CoverageTable.TryLoad(format1.AsMemory(0, format1.Length - 1), 0, out _));
            Assert.False(CoverageTable.TryLoad(format2.AsMemory(0, format2.Length - 1), 0, out _));
            Assert.False(CoverageTable.TryLoad(format1.AsMemory(0, 3), 0, out _));
        }

        [Fact]
        public void Offset_Outside_The_Parent_Is_Rejected()
        {
            var data = Format1(3);

            Assert.False(CoverageTable.TryLoad(data, data.Length - 3, out _));
            Assert.False(CoverageTable.TryLoad(data, data.Length + 10, out _));
            Assert.False(CoverageTable.TryLoad(data, -1, out _));
        }
    }
}
