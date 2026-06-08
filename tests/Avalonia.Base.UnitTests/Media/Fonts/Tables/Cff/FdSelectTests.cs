using System;
using System.Collections.Generic;
using Avalonia.Media.Fonts.Tables.Cff;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// Exercises FDSelect glyph → Font DICT mapping (CID-keyed CFF) on hand-built blobs — both the
    /// flat per-glyph array (format 0) and the sorted ranges with binary search (format 3).
    /// </summary>
    public class FdSelectTests
    {
        [Fact]
        public void Format0_Maps_Each_Glyph_To_Its_Fd()
        {
            // format 0, fds: glyph 0->0, 1->1, 2->1, 3->2, 4->0
            byte[] blob = { 0, 0, 1, 1, 2, 0 };
            var fdSelect = FdSelect.Parse(blob, 0, glyphCount: 5);

            Assert.NotNull(fdSelect);
            Assert.Equal(0, fdSelect!.GetFd(0));
            Assert.Equal(1, fdSelect.GetFd(1));
            Assert.Equal(1, fdSelect.GetFd(2));
            Assert.Equal(2, fdSelect.GetFd(3));
            Assert.Equal(0, fdSelect.GetFd(4));
        }

        [Fact]
        public void Format0_Out_Of_Range_Glyph_Returns_Zero()
        {
            byte[] blob = { 0, 3, 3 };
            var fdSelect = FdSelect.Parse(blob, 0, glyphCount: 2);

            Assert.Equal(0, fdSelect!.GetFd(99));
        }

        [Fact]
        public void Format3_Maps_Ranges_Via_Binary_Search()
        {
            // Ranges: [0,3) -> fd 0 ; [3,6) -> fd 1 ; [6,10) -> fd 2 ; sentinel 10.
            var blob = BuildFormat3(new (int First, byte Fd)[] { (0, 0), (3, 1), (6, 2) }, sentinel: 10);
            var fdSelect = FdSelect.Parse(blob, 0, glyphCount: 10);

            Assert.NotNull(fdSelect);

            // Boundaries on both sides of each range edge.
            Assert.Equal(0, fdSelect!.GetFd(0));
            Assert.Equal(0, fdSelect.GetFd(2));
            Assert.Equal(1, fdSelect.GetFd(3));
            Assert.Equal(1, fdSelect.GetFd(5));
            Assert.Equal(2, fdSelect.GetFd(6));
            Assert.Equal(2, fdSelect.GetFd(9));

            // At / past the sentinel — no range contains it.
            Assert.Equal(0, fdSelect.GetFd(10));
        }

        [Fact]
        public void Format3_Honours_A_Nonzero_Offset()
        {
            // Prepend padding so Parse must respect the offset rather than assuming 0.
            var inner = BuildFormat3(new (int First, byte Fd)[] { (0, 5), (4, 7) }, sentinel: 8);
            var blob = new byte[3 + inner.Length];
            Array.Copy(inner, 0, blob, 3, inner.Length);

            var fdSelect = FdSelect.Parse(blob, 3, glyphCount: 8);

            Assert.Equal(5, fdSelect!.GetFd(0));
            Assert.Equal(5, fdSelect.GetFd(3));
            Assert.Equal(7, fdSelect.GetFd(4));
            Assert.Equal(7, fdSelect.GetFd(7));
        }

        private static byte[] BuildFormat3((int First, byte Fd)[] ranges, int sentinel)
        {
            var blob = new List<byte> { 3 };
            WriteU16(blob, ranges.Length);

            foreach (var (first, fd) in ranges)
            {
                WriteU16(blob, first);
                blob.Add(fd);
            }

            WriteU16(blob, sentinel);
            return blob.ToArray();
        }

        private static void WriteU16(List<byte> blob, int value)
        {
            blob.Add((byte)(value >> 8));
            blob.Add((byte)(value & 0xFF));
        }
    }
}
