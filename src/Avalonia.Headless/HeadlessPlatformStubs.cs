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

    class HeadlessPlatformSettingsStub : IPlatformSettings
    {
        public Size DoubleClickSize { get; } = new Size(2, 2);
        public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);

        public Size TouchDoubleClickSize => new Size(16,16);

        public TimeSpan TouchDoubleClickTime => DoubleClickTime;
    }

    class HeadlessGlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        public short DesignEmHeight => 10;

        public int Ascent => 5;

        public int Descent => 5;

        public int LineGap => 2;

        public int UnderlinePosition => 5;

        public int UnderlineThickness => 5;

        public int StrikethroughPosition => 5;

        public int StrikethroughThickness => 2;

        public bool IsFixedPitch => true;

        public int GlyphCount => 1337;

        public void Dispose()
        {
        }

        public ushort GetGlyph(uint codepoint)
        {
            return 1;
        }

        public int GetGlyphAdvance(ushort glyph)
        {
            return 1;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            return glyphs.ToArray().Select(x => (int)x).ToArray();
        }

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            return codepoints.ToArray().Select(x => (ushort)x).ToArray();
        }
    }

    class HeadlessTextShaperStub : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlySlice<char> text, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;

            return new ShapedBuffer(text, text.Length, typeface, fontRenderingEmSize, bidiLevel);
        }
    }

    class HeadlessFontManagerStub : IFontManagerImpl
    {
        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
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
