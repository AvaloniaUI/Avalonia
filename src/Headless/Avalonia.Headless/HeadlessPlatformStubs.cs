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
        private readonly UnmanagedFontMemory? _fontMemory;

        public HeadlessPlatformTypeface(Stream stream, string? familyName = null)
        {
            _fontMemory = UnmanagedFontMemory.LoadFromStream(stream);

            var dummy = new GlyphTypeface(this);

            FamilyName = familyName ?? dummy.FamilyName;
            Weight = dummy.Weight;
            Style = dummy.Style;
            Stretch = dummy.Stretch;
        }

        public string FamilyName { get; }

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public FontSimulations FontSimulations => FontSimulations.None;

        public void Dispose()
        {
            _fontMemory?.Dispose();
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
        {
            stream = null;
            
            if (_fontMemory is null)
            {
                return false;
            }
            
            var data = _fontMemory.Memory.Span;

            stream = new MemoryStream(data.ToArray());

            return true;
        }

        public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
        {
            table = default;
            
            return _fontMemory is not null && _fontMemory.TryGetTable(tag, out table);
        }
    }
}

internal class HeadlessFontManagerStub : IFontManagerImpl
{
    private readonly string _defaultFamilyName;
    
    public HeadlessFontManagerStub(string defaultFamilyName = "Default")
    {
        _defaultFamilyName = defaultFamilyName;
    }

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
        string? familyName,
        CultureInfo? culture,
        out IPlatformTypeface platformTypeface)
    {
        platformTypeface = null!;

        return false;
    }

    public virtual bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
        FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        var defaultFontUri = new Uri("resm:Avalonia.Headless.BareMinimum.ttf?assembly=Avalonia.Headless");

        var assetLoader = new StandardAssetLoader(typeof(HeadlessFontManagerStub).Assembly);
        
        var stream = assetLoader.Open(defaultFontUri);
        
        platformTypeface = new HeadlessPlatformTypeface(stream, familyName);
        
        return true;
    }

    public virtual bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = new HeadlessPlatformTypeface(stream);
        
        return true;
    }

    public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
    {
        familyTypefaces = null;
        
        return false;
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
        platformTypeface = null;
        
        return false;
    }

    public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = null;
        
        return false;
    }

    public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
    {
        familyTypefaces = null;
        
        return false;
    }

    public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, 
        string? familyName, CultureInfo? culture, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = null;
        
        return false;
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
