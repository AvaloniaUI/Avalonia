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
