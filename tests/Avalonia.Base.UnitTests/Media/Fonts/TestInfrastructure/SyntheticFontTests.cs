using System.IO;
using System.Linq;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.TestInfrastructure
{
    /// <summary>
    /// Self-tests for the <see cref="SyntheticFont"/> / <see cref="BigEndianBuffer"/> test
    /// infrastructure. These must stay green: they prove the harness produces fonts the real
    /// pipeline accepts and that its mutations behave as documented, so that font-robustness
    /// tests built on top can attribute failures to the code under test, not the harness.
    /// </summary>
    public class SyntheticFontTests
    {
        [Fact]
        public void FromAsset_Produces_A_Loadable_Typeface()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

            var typeface = font.TryCreateGlyphTypeface();

            Assert.NotNull(typeface);
            Assert.True(typeface!.GlyphCount > 0);
            Assert.True(typeface.CharacterToGlyphMap.ContainsGlyph('A'));
        }

        [Fact]
        public void FromAsset_Parses_The_Core_Sfnt_Tables()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

            // A static TrueType font carries at least these.
            Assert.True(font.Contains("head"));
            Assert.True(font.Contains("maxp"));
            Assert.True(font.Contains("cmap"));
            Assert.True(font.Contains("hhea"));
            Assert.True(font.Contains("hmtx"));
            Assert.True(font.Contains("glyf"));
            Assert.True(font.Contains("loca"));
        }

        [Fact]
        public void Variable_Font_Carries_The_Variation_Tables()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable);

            Assert.True(font.Contains("fvar"));
            Assert.True(font.Contains("HVAR"));
            Assert.True(font.Contains("gvar"));
        }

        [Fact]
        public void Truncate_Shortens_The_Table()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);
            var originalLength = font.TableLength("post");

            font.Truncate("post", 4);

            Assert.Equal(4, font.TableLength("post"));
            Assert.True(originalLength > 4);
        }

        [Fact]
        public void Remove_Drops_The_Table()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

            Assert.True(font.Contains("post"));

            font.Remove("post");

            Assert.False(font.Contains("post"));
        }

        [Fact]
        public void PatchUInt16_Writes_Big_Endian_At_The_Given_Offset()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

            // head.unitsPerEm lives at offset 18.
            font.PatchUInt16("head", 18, 0x0801);

            var head = font.GetTable("head");
            Assert.Equal(0x08, head[18]);
            Assert.Equal(0x01, head[19]);
        }

        [Fact]
        public void ToPlatformTypeface_Snapshots_The_Tables_At_Call_Time()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

            var headTag = OpenTypeTag.Parse("head");
            var beforeMutation = font.GetTable("head");

            var platformTypeface = font.ToPlatformTypeface();

            // Mutate the source after taking the snapshot.
            font.PatchUInt16("head", 18, 0x1234);

            Assert.True(platformTypeface.TryGetTable(headTag, out var snapshot));

            // The snapshot reflects the pre-mutation bytes; the source reflects the mutation.
            Assert.Equal(beforeMutation, snapshot.ToArray());
            Assert.NotEqual(beforeMutation, font.GetTable("head"));
        }

        [Fact]
        public void ToBytes_RoundTrips_Through_The_Real_Sfnt_Parser()
        {
            // ToBytes() must emit a directory the production UnmanagedFontMemory parser
            // accepts. CustomPlatformTypeface wraps UnmanagedFontMemory.LoadFromStream.
            var rebuilt = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).ToBytes();

            var platformTypeface = new CustomPlatformTypeface(new MemoryStream(rebuilt));
            var typeface = GlyphTypeface.TryCreate(platformTypeface);

            Assert.NotNull(typeface);
            Assert.True(typeface!.GlyphCount > 0);
            Assert.True(typeface.CharacterToGlyphMap.ContainsGlyph('A'));
        }

        [Fact]
        public void ToBytes_RoundTrips_Through_FromBytes_Preserving_Tables()
        {
            var original = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

            var reparsed = SyntheticFont.FromBytes(original.ToBytes());

            Assert.Equal(
                original.Tags.Select(t => (uint)t).OrderBy(t => t),
                reparsed.Tags.Select(t => (uint)t).OrderBy(t => t));

            // Table contents survive the round-trip byte-for-byte.
            Assert.Equal(original.GetTable("maxp"), reparsed.GetTable("maxp"));
        }

        [Fact]
        public void BigEndianBuffer_Writes_Big_Endian_And_Patches_Reserved_Offsets()
        {
            var buffer = new BigEndianBuffer();

            buffer.UInt16(0x1234);
            var offsetPos = buffer.ReserveOffset32();
            buffer.UInt8(0xAB);
            buffer.PatchUInt32(offsetPos, 0xDEADBEEF);

            Assert.Equal(
                new byte[] { 0x12, 0x34, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB },
                buffer.ToArray());
        }

        [Fact]
        public void BigEndianBuffer_F2Dot14_And_Tag_Encode_Correctly()
        {
            var buffer = new BigEndianBuffer();

            buffer.F2Dot14(1.0);  // 1.0 == 0x4000 in F2DOT14
            buffer.Tag("head");

            var bytes = buffer.ToArray();

            Assert.Equal(new byte[] { 0x40, 0x00 }, bytes[..2]);
            Assert.Equal(new byte[] { (byte)'h', (byte)'e', (byte)'a', (byte)'d' }, bytes[2..6]);
        }
    }
}
