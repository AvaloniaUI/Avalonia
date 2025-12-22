using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class MaxpTableTests
    {
        private static string s_InterFontUri = "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        [Fact]
        public void Should_Load_MaxpTable_From_Inter_Font()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.NotEqual(default, maxpTable);
        }

        [Fact]
        public void MaxpTable_Should_Have_Valid_NumGlyphs()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(2547, maxpTable.NumGlyphs);
        }

        [Fact]
        public void MaxpTable_TrueType_Should_Have_Version_1_0()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(1, maxpTable.Version.Major);
            Assert.Equal(0, maxpTable.Version.Minor);
        }

        [Fact]
        public void MaxpTable_Version_1_0_Should_Have_Valid_MaxPoints()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(148, maxpTable.MaxPoints);
        }

        [Fact]
        public void MaxpTable_Version_1_0_Should_Have_Valid_MaxContours()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(12, maxpTable.MaxContours);
        }

        [Fact]
        public void MaxpTable_Version_1_0_Should_Have_Valid_MaxZones()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(1, maxpTable.MaxZones);
        }

        [Fact]
        public void MaxpTable_Should_Have_Valid_MaxCompositePoints()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(112, maxpTable.MaxCompositePoints);
        }

        [Fact]
        public void MaxpTable_Should_Have_Valid_MaxCompositeContours()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(7, maxpTable.MaxCompositeContours);
        }

        [Fact]
        public void MaxpTable_Should_Have_Valid_MaxStackElements()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(0, maxpTable.MaxStackElements);
        }

        [Fact]
        public void MaxpTable_Should_Have_Valid_MaxComponentDepth()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(1, maxpTable.MaxComponentDepth);
        }

        [Fact]
        public void MaxpTable_NumGlyphs_Should_Match_GlyphTypeface_GlyphCount()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var maxpTable = MaxpTable.Load(typeface);

            Assert.Equal(maxpTable.NumGlyphs, typeface.GlyphCount);
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
