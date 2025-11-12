using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Utils;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace Avalonia.Headless
{
    internal sealed class HeadlessClipboardImplStub : IOwnedClipboardImpl
    {
        private IAsyncDataTransfer? _data;

        public Task<IAsyncDataTransfer?> TryGetDataAsync()
            // Return an instance that won't be disposed (we're keeping the ownership).
            => Task.FromResult<IAsyncDataTransfer?>(_data is null ? null : new NonDisposingDataTransfer(_data));

        public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
        {
            _data = dataTransfer;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _data?.Dispose();
            _data = null;
            return Task.CompletedTask;
        }

        public Task<bool> IsCurrentOwnerAsync()
            => Task.FromResult(_data is not null);

        private sealed class NonDisposingDataTransfer(IAsyncDataTransfer wrapped) : IAsyncDataTransfer
        {
            private readonly IAsyncDataTransfer _wrapped = wrapped;

            public IReadOnlyList<DataFormat> Formats
                => _wrapped.Formats;

            public IReadOnlyList<IAsyncDataTransferItem> Items
                => _wrapped.Items;

            void IDisposable.Dispose()
            {
            }
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

    internal class HeadlessPlatformTypeface : IPlatformTypeface
    {
        private readonly UnmanagedFontMemory _fontMemory;

        public HeadlessPlatformTypeface(Stream stream)
        {
            _fontMemory = UnmanagedFontMemory.LoadFromStream(stream);

            var dummy = new GlyphTypeface(this, FontSimulations.None);

            Weight = dummy.Weight;
            Style = dummy.Style;
            Stretch = dummy.Stretch;
        }

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public void Dispose()
        {
            _fontMemory.Dispose();
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
        {
            var data = _fontMemory.Memory.Span;

            stream = new MemoryStream(data.ToArray());

            return true;
        }

        public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table) => _fontMemory.TryGetTable(tag, out table);
    }
}

internal class HeadlessGlyphTypeface : IGlyphTypeface
{
    private readonly IGlyphTypeface _inner;

    public HeadlessGlyphTypeface(IGlyphTypeface inner, string familyName)
    {
        _inner = inner;
        FamilyName = familyName;
    }

    public string FamilyName { get; }

    public string TypographicFamilyName => FamilyName;

    public IReadOnlyDictionary<CultureInfo, string> FamilyNames => _inner.FamilyNames;

    public IReadOnlyList<OpenTypeTag> SupportedFeatures => _inner.SupportedFeatures;

    public IReadOnlyDictionary<CultureInfo, string> FaceNames => _inner.FaceNames;

    public FontWeight Weight => _inner.Weight;

    public FontStyle Style => _inner.Style;

    public FontStretch Stretch => _inner.Stretch;

    public uint GlyphCount => _inner.GlyphCount;

    public FontSimulations FontSimulations => _inner.FontSimulations;

    public FontMetrics Metrics => _inner.Metrics;

    public IReadOnlyDictionary<int, ushort> CharacterToGlyphMap => _inner.CharacterToGlyphMap;

    public IPlatformTypeface PlatformTypeface => _inner.PlatformTypeface;

    public ITextShaperTypeface TextShaperTypeface => _inner.TextShaperTypeface;

    public void Dispose() => _inner.Dispose();

    public ushort GetGlyphAdvance(ushort glyph) => _inner.GetGlyphAdvance(glyph);

    public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics) => _inner.TryGetGlyphMetrics(glyph, out metrics);
}

internal class HeadlessFontManagerStub : IFontManagerImpl
{
    private readonly string _interFontUri = "avares://Avalonia.Fonts.Inter/Assets/Inter-Regular.ttf";
    private readonly string _defaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter";

    public int TryCreateGlyphTypefaceCount { get; private set; }

    public string GetDefaultFontFamilyName() => _defaultFamilyName;

    string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
    {
        return new[] { _defaultFamilyName };
    }

    public bool TryMatchCharacter(
        int codepoint,
        FontStyle fontStyle,
        FontWeight fontWeight,
        FontStretch fontStretch,
        CultureInfo? culture,
        out IPlatformTypeface platformTypeface)
    {
        platformTypeface = null!;

        return false;
    }

    public virtual bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
        FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = null;

        if (familyName == "MyFont")
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var stream = assetLoader.Open(new Uri(_interFontUri));

            platformTypeface = new HeadlessPlatformTypeface(stream);
        }

        TryCreateGlyphTypefaceCount++;

        return platformTypeface != null;
    }

    public virtual bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = new HeadlessPlatformTypeface(stream);

        TryCreateGlyphTypefaceCount++;

        return true;
    }

    public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
    {
        throw new NotImplementedException();
    }
}

internal class HeadlessFontManagerWithMultipleSystemFontsStub : IFontManagerImpl
{
    private readonly string[] _installedFontFamilyNames;
    private readonly string _defaultFamilyName;

    public HeadlessFontManagerWithMultipleSystemFontsStub(
        string[] installedFontFamilyNames,
        string defaultFamilyName = "Default")
    {
        _installedFontFamilyNames = installedFontFamilyNames;
        _defaultFamilyName = defaultFamilyName;
    }

    public int TryCreateGlyphTypefaceCount { get; private set; }

    string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
    {
        return _installedFontFamilyNames;
    }

    public string GetDefaultFontFamilyName()
    {
        return _defaultFamilyName;
    }

    public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        throw new NotImplementedException();
    }

    public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        throw new NotImplementedException();
    }

    public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
    {
        throw new NotImplementedException();
    }

    public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, CultureInfo? culture, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        throw new NotImplementedException();
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

internal class HeadlessScreensStub : ScreensBase<int, PlatformScreen>
{
    protected override IReadOnlyList<int> GetAllScreenKeys() => new[] { 1 };

    protected override PlatformScreen CreateScreenFromKey(int key) => new PlatformScreenStub(key);

    private class PlatformScreenStub : PlatformScreen
    {
        public PlatformScreenStub(int key) : base(new PlatformHandle((nint)key, nameof(HeadlessScreensStub)))
        {
            Scaling = 1;
            Bounds = WorkingArea = new PixelRect(0, 0, 1920, 1280);
            IsPrimary = true;
        }
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
