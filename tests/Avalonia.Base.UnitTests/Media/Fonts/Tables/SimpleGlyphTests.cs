using System.Collections.Generic;
using Avalonia.Media.Fonts.Tables.Glyf;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Pins the degrade-to-default contract of the simple glyph parser for truncated data: the
    /// counts in a glyph header are untrusted, so a glyph body shorter than they promise must
    /// yield an empty outline instead of throwing out of the parse.
    /// </summary>
    public class SimpleGlyphTests
    {
        [Fact]
        public void Create_Returns_Default_When_Endpoint_Array_Is_Truncated()
        {
            // Four contours promise 8 bytes of endpoints (plus the instruction length field);
            // only 4 bytes are present.
            var glyph = SimpleGlyph.Create(new byte[4], numberOfContours: 4);

            Assert.Equal(0, glyph.EndPtsOfContours.Length);
            Assert.Equal(0, glyph.Flags.Length);
        }

        [Fact]
        public void Create_Returns_Default_When_Instructions_Are_Truncated()
        {
            var data = new List<byte>();
            WriteU16(data, 0);   // endPtsOfContours[0] -> 1 point
            WriteU16(data, 100); // instructionLength promises 100 bytes; none follow

            var glyph = SimpleGlyph.Create(data.ToArray(), numberOfContours: 1);

            Assert.Equal(0, glyph.EndPtsOfContours.Length);
        }

        [Fact]
        public void Create_Returns_Default_When_Flags_Are_Truncated()
        {
            var data = new List<byte>();
            WriteU16(data, 2); // endPtsOfContours[0] -> 3 points
            WriteU16(data, 0); // instructionLength
            data.Add((byte)GlyphFlag.OnCurvePoint); // one flag; two more points need flags

            var glyph = SimpleGlyph.Create(data.ToArray(), numberOfContours: 1);

            Assert.Equal(0, glyph.EndPtsOfContours.Length);
        }

        [Fact]
        public void Create_Returns_Default_When_Coordinates_Are_Truncated()
        {
            var data = new List<byte>();
            WriteU16(data, 0); // endPtsOfContours[0] -> 1 point
            WriteU16(data, 0); // instructionLength
            data.Add((byte)GlyphFlag.XShortVector); // the point promises a 1-byte x delta; none follows

            var glyph = SimpleGlyph.Create(data.ToArray(), numberOfContours: 1);

            Assert.Equal(0, glyph.EndPtsOfContours.Length);
        }

        [Fact]
        public void Create_Parses_A_Minimal_Complete_Glyph()
        {
            var data = new List<byte>();
            WriteU16(data, 1); // endPtsOfContours[0] -> 2 points
            WriteU16(data, 0); // instructionLength
            data.Add((byte)GlyphFlag.OnCurvePoint);
            data.Add((byte)GlyphFlag.OnCurvePoint);
            WriteI16(data, 10); // x deltas
            WriteI16(data, 20);
            WriteI16(data, 30); // y deltas
            WriteI16(data, 40);

            var glyph = SimpleGlyph.Create(data.ToArray(), numberOfContours: 1);

            try
            {
                Assert.Equal(1, glyph.EndPtsOfContours.Length);
                Assert.Equal(2, glyph.Flags.Length);
                Assert.Equal(10, glyph.XCoordinates[0]);
                Assert.Equal(30, glyph.XCoordinates[1]);
                Assert.Equal(30, glyph.YCoordinates[0]);
                Assert.Equal(70, glyph.YCoordinates[1]);
            }
            finally
            {
                glyph.Dispose();
            }
        }

        private static void WriteU16(List<byte> data, ushort value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)(value & 0xFF));
        }

        private static void WriteI16(List<byte> data, short value) => WriteU16(data, (ushort)value);
    }
}
