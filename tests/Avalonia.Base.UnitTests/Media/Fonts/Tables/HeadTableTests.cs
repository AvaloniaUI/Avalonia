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

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.NotNull(headTable);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Version()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal((ushort)1, headTable.Value.Version.Major);
            Assert.Equal((ushort)0, headTable.Value.Version.Minor);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_MagicNumber()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(0x5F0F3CF5u, headTable.Value.MagicNumber);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_UnitsPerEm()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(2816, headTable.Value.UnitsPerEm);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_BoundingBox()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(-2080, headTable.Value.XMin);
            Assert.Equal(7274, headTable.Value.XMax);
            Assert.Equal(-900, headTable.Value.YMin);
            Assert.Equal(3072, headTable.Value.YMax);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_IndexToLocFormat()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(IndexToLocFormat.Long, headTable.Value.IndexToLocFormat);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_GlyphDataFormat()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(GlyphDataFormat.Current, headTable.Value.GlyphDataFormat);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_LowestRecPPEM()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(6, headTable.Value.LowestRecPPEM);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_FontRevision()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.True(headTable.Value.FontRevision.ToFloat() > 0);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Created_Timestamp()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.True(headTable.Value.Created > new DateTime(1904, 1, 1));
            Assert.True(headTable.Value.Created < DateTime.UtcNow);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Modified_Timestamp()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.True(headTable.Value.Modified > new DateTime(1904, 1, 1));
            Assert.True(headTable.Value.Modified < DateTime.UtcNow);
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_Flags()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.True(headTable.Value.Flags.HasFlag(HeadFlags.BaselineAtY0));
        }

        [Fact]
        public void HeadTable_Should_Have_Valid_FontDirectionHint()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var success = HeadTable.TryLoad(typeface, out var headTable);

            Assert.True(success);
            Assert.Equal(FontDirectionHint.LeftToRightWithNeutrals, headTable.Value.FontDirectionHint);
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
