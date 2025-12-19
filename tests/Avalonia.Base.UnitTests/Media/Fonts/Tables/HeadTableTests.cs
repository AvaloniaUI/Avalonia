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
    public class HeadTableTests
    {
        private static string s_InterFontUri = "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        [Fact]
        public void Should_Load_HeadTable_From_Inter_Font()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.NotNull(headTable);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Version()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.Equal((ushort)1, headTable.MajorVersion);
            Assert.Equal((ushort)0, headTable.MinorVersion);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_MagicNumber()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.Equal(0x5F0F3CF5u, headTable.MagicNumber);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_UnitsPerEm()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.True(headTable.UnitsPerEm >= 16);
            Assert.True(headTable.UnitsPerEm <= 16384);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_BoundingBox()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.True(headTable.XMin <= headTable.XMax);
            Assert.True(headTable.YMin <= headTable.YMax);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_IndexToLocFormat()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.True(headTable.IndexToLocFormat == 0 || headTable.IndexToLocFormat == 1);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_GlyphDataFormat()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.Equal((short)0, headTable.GlyphDataFormat);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_LowestRecPPEM()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.True(headTable.LowestRecPPEM > 0);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_FontRevision()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.True(headTable.FontRevision > 0);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Created_Timestamp()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.NotEqual(0, headTable.Created);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Modified_Timestamp()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var headTable = HeadTable.Load(typeface);

            Assert.NotEqual(0, headTable.Modified);
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
                _fontMemory.Dispose();
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
