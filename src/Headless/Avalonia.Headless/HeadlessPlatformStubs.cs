using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    class HeadlessClipboardStub : IClipboard
    {
        private string _text;
        private IDataObject _data;

        public Task<string> GetTextAsync()
        {
            return Task.Run(() => _text);
        }

        public Task SetTextAsync(string text)
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
            throw new NotImplementedException();
        }

        public async Task<object> GetDataAsync(string format)
        {
            return await Task.Run(() => _data);
        }
    }

    class HeadlessCursorFactoryStub : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub();
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new CursorStub();

        private class CursorStub : ICursorImpl
        {
            public void Dispose() { }
        }
    }

    class HeadlessGlyphTypefaceImpl : IGlyphTypeface
    {
        public FontMetrics Metrics => new FontMetrics
        {
            DesignEmHeight = 1,
            Ascent = 8,
            Descent = 4,
            LineGap = 0,
            UnderlinePosition = 2,
            UnderlineThickness = 1,
            StrikethroughPosition = 2,
            StrikethroughThickness = 1,
            IsFixedPitch = true
        };

        public int GlyphCount => 1337;

        public FontSimulations FontSimulations { get; }

        public void Dispose()
        {
        }

        public ushort GetGlyph(uint codepoint)
        {
            return 1;
        }

        public bool TryGetGlyph(uint codepoint, out ushort glyph)
        {
            glyph = 1;

            return true;
        }

        public int GetGlyphAdvance(ushort glyph)
        {
            return 12;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            return glyphs.ToArray().Select(x => (int)x).ToArray();
        }

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            return codepoints.ToArray().Select(x => (ushort)x).ToArray();
        }

        public bool TryGetTable(uint tag, out byte[] table)
        {
            table = null;
            return false;
        }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = new GlyphMetrics
            {
                Height = 10,
                Width = 8
            };

            return true;
        }
    }

    class HeadlessTextShaperStub : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;

            return new ShapedBuffer(text, text.Length, typeface, fontRenderingEmSize, bidiLevel);
        }
    }

    class HeadlessFontManagerStub : IFontManagerImpl
    {
        public IGlyphTypeface CreateGlyphTypeface(Typeface typeface)
        {
            return new HeadlessGlyphTypefaceImpl();
        }

        public string GetDefaultFontFamilyName()
        {
            return "Arial";
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return new List<string> { "Arial" };
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch,
            FontFamily fontFamily, CultureInfo culture, out Typeface typeface)
        {
            typeface = new Typeface("Arial", fontStyle, fontWeight, fontStretch);
            return true;
        }
    }

    class HeadlessIconLoaderStub : IPlatformIconLoader
    {

        class IconStub : IWindowIconImpl
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

    class HeadlessScreensStub : IScreenImpl
    {
        public int ScreenCount { get; } = 1;

        public IReadOnlyList<Screen> AllScreens { get; } = new[]
        {
            new Screen(1, new PixelRect(0, 0, 1920, 1280),
                new PixelRect(0, 0, 1920, 1280), true),
        };

        public Screen ScreenFromPoint(PixelPoint point)
        {
            return ScreenHelper.ScreenFromPoint(point, AllScreens);
        }

        public Screen ScreenFromRect(PixelRect rect)
        {
            return ScreenHelper.ScreenFromRect(rect, AllScreens);
        }

        public Screen ScreenFromWindow(IWindowBaseImpl window)
        {
            return ScreenHelper.ScreenFromWindow(window, AllScreens);
        }
    }

    internal class NoopStorageProvider : BclStorageProvider
    {
        public override bool CanOpen => false;
        public override Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            return Task.FromResult<IReadOnlyList<IStorageFile>>(Array.Empty<IStorageFile>());
        }

        public override bool CanSave => false;
        public override Task<IStorageFile> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            return Task.FromResult<IStorageFile>(null);
        }

        public override bool CanPickFolder => false;
        public override Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            return Task.FromResult<IReadOnlyList<IStorageFolder>>(Array.Empty<IStorageFolder>());
        }
    }
}
