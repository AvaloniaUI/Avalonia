using System;
using System.Buffers.Binary;
using System.Text;
using Avalonia.Media.Fonts.Tables;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class MetaTableTests
    {
        [Fact]
        public void TryParse_Empty_Span_Returns_False()
        {
            Assert.False(MetaTable.TryParse(ReadOnlySpan<byte>.Empty, out _));
        }

        [Fact]
        public void TryParse_Wrong_Version_Returns_False()
        {
            var bytes = new byte[16];
            // version 2 is not supported
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 2);

            Assert.False(MetaTable.TryParse(bytes, out _));
        }

        [Fact]
        public void TryParse_Reads_Dlng_And_Slng_Tags()
        {
            const string dlng = "ja,zh-Hant";
            const string slng = "en";

            var bytes = BuildMetaTable(dlng, slng);

            Assert.True(MetaTable.TryParse(bytes, out var table));
            Assert.Equal(new[] { "ja", "zh-Hant" }, table.DesignLanguages);
            Assert.Equal(new[] { "en" }, table.SupportedLanguages);
        }

        [Fact]
        public void TryParse_Trims_Whitespace_And_Skips_Empty_Tags()
        {
            var bytes = BuildMetaTable(" ja , , zh-Hant ", "en-US");

            Assert.True(MetaTable.TryParse(bytes, out var table));
            Assert.Equal(new[] { "ja", "zh-Hant" }, table.DesignLanguages);
            Assert.Equal(new[] { "en-US" }, table.SupportedLanguages);
        }

        [Fact]
        public void TryParse_Returns_Empty_Arrays_When_Only_Unknown_Maps()
        {
            // Build a meta table with a single data map using an unknown tag.
            var unknownPayload = Encoding.UTF8.GetBytes("ignored");
            const int header = 16;
            const int mapSize = 12;
            var dataOffset = header + mapSize;
            var bytes = new byte[dataOffset + unknownPayload.Length];

            // Header
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 1); // version
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0); // flags
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 0); // reserved
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), 1); // dataMapsCount

            // Data map
            WriteTag(bytes.AsSpan(16, 4), "xxxx");
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint)dataOffset);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(24, 4), (uint)unknownPayload.Length);

            unknownPayload.CopyTo(bytes.AsSpan(dataOffset));

            Assert.True(MetaTable.TryParse(bytes, out var table));
            Assert.Empty(table.DesignLanguages);
            Assert.Empty(table.SupportedLanguages);
        }

        private static byte[] BuildMetaTable(string dlngValue, string slngValue)
        {
            var dlngBytes = Encoding.UTF8.GetBytes(dlngValue);
            var slngBytes = Encoding.UTF8.GetBytes(slngValue);

            const int header = 16;
            const int mapSize = 12;
            const int mapCount = 2;
            var dlngOffset = header + (mapSize * mapCount);
            var slngOffset = dlngOffset + dlngBytes.Length;
            var totalLength = slngOffset + slngBytes.Length;

            var bytes = new byte[totalLength];

            // Header
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 1); // version
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0); // flags
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 0); // reserved
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), mapCount); // dataMapsCount

            // dlng data map
            WriteTag(bytes.AsSpan(16, 4), "dlng");
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint)dlngOffset);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(24, 4), (uint)dlngBytes.Length);

            // slng data map
            WriteTag(bytes.AsSpan(28, 4), "slng");
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(32, 4), (uint)slngOffset);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(36, 4), (uint)slngBytes.Length);

            // Payloads
            dlngBytes.CopyTo(bytes.AsSpan(dlngOffset));
            slngBytes.CopyTo(bytes.AsSpan(slngOffset));

            return bytes;
        }

        private static void WriteTag(Span<byte> destination, string tag)
        {
            Encoding.ASCII.GetBytes(tag, destination);
        }
    }
}
