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
    public class OS2TableTests
    {
        private static string s_InterFontUri = "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        [Fact]
        public void Should_Load_OS2Table_From_Inter_Font()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
        }

        [Fact]
        public void OS2Table_Should_Have_Valid_WeightClass()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(400, os2Table.WeightClass);
        }

        [Fact]
        public void OS2Table_Should_Have_Valid_WidthClass()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(5, os2Table.WidthClass);
        }

        [Fact]
        public void OS2Table_Should_Have_Valid_TypoMetrics()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(2728, os2Table.TypoAscender);
            Assert.Equal(-680, os2Table.TypoDescender);
            Assert.True(os2Table.TypoAscender > os2Table.TypoDescender);
        }

        [Fact]
        public void OS2Table_Should_Have_Valid_WinMetrics()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(2728, os2Table.WinAscent);
            Assert.Equal(680, os2Table.WinDescent);
        }

        [Fact]
        public void OS2Table_Should_Have_Valid_StrikeoutMetrics()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(192, os2Table.StrikeoutSize);
        }

        [Fact]
        public void OS2Table_Inter_Regular_Should_Be_Regular()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.True(os2Table.Selection.HasFlag(OS2Table.FontSelectionFlags.REGULAR));
        }

        [Fact]
        public void OS2Table_Should_Have_Consistent_Ascent_Values()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(2728, os2Table.TypoAscender);
            Assert.Equal(2728, os2Table.WinAscent);
        }

        [Fact]
        public void OS2Table_Should_Have_Consistent_Descent_Values()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(-680, os2Table.TypoDescender);
            Assert.Equal(680, os2Table.WinDescent);
        }

        [Fact]
        public void OS2Table_Should_Have_Valid_Panose()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

            var loaded = OS2Table.TryLoad(typeface, out var os2Table);

            Assert.True(loaded);
            var panose = os2Table.Panose;
            Assert.Equal(PanoseFamilyKind.LatinText, panose.FamilyKind);
        }

        [Fact]
        public void TryLoad_From_Span_Should_Parse_WeightClass_And_Selection_Of_Inter_Font()
        {
            var assetLoader = new StandardAssetLoader();

            using var stream = assetLoader.Open(new Uri(s_InterFontUri));

            using var platformTypeface = new CustomPlatformTypeface(stream);

            Assert.True(platformTypeface.TryGetTable(OS2Table.Tag, out var table));

            var loaded = OS2Table.TryLoad(table.Span, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(400, os2Table.WeightClass);
            Assert.True(os2Table.Selection.HasFlag(OS2Table.FontSelectionFlags.REGULAR));
        }

        [Fact]
        public void TryLoad_From_Span_Should_Parse_WeightClass_And_Selection()
        {
            var data = CreateTable(0, 78, 700, OS2Table.FontSelectionFlags.BOLD | OS2Table.FontSelectionFlags.ITALIC);

            var loaded = OS2Table.TryLoad(data, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(0, os2Table.Version);
            Assert.Equal(700, os2Table.WeightClass);
            Assert.Equal(OS2Table.FontSelectionFlags.BOLD | OS2Table.FontSelectionFlags.ITALIC, os2Table.Selection);
        }

        [Theory]
        [InlineData(0, 78)]
        [InlineData(1, 86)]
        [InlineData(2, 96)]
        [InlineData(4, 96)]
        [InlineData(5, 100)]
        public void TryLoad_From_Span_Should_Accept_Table_Of_Minimum_Length_For_Its_Version(ushort version, int length)
        {
            var data = CreateTable(version, length, 400, OS2Table.FontSelectionFlags.REGULAR);

            var loaded = OS2Table.TryLoad(data, out var os2Table);

            Assert.True(loaded);
            Assert.Equal(version, os2Table.Version);
            Assert.Equal(400, os2Table.WeightClass);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 64)]
        [InlineData(0, 77)]
        [InlineData(1, 85)]
        [InlineData(2, 95)]
        [InlineData(4, 95)]
        [InlineData(5, 99)]
        public void TryLoad_From_Span_Should_Return_False_For_Truncated_Table(ushort version, int length)
        {
            var data = CreateTable(version, length, 400, OS2Table.FontSelectionFlags.REGULAR);

            var loaded = OS2Table.TryLoad(data, out _);

            Assert.False(loaded);
        }

        private static byte[] CreateTable(ushort version, int length, ushort weightClass,
            OS2Table.FontSelectionFlags selection)
        {
            var data = new byte[length];

            WriteUInt16(data, 0, version);
            WriteUInt16(data, 4, weightClass);
            WriteUInt16(data, 62, (ushort)selection);

            return data;
        }

        // Writes only the bytes that fit, so a truncated table keeps the leading fields it still holds.
        private static void WriteUInt16(byte[] data, int offset, ushort value)
        {
            if (offset < data.Length)
            {
                data[offset] = (byte)(value >> 8);
            }

            if (offset + 1 < data.Length)
            {
                data[offset + 1] = (byte)value;
            }
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
