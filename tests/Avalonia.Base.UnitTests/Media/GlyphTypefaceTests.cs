using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphTypefaceTests
    {
        private const string InterFontUri = "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";
        private const string BlankFontUri = "resm:Avalonia.Base.UnitTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Base.UnitTests";
        private const string GB18030FontUri = "resm:Avalonia.Base.UnitTests.Assets.NISC18030.ttf?assembly=Avalonia.Base.UnitTests";

        [Fact]
        public void Should_Load_Inter_Font()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.Equal("Inter", typeface.FamilyName);
        }

        [Fact]
        public void Should_Have_CharacterToGlyphMap_For_Common_Characters()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            Assert.NotNull(map);

            Assert.True(map.ContainsGlyph('A'));
            Assert.True(map['A'] != 0);

            Assert.True(map.ContainsGlyph('a'));
            Assert.True(map['a'] != 0);

            Assert.True(map.ContainsGlyph(' '));
            Assert.True(map[' '] != 0);
        }

        [Fact]
        public void GetGlyphAdvance_Should_Return_Advance_For_GlyphId()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            Assert.True(map.ContainsGlyph('A'));

            var glyphId = map['A'];

            // Ensure metrics are available for this glyph
            Assert.True(typeface.TryGetGlyphMetrics(glyphId, out var metrics));

            // Ensure advance can be retrieved
            Assert.True(typeface.TryGetHorizontalGlyphAdvance(glyphId, out var advance));

            // The advance lives on AdvanceWidth; Width is the ink bounding-box width.
            Assert.Equal(metrics.AdvanceWidth, advance);
        }

        [Theory]
        [InlineData(InterFontUri)]
        [InlineData(GB18030FontUri)] // Font without head table
        public void Should_Have_Valid_FontMetrics(string fontUri)
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(fontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var metrics = typeface.Metrics;

            Assert.True(metrics.DesignEmHeight > 0);
            Assert.True(metrics.Ascent != 0);
            Assert.True(metrics.Descent != 0);
            Assert.True(metrics.LineSpacing > 0);
        }

        [Fact]
        public void Should_Have_Positive_GlyphCount()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.True(typeface.GlyphCount > 0);
        }

        [Fact]
        public void Should_Have_Correct_Font_Properties()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.Equal(FontWeight.Normal, typeface.Weight);
            Assert.Equal(FontStyle.Normal, typeface.Style);
            Assert.Equal(FontStretch.Normal, typeface.Stretch);
            Assert.Equal(FontSimulations.None, typeface.FontSimulations);
        }

        [Fact]
        public void Should_Apply_Bold_Simulation()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream), FontSimulations.Bold);

            Assert.Equal(FontWeight.Bold, typeface.Weight);
            Assert.Equal(FontSimulations.Bold, typeface.FontSimulations);
        }

        [Fact]
        public void Should_Apply_Oblique_Simulation()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream), FontSimulations.Oblique);

            Assert.Equal(FontStyle.Italic, typeface.Style);
            Assert.Equal(FontSimulations.Oblique, typeface.FontSimulations);
        }

        [Fact]
        public void Should_Apply_Combined_Simulations()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream), 
                FontSimulations.Bold | FontSimulations.Oblique);

            Assert.Equal(FontWeight.Bold, typeface.Weight);
            Assert.Equal(FontStyle.Italic, typeface.Style);
            Assert.Equal(FontSimulations.Bold | FontSimulations.Oblique, typeface.FontSimulations);
        }

        [Fact]
        public void Should_Have_TypographicFamilyName()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.NotNull(typeface.TypographicFamilyName);
        }

        [Fact]
        public void Should_Have_FamilyNames_Dictionary()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.NotNull(typeface.FamilyNames);
            Assert.NotEmpty(typeface.FamilyNames);
        }

        [Fact]
        public void Should_Have_FaceNames_Dictionary()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.NotNull(typeface.FaceNames);
            Assert.NotEmpty(typeface.FaceNames);
        }

        [Fact]
        public void Should_Have_SupportedFeatures()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var features = typeface.SupportedFeatures;

            Assert.NotEmpty(features);
        }

        [Fact]
        public void Should_Cache_SupportedFeatures()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var features1 = typeface.SupportedFeatures;
            var features2 = typeface.SupportedFeatures;

            Assert.Same(features1, features2);
        }

        [Fact]
        public void TryGetGlyphAdvance_Should_Return_False_For_Invalid_GlyphId()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.False(typeface.TryGetHorizontalGlyphAdvance(ushort.MaxValue, out var advance));
        }

        [Fact]
        public void TryGetGlyphMetrics_Should_Return_False_For_Invalid_GlyphId()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var result = typeface.TryGetGlyphMetrics(ushort.MaxValue, out var metrics);

            Assert.False(result);
            Assert.Equal(default, metrics);
        }

        [Fact]
        public void TryGetGlyphMetrics_Should_Return_Valid_Metrics()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;
            Assert.True(map.ContainsGlyph('A'));

            var glyphId = map['A'];
            var result = typeface.TryGetGlyphMetrics(glyphId, out var metrics);

            Assert.True(result);
            Assert.True(metrics.Width > 0);
        }

        [Fact]
        public void TryGetGlyphMetrics_Width_Is_Ink_Box_Not_Advance()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var glyphId = typeface.CharacterToGlyphMap['A'];

            Assert.True(typeface.TryGetGlyphMetrics(glyphId, out var metrics));
            Assert.True(typeface.TryGetHorizontalGlyphAdvance(glyphId, out var advance));

            // The advance belongs on AdvanceWidth...
            Assert.Equal(advance, metrics.AdvanceWidth);

            // ...and Width is the ink bounding-box width, a distinct value.
            Assert.True(metrics.Width > 0);
            Assert.NotEqual(metrics.AdvanceWidth, metrics.Width);
        }

        [Fact]
        public void TryGetGlyphMetrics_Empty_Glyph_Has_Advance_But_No_Ink()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var spaceGlyph = typeface.CharacterToGlyphMap[' '];

            Assert.True(typeface.TryGetGlyphMetrics(spaceGlyph, out var metrics));

            // The space glyph has a horizontal advance but no ink.
            Assert.True(metrics.AdvanceWidth > 0);
            Assert.Equal((ushort)0, metrics.Width);
            Assert.Equal((ushort)0, metrics.Height);
        }

        [Fact]
        public void TryGetGlyphMetrics_Batch_Matches_Single()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;
            var glyphIds = new ushort[] { map['A'], map['B'], map['g'], map[' '] };

            var batch = new GlyphMetrics[glyphIds.Length];
            Assert.True(typeface.TryGetGlyphMetrics(glyphIds, batch));

            for (var i = 0; i < glyphIds.Length; i++)
            {
                Assert.True(typeface.TryGetGlyphMetrics(glyphIds[i], out var single));

                // GlyphMetrics is a record struct, so this is structural equality.
                Assert.Equal(single, batch[i]);
            }
        }

        [Fact]
        public void Should_Have_Valid_PlatformTypeface()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var platformTypeface = new CustomPlatformTypeface(stream);
            var typeface = new GlyphTypeface(platformTypeface);

            Assert.NotNull(typeface.PlatformTypeface);
            Assert.Same(platformTypeface, typeface.PlatformTypeface);
        }

        [Fact]
        public void Should_Dispose_Properly()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            typeface.Dispose();

            // Should not throw on double dispose
            typeface.Dispose();
        }

        [Fact]
        public void CharacterToGlyphMap_Should_Have_Different_Glyphs_For_Different_Characters()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            Assert.True(map.ContainsGlyph('A'));
            Assert.True(map.ContainsGlyph('B'));

            var glyphA = map['A'];
            var glyphB = map['B'];

            Assert.NotEqual(glyphA, glyphB);
        }

        [Fact]
        public void CharacterToGlyphMap_With_Format13_Should_Have_Same_Glyph_For_Different_Characters()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(BlankFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            Assert.True(map.ContainsGlyph('A'));
            Assert.True(map.ContainsGlyph('B'));

            var glyphA = map['A'];
            var glyphB = map['B'];

            Assert.Equal(glyphA, glyphB);
        }

        [Fact]
        public void FontMetrics_LineSpacing_Should_Be_Calculated_Correctly()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var metrics = typeface.Metrics;

            var expectedLineSpacing = metrics.Descent - metrics.Ascent + metrics.LineGap;

            Assert.Equal(expectedLineSpacing, metrics.LineSpacing);
        }

        [Fact]
        public void Should_Support_Multiple_Characters_In_CharacterToGlyphMap()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            var testCharacters = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            foreach (var ch in testCharacters)
            {
                Assert.True(map.ContainsGlyph(ch), $"Character '{ch}' not found in glyph map");
            }
        }

        [Fact]
        public void AsReadOnlyDictionary_Returns_NonNull_Dictionary()
        {
            var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

            Assert.NotNull(dict);
        }

        [Fact]
        public void AsReadOnlyDictionary_ContainsKey_Matches_Underlying_Map()
        {
            var map = LoadInterCharacterToGlyphMap();
            var dict = map.AsReadOnlyDictionary();

            // 'A' is in Inter.
            Assert.True(map.ContainsGlyph('A'));
            Assert.True(dict.ContainsKey('A'));

            // U+10FFFD is the last code point of the supplementary private-use
            // Plane 16 — Inter does not map it and a Format 4 cmap cannot.
            Assert.False(map.ContainsGlyph(0x10FFFD));
            Assert.False(dict.ContainsKey(0x10FFFD));
        }

        [Theory]
        [InlineData('A')]
        [InlineData('z')]
        [InlineData('0')]
        [InlineData(' ')]
        public void AsReadOnlyDictionary_Indexer_Returns_Same_GlyphId_As_Map(int codePoint)
        {
            var map = LoadInterCharacterToGlyphMap();
            var dict = map.AsReadOnlyDictionary();

            Assert.Equal(map.GetGlyph(codePoint), dict[codePoint]);
        }

        [Fact]
        public void AsReadOnlyDictionary_Indexer_Throws_For_Unmapped_CodePoint()
        {
            var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

            Assert.Throws<KeyNotFoundException>(() => _ = dict[0x10FFFD]);
        }

        [Fact]
        public void AsReadOnlyDictionary_TryGetValue_Returns_True_With_GlyphId_For_Known_CodePoint()
        {
            var map = LoadInterCharacterToGlyphMap();
            var dict = map.AsReadOnlyDictionary();

            Assert.True(dict.TryGetValue('A', out var glyphId));
            Assert.Equal(map.GetGlyph('A'), glyphId);
            Assert.NotEqual(0, glyphId);
        }

        [Fact]
        public void AsReadOnlyDictionary_TryGetValue_Returns_False_For_Unmapped_CodePoint()
        {
            var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

            Assert.False(dict.TryGetValue(0x10FFFD, out var glyphId));
            Assert.Equal((ushort)0, glyphId);
        }

        [Fact]
        public void AsReadOnlyDictionary_Count_Is_Positive_And_Matches_Enumeration()
        {
            var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

            Assert.True(dict.Count > 0);

            var enumerated = 0;
            foreach (var _ in dict)
            {
                enumerated++;
            }

            Assert.Equal(dict.Count, enumerated);
        }

        [Fact]
        public void AsReadOnlyDictionary_Enumeration_Yields_Pairs_That_Round_Trip_Through_The_Map()
        {
            var map = LoadInterCharacterToGlyphMap();
            var dict = map.AsReadOnlyDictionary();

            var checkedPairs = 0;
            foreach (var kvp in dict)
            {
                // Every (key, value) the dictionary yields must agree with the
                // underlying map. The dictionary is a view, not a snapshot.
                Assert.Equal(map.GetGlyph(kvp.Key), kvp.Value);
                Assert.True(map.ContainsGlyph(kvp.Key));

                if (++checkedPairs >= 500)
                {
                    // Inter has thousands of mappings; sampling the first 500
                    // is enough to exercise the enumerator without making the
                    // test prohibitively slow.
                    break;
                }
            }

            Assert.True(checkedPairs > 0);
        }

        [Fact]
        public void AsReadOnlyDictionary_Keys_Match_Dictionary_Enumeration_Keys()
        {
            var dict = LoadInterCharacterToGlyphMap().AsReadOnlyDictionary();

            var keysFromEnumeration = new HashSet<int>();
            var pairsKeysFromEnumeration = new HashSet<int>();

            foreach (var key in dict.Keys)
            {
                keysFromEnumeration.Add(key);
                if (keysFromEnumeration.Count >= 500)
                {
                    break;
                }
            }

            foreach (var kvp in dict)
            {
                pairsKeysFromEnumeration.Add(kvp.Key);
                if (pairsKeysFromEnumeration.Count >= 500)
                {
                    break;
                }
            }

            Assert.True(keysFromEnumeration.Count > 0);
            Assert.Equal(pairsKeysFromEnumeration, keysFromEnumeration);
        }

        [Fact]
        public void AsReadOnlyDictionary_Returns_A_Fresh_View_That_Is_Functionally_Equivalent()
        {
            var map = LoadInterCharacterToGlyphMap();

            var first = map.AsReadOnlyDictionary();
            var second = map.AsReadOnlyDictionary();

            // The dictionary is a lightweight wrapper that may or may not be
            // the same instance; what matters is that two views of the same
            // map agree on lookups.
            Assert.True(first.ContainsKey('A'));
            Assert.True(second.ContainsKey('A'));
            Assert.Equal(first['A'], second['A']);
        }

        private static Avalonia.Media.Fonts.Tables.Cmap.CharacterToGlyphMap LoadInterCharacterToGlyphMap()
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(InterFontUri));
            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));
            return typeface.CharacterToGlyphMap;
        }

        [Fact]
        public void FamilyNames_Should_Contain_InvariantCulture_Entry()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.True(typeface.FamilyNames.ContainsKey(CultureInfo.InvariantCulture) || 
                       typeface.FamilyNames.Count > 0);
        }

        [Fact]
        public void FaceNames_Should_Contain_InvariantCulture_Entry()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.True(typeface.FaceNames.ContainsKey(CultureInfo.InvariantCulture) || 
                       typeface.FaceNames.Count > 0);
        }

        private class CustomPlatformTypeface : IPlatformTypeface
        {
            private readonly UnmanagedFontMemory _fontMemory;

            public CustomPlatformTypeface(Stream stream, string fontFamily = "Custom")
            {
                _fontMemory = UnmanagedFontMemory.LoadFromStream(stream);
                FamilyName = fontFamily;
            }

            public FontWeight Weight => FontWeight.Normal;

            public FontStyle Style => FontStyle.Normal;

            public FontStretch Stretch => FontStretch.Normal;

            public string FamilyName { get; }

            public FontSimulations FontSimulations => FontSimulations.None;

            public void Dispose()
            {
                ((IDisposable)_fontMemory).Dispose();
            }

            public unsafe bool TryGetStream([NotNullWhen(true)] out Stream stream)
            {
                var memory = _fontMemory.Memory;

                var handle = memory.Pin();
                stream = new PinnedUnmanagedMemoryStream(handle, memory.Length);

                return true;
            }

            private sealed class PinnedUnmanagedMemoryStream : UnmanagedMemoryStream
            {
                private MemoryHandle _handle;

                public unsafe PinnedUnmanagedMemoryStream(MemoryHandle handle, long length)
                    : base((byte*)handle.Pointer, length)
                {
                    _handle = handle;
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        base.Dispose(disposing);
                    }
                    finally
                    {
                        _handle.Dispose();
                    }
                }
            }

            public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table) => _fontMemory.TryGetTable(tag, out table);
        }
    }
}
