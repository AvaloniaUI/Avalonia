using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Registering a variable face also registers the families its STAT table composes for its
    /// instances, in every language the font localizes its family name in.
    /// </summary>
    public class InstanceFamilyRegistrationTests
    {
        private const ushort German = 0x0407;

        private static readonly OpenTypeTag s_opsz = OpenTypeTag.Parse("opsz");
        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");

        [Fact]
        public void Localized_Family_Names_Compose_Localized_Instance_Families()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable);

            font.Replace("name", AddGermanNames(font.GetTable("name")));

            var typeface = font.CreateGlyphTypeface();
            var names = new List<string>();

            typeface.GetInstanceFamilyNames(new Dictionary<OpenTypeTag, float> { [s_opsz] = 32 }, names);

            Assert.Equal(new[] { "Inter Variable Display", "Inter Variabel Anzeige" }, names);
        }

        [Fact]
        public void Localized_Instance_Family_Resolves_In_The_Collection()
        {
            using (Start())
            {
                var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable);

                font.Replace("name", AddGermanNames(font.GetTable("name")));

                var root = font.CreateGlyphTypeface();
                var collection = new TestFontCollection();

                Assert.True(collection.TryAddGlyphTypeface(root));

                Assert.Contains("Inter Variabel Anzeige", collection.Select(x => x.Name));
                Assert.True(collection.TryGetGlyphTypeface("Inter Variabel Anzeige", FontStyle.Normal,
                    FontWeight.Bold, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(
                    root.CreateNormalizedPosition(new FontVariationSettings(new[]
                    {
                        new FontVariation(s_opsz, 32), new FontVariation(s_wght, 700)
                    })),
                    glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Instance_Family_Named_Like_The_Font_Is_Not_Registered_Apart()
        {
            using (Start())
            {
                // Without STAT every instance keeps the typographic family "Inter Variable".
                var root = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Remove("STAT")
                    .CreateGlyphTypeface();
                var collection = new TestFontCollection();

                Assert.Empty(FontCollectionBase.GetInstanceFamilies(root));
                Assert.True(collection.TryAddGlyphTypeface(root));

                Assert.False(collection.TryGetInstanceFamilyFaces("Inter Variable", out _));
                Assert.True(collection.TryGetGlyphTypeface("Inter Variable", FontStyle.Normal,
                    FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));
                Assert.Same(root, glyphTypeface);
            }
        }

        [Fact]
        public void Static_Face_Registers_No_Instance_Families()
        {
            using (Start())
            {
                var root = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).CreateGlyphTypeface();
                var collection = new TestFontCollection();

                Assert.True(collection.TryAddGlyphTypeface(root));

                Assert.Equal(new[] { "Inter" }, collection.Select(x => x.Name));
            }
        }

        /// <summary>
        /// Adds German family and optical size names to a <c>name</c> table and writes
        /// it back as a format 0 table.
        /// </summary>
        private static byte[] AddGermanNames(byte[] table)
        {
            var count = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(2));
            var storageOffset = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(4));
            var records = new List<(ushort Platform, ushort Encoding, ushort Language, ushort NameId, byte[] Value)>();

            for (var i = 0; i < count; i++)
            {
                var record = table.AsSpan(6 + i * 12, 12);
                var length = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(8));
                var offset = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(10));

                records.Add((
                    BinaryPrimitives.ReadUInt16BigEndian(record),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(2)),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(4)),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(6)),
                    table.AsSpan(storageOffset + offset, length).ToArray()));
            }

            var displayNameId = records.First(x =>
                x.Platform == 3 && x.Language == 0x0409 && x.NameId >= 256 &&
                Encoding.BigEndianUnicode.GetString(x.Value) == "Display").NameId;

            // The family name the composed family starts from: name ID 16 when the font has it.
            var familyNameId = (ushort)(records.Any(x => x.Platform == 3 && x.NameId == 16) ? 16 : 1);

            records.Add((3, 1, German, familyNameId, Encoding.BigEndianUnicode.GetBytes("Inter Variabel")));
            records.Add((3, 1, German, displayNameId, Encoding.BigEndianUnicode.GetBytes("Anzeige")));

            // Records are sorted by platform, encoding, language and name ID.
            records = records
                .OrderBy(x => x.Platform).ThenBy(x => x.Encoding).ThenBy(x => x.Language).ThenBy(x => x.NameId)
                .ToList();

            var headerLength = 6 + records.Count * 12;
            var storage = new List<byte>();
            var result = new byte[headerLength + records.Sum(x => x.Value.Length)];

            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(0), 0);
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), (ushort)records.Count);
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(4), (ushort)headerLength);

            for (var i = 0; i < records.Count; i++)
            {
                var record = result.AsSpan(6 + i * 12, 12);
                var (platform, encoding, language, nameId, value) = records[i];

                BinaryPrimitives.WriteUInt16BigEndian(record, platform);
                BinaryPrimitives.WriteUInt16BigEndian(record.Slice(2), encoding);
                BinaryPrimitives.WriteUInt16BigEndian(record.Slice(4), language);
                BinaryPrimitives.WriteUInt16BigEndian(record.Slice(6), nameId);
                BinaryPrimitives.WriteUInt16BigEndian(record.Slice(8), (ushort)value.Length);
                BinaryPrimitives.WriteUInt16BigEndian(record.Slice(10), (ushort)storage.Count);

                storage.AddRange(value);
            }

            storage.CopyTo(result, headerLength);

            return result;
        }

        private static IDisposable Start() => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        private sealed class TestFontCollection : FontCollectionBase
        {
            public override Uri Key { get; } = new("fonts:instances", UriKind.Absolute);
        }
    }
}
