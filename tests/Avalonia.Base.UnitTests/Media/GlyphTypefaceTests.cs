using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphTypefaceTests
    {
        private static string s_InterFontUri = "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        [Fact]
        public void Should_Load_Inter_Font()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            Assert.Equal("Inter", typeface.FamilyName);
        }

        [Fact]
        public void Should_Have_CharacterToGlyphMap_For_Common_Characters()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            Assert.NotNull(map);

            Assert.True(map.ContainsKey('A'));
            Assert.True(map['A'] != 0);

            Assert.True(map.ContainsKey('a'));
            Assert.True(map['a'] != 0);

            Assert.True(map.ContainsKey(' '));
            Assert.True(map[' '] != 0);
        }

        [Fact]
        public void GetGlyphAdvance_Should_Return_Advance_For_GlyphId()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var map = typeface.CharacterToGlyphMap;

            Assert.True(map.ContainsKey('A'));

            var glyphId = map['A'];

            // Ensure metrics are available for this glyph
            Assert.True(typeface.TryGetGlyphMetrics(glyphId, out var metrics));

            var advance = typeface.GetGlyphAdvance(glyphId);

            // Advance returned by GetGlyphAdvance should match the metrics width
            Assert.Equal(metrics.Width, advance);
        }

        private class CustomPlatformTypeface : IPlatformTypeface
        {
            private readonly UnmanagedFontMemory _fontMemory;

            public CustomPlatformTypeface(Stream stream)
            {
                _fontMemory = UnmanagedFontMemory.LoadFromStream(stream);
            }

            public FontWeight Weight => FontWeight.Normal;

            public FontStyle Style => FontStyle.Normal;

            public FontStretch Stretch => FontStretch.Normal;

            public void Dispose()
            {
                _fontMemory.Dispose();
            }

            public unsafe bool TryGetStream([NotNullWhen(true)] out Stream stream)
            {
                var memory = _fontMemory.Memory;

                var handle = memory.Pin(); // MemoryHandle merken
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
