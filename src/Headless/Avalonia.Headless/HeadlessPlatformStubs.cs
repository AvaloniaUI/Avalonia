using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    internal class HeadlessClipboardStub : IClipboard
    {
        private string? _text;
        private IDataObject? _data;

        public Task<string?> GetTextAsync()
        {
            return Task.Run(() => _text);
        }

        public Task SetTextAsync(string? text)
        {
            return Task.Run(() => _text = text);
        }

        public Task ClearAsync()
        {
            return Task.Run(() => _text = null);
        }

        public Task SetDataObjectAsync(IDataObject data)
        {
            return Task.Run(() => _data = data);
        }

        public Task<string[]> GetFormatsAsync()
        {
            return Task.Run(() =>
            {
                if (_data is not null)
                {
                    return _data.GetDataFormats().ToArray();
                }

                if (_text is not null)
                {
                    return new[] { DataFormats.Text };
                }

                return Array.Empty<string>();
            });
        }

        public async Task<object?> GetDataAsync(string format)
        {
            return await Task.Run(() => _data);
        }
    }

    internal class HeadlessCursorFactoryStub : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub();
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new CursorStub();

        private class CursorStub : ICursorImpl
        {
            public void Dispose() { }
        }
    }

    internal class HeadlessGlyphTypefaceImpl : IGlyphTypeface
    {
        public FontMetrics Metrics => new FontMetrics
        {
            DesignEmHeight = 10,
            Ascent = 2,
            Descent = 10,
            IsFixedPitch = true,
            LineGap = 0,
            UnderlinePosition = 2,
            UnderlineThickness = 1,
            StrikethroughPosition = 2,
            StrikethroughThickness = 1
        };

        public int GlyphCount => 1337;

        public FontSimulations FontSimulations => FontSimulations.None;

        public string FamilyName => "$Default";

        public FontWeight Weight => FontWeight.Normal;

        public FontStyle Style => FontStyle.Normal;

        public FontStretch Stretch => FontStretch.Normal;

        public void Dispose()
        {
        }

        public ushort GetGlyph(uint codepoint)
        {
            return (ushort)codepoint;
        }

        public bool TryGetGlyph(uint codepoint, out ushort glyph)
        {
            glyph = 8;

            return true;
        }

        public int GetGlyphAdvance(ushort glyph)
        {
            return 8;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var advances = new int[glyphs.Length];

            for (var i = 0; i < advances.Length; i++)
            {
                advances[i] = 8;
            }

            return advances;
        }

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            return codepoints.ToArray().Select(x => (ushort)x).ToArray();
        }

        public bool TryGetTable(uint tag, out byte[] table)
        {
            table = null!;
            return false;
        }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = new GlyphMetrics
            {
                Width = 10,
                Height = 10
            };

            return true;
        }
    }

    internal class HeadlessTextShaperStub : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;
            var shapedBuffer = new ShapedBuffer(text, text.Length, typeface, fontRenderingEmSize, bidiLevel);
            var textSpan = text.Span;
            var textStartIndex = TextTestHelper.GetStartCharIndex(text);

            for (var i = 0; i < shapedBuffer.Length;)
            {
                var glyphCluster = i + textStartIndex;

                var codepoint = Codepoint.ReadAt(textSpan, i, out var count);

                var glyphIndex = typeface.GetGlyph(codepoint);

                for (var j = 0; j < count; ++j)
                {
                    shapedBuffer[i + j] = new GlyphInfo(glyphIndex, glyphCluster, 10);
                }

                i += count;
            }

            return shapedBuffer;
        }
    }

    internal class HeadlessFontManagerStub : IFontManagerImpl
    {
        private readonly string _defaultFamilyName;

        public HeadlessFontManagerStub(string defaultFamilyName = "Default")
        {
            _defaultFamilyName = defaultFamilyName;
        }

        public int TryCreateGlyphTypefaceCount { get; private set; }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
        {
            return new[] { _defaultFamilyName };
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch,
            CultureInfo? culture, out Typeface fontKey)
        {
            fontKey = new Typeface(_defaultFamilyName);

            return false;
        }

        public virtual bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, 
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            TryCreateGlyphTypefaceCount++;

            if (familyName == "Unknown")
            {
                return false;
            }

            glyphTypeface = new HeadlessGlyphTypefaceImpl();

            return true;
        }

        public virtual bool TryCreateGlyphTypeface(Stream stream, out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = new HeadlessGlyphTypefaceImpl();

            return true;
        }
    }

    internal class HeadlessIconLoaderStub : IPlatformIconLoader
    {
        private class IconStub : IWindowIconImpl
        {
            public void Save(Stream outputStream)
            {

            }
        }
        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new IconStub();
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new IconStub();
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            return new IconStub();
        }
    }

    internal class HeadlessScreensStub : IScreenImpl
    {
        public int ScreenCount { get; } = 1;

        public IReadOnlyList<Screen> AllScreens { get; } = new[]
        {
            new Screen(1, new PixelRect(0, 0, 1920, 1280),
                new PixelRect(0, 0, 1920, 1280), true),
        };

        public Screen? ScreenFromPoint(PixelPoint point)
        {
            return ScreenHelper.ScreenFromPoint(point, AllScreens);
        }

        public Screen? ScreenFromRect(PixelRect rect)
        {
            return ScreenHelper.ScreenFromRect(rect, AllScreens);
        }

        public Screen? ScreenFromWindow(IWindowBaseImpl window)
        {
            return ScreenHelper.ScreenFromWindow(window, AllScreens);
        }
    }
    
    internal static class TextTestHelper
    {
        public static int GetStartCharIndex(ReadOnlyMemory<char> text)
        {
            if (!MemoryMarshal.TryGetString(text, out _, out var start, out _))
                throw new InvalidOperationException("text memory should have been a string");
            return start;
        }
    }
}
