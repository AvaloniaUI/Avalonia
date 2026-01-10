using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests;

public class TestFontManager : IFontManagerImpl
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

    public virtual bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
        [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = new HeadlessPlatformTypeface(stream);

        TryCreateGlyphTypefaceCount++;

        return true;
    }

    public bool TryGetFamilyTypefaces(string familyName,
        [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
    {
        familyTypefaces = null;

        return false;
    }
}
