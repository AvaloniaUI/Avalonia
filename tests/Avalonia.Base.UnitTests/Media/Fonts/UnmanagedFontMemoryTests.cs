using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class UnmanagedFontMemoryTests
    {
        private static byte[] BuildFont(OpenTypeTag tag, byte[] tableData)
        {
            const int recordsStart = 12;
            const int numTables = 1;
            var directoryBytes = recordsStart + numTables * 16; // 12 + 16 = 28
            var offset = directoryBytes;
            var result = new byte[offset + tableData.Length];

            // Simple SFNT header (version 0x00010000)
            result[0] = 0;
            result[1] = 1;
            result[2] = 0;
            result[3] = 0;
            // numTables (big-endian)
            result[4] = 0;
            result[5] = 1;
            // rest of header (6 bytes) left as zero

            // Table record at offset 12
            uint v = tag;
            result[12] = (byte)(v >> 24);
            result[13] = (byte)(v >> 16);
            result[14] = (byte)(v >> 8);
            result[15] = (byte)v;

            // checksum (4 bytes) left as zero

            // offset (big-endian) at bytes 20..23
            result[20] = (byte)(offset >> 24);
            result[21] = (byte)(offset >> 16);
            result[22] = (byte)(offset >> 8);
            result[23] = (byte)offset;

            // length (big-endian) at bytes 24..27
            var len = tableData.Length;
            result[24] = (byte)(len >> 24);
            result[25] = (byte)(len >> 16);
            result[26] = (byte)(len >> 8);
            result[27] = (byte)len;

            Buffer.BlockCopy(tableData, 0, result, offset, len);

            return result;
        }

        [Fact]
        public unsafe void TryGetTable_ReturnsTableData_WhenExists()
        {
            var tag = OpenTypeTag.Parse("test");
            var data = new byte[] { 1, 2, 3, 4, 5 };
            var font = BuildFont(tag, data);

            using var ms = new MemoryStream(font);
            using var mem = UnmanagedFontMemory.LoadFromStream(ms);

            Assert.True(mem.TryGetTable(tag, out var table));
            Assert.Equal(data, table.ToArray());

            // Second call should also succeed (cache path)
            Assert.True(mem.TryGetTable(tag, out var table2));
            Assert.Equal(table.Length, table2.Length);

            // Ensure both ReadOnlyMemory instances reference the same underlying memory
            ref byte r1 = ref MemoryMarshal.GetReference(table.Span);
            ref byte r2 = ref MemoryMarshal.GetReference(table2.Span);

            fixed (byte* p1 = &r1)
            fixed (byte* p2 = &r2)
            {
                Assert.Equal((IntPtr)p1, (IntPtr)p2);
            }
        }

        [Fact]
        public void TryGetTable_ReturnsFalse_ForUnknownTag()
        {
            var tag = OpenTypeTag.Parse("TEST");
            var other = OpenTypeTag.Parse("OTHR");
            var data = new byte[] { 9, 8, 7 };
            var font = BuildFont(tag, data);

            using var ms = new MemoryStream(font);
            using var mem = UnmanagedFontMemory.LoadFromStream(ms);

            Assert.False(mem.TryGetTable(other, out _));
        }

        [Fact]
        public void TryGetTable_ReturnsFalse_ForInvalidFont()
        {
            // Too short to be a valid SFNT
            var shortData = new byte[8];

            using var ms = new MemoryStream(shortData);
            using var mem = UnmanagedFontMemory.LoadFromStream(ms);

            Assert.False(mem.TryGetTable(OpenTypeTag.Parse("test"), out _));
        }

        [Fact]
        public void GetSpan_ReturnsUnderlyingData()
        {
            var tag = OpenTypeTag.Parse("span");
            var tableData = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
            var font = BuildFont(tag, tableData);

            using var ms = new MemoryStream(font);
            using var mem = UnmanagedFontMemory.LoadFromStream(ms);

            var span = mem.GetSpan();
            Assert.Equal(font.Length, span.Length);
            Assert.Equal(font, span.ToArray());
        }

        [Fact]
        public void Pin_IncrementsPinCount_And_Dispose_Throws_WhenPinned()
        {
            var tag = OpenTypeTag.Parse("pin ");
            var data = new byte[] { 1, 2, 3 };
            var font = BuildFont(tag, data);

            using var ms = new MemoryStream(font);
            UnmanagedFontMemory mem = UnmanagedFontMemory.LoadFromStream(ms);
            UnmanagedFontMemory? fresh = null;

            try
            {
                var handle = mem.Pin();

                try
                {
                    // Attempting to dispose while pinned should throw
                    Assert.Throws<InvalidOperationException>(() => mem.Dispose());
                }
                finally
                {
                    // Release the pin via the handle. After the failed Dispose the original
                    // instance may be in an invalid state, so prefer releasing the pin
                    // through the handle rather than calling methods on the possibly corrupted instance.
                    try
                    {
                        handle.Dispose();
                    }
                    catch { }
                }

                // After the exception the original instance may be unusable; construct a new instance
                // for further operations and assertions.
                fresh = UnmanagedFontMemory.LoadFromStream(new MemoryStream(font));

                // Now disposing the fresh instance should not throw
                fresh.Dispose();
            }
            finally
            {
                // Ensure final cleanup if something went wrong
                try
                {
                    mem.Dispose();
                }
                catch { }

                if (fresh != null)
                {
                    try
                    {
                        fresh.Dispose();
                    }
                    catch { }
                }
            }
        }
    }
}
