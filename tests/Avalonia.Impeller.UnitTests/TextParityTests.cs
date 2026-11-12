using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Skia;
using Xunit;
using static Avalonia.Impeller.UnitTests.ImpellerTestApp;

namespace Avalonia.Impeller.UnitTests;

/// <summary>Typefaces, glyph runs and font matching on Impeller must answer like the Skia backend.</summary>
public class TextParityTests
{
    private static readonly (IPlatformRenderInterface Render, IFontManagerImpl Fonts) Impeller =
        Create(() => ImpellerPlatform.Initialize());
    private static readonly (IPlatformRenderInterface Render, IFontManagerImpl Fonts) Skia =
        Create(() => SkiaPlatform.Initialize());
    private static IPlatformRenderInterface ImpellerRender => Impeller.Render;
    private static IFontManagerImpl ImpellerFonts => Impeller.Fonts;

    private static (IPlatformRenderInterface, IFontManagerImpl) Create(Action initialize)
    {
        using (AvaloniaLocator.EnterScope())
        {
            initialize();
            return (AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>(),
                AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>());
        }
    }

    public static IEnumerable<object[]> Fonts() =>
    [
        ["Inter-Regular.ttf"], ["Inter-Bold.ttf"], ["NotoSans-Italic.ttf"], ["SourceSerif4_36pt-Italic.ttf"],
        ["NotoSansArabic-Regular.ttf"],
    ];

    private static Stream Asset(string name) =>
        typeof(TextParityTests).Assembly.GetManifestResourceStream("Avalonia.Impeller.UnitTests.Assets." + name)
        ?? throw new FileNotFoundException(name);

    private static IPlatformTypeface Load(IFontManagerImpl fonts, string name,
        FontSimulations simulations = FontSimulations.None)
    {
        Assert.True(fonts.TryCreateGlyphTypeface(Asset(name), simulations, out var typeface));
        return typeface!;
    }

    [Theory]
    [MemberData(nameof(Fonts))]
    public void Typeface_Properties_Match_Skia(string font)
    {
        using var skia = Load(Skia.Fonts, font);
        using var impeller = Load(ImpellerFonts, font);

        Assert.Equal(skia.FamilyName, impeller.FamilyName);
        Assert.Equal(skia.Weight, impeller.Weight);
        Assert.Equal(skia.Style, impeller.Style);
        Assert.Equal(skia.Stretch, impeller.Stretch);

        foreach (var tag in new[] { "head", "cmap", "hhea", "OS/2", "GSUB" })
        {
            var openTypeTag = OpenTypeTag.Parse(tag);
            Assert.Equal(skia.TryGetTable(openTypeTag, out var skiaTable),
                impeller.TryGetTable(openTypeTag, out var impellerTable));
            Assert.True(skiaTable.Span.SequenceEqual(impellerTable.Span), $"{tag} table differs");
        }

        Assert.True(impeller.TryGetStream(out var stream));
        using (stream)
        using (var original = Asset(font))
            Assert.Equal(original.Length, stream.Length);
    }

    private static IReadOnlyList<GlyphInfo> Shape(GlyphTypeface typeface, string text, double size)
    {
        var scale = size / typeface.Metrics.DesignEmHeight;
        var infos = new List<GlyphInfo>();
        for (var i = 0; i < text.Length; i++)
        {
            var glyph = typeface.CharacterToGlyphMap[text[i]];
            typeface.TryGetGlyphMetrics(glyph, out var metrics);
            // No offsets: the Skia backend leaves them out of bounds and outlines.
            infos.Add(new GlyphInfo(glyph, i, metrics.AdvanceWidth * scale));
        }
        return infos;
    }

    [Theory]
    [InlineData("Inter-Regular.ttf", 12.0, FontSimulations.None)]
    [InlineData("Inter-Regular.ttf", 37.5, FontSimulations.None)]
    [InlineData("Inter-Regular.ttf", 24.0, FontSimulations.Bold)]
    [InlineData("Inter-Regular.ttf", 24.0, FontSimulations.Oblique)]
    [InlineData("SourceSerif4_36pt-Italic.ttf", 20.0, FontSimulations.None)]
    public void Glyph_Runs_Match_Skia(string font, double size, FontSimulations simulations)
    {
        using var app = Start();
        const string text = "Hello, Avalonia! gjpqy";
        var origin = new Point(10.5, 40.25);

        var skiaTypeface = new GlyphTypeface(Load(Skia.Fonts, font, simulations), simulations);
        var impellerTypeface = new GlyphTypeface(Load(ImpellerFonts, font, simulations), simulations);
        var infos = Shape(skiaTypeface, text, size);

        using var skiaRun = Skia.Render.CreateGlyphRun(skiaTypeface, size, infos, origin);
        using var impellerRun = ImpellerRender.CreateGlyphRun(impellerTypeface, size, infos, origin);
        AssertClose(skiaRun.Bounds, impellerRun.Bounds, 1.5, "glyph run bounds");

        var skiaGeometry = Skia.Render.BuildGlyphRunGeometry(
            new GlyphRun(skiaTypeface, size, text.AsMemory(), infos, origin));
        var impellerGeometry = ImpellerRender.BuildGlyphRunGeometry(
            new GlyphRun(impellerTypeface, size, text.AsMemory(), infos, origin));
        AssertClose(skiaGeometry.Bounds, impellerGeometry.Bounds, 0.5, "outline bounds");

        var mismatches = 0;
        var total = 0;
        for (var y = skiaGeometry.Bounds.Top; y < skiaGeometry.Bounds.Bottom; y += 0.7)
            for (var x = skiaGeometry.Bounds.Left; x < skiaGeometry.Bounds.Right; x += 0.7)
            {
                total++;
                if (skiaGeometry.FillContains(new Point(x, y)) != impellerGeometry.FillContains(new Point(x, y)))
                    mismatches++;
            }
        Assert.True(mismatches <= total / 100, $"{mismatches} of {total} outline hit tests differ");
    }

    private static void AssertClose(Rect expected, Rect actual, double tolerance, string what) =>
        Assert.True(Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance &&
                    Math.Abs(expected.Right - actual.Right) <= tolerance &&
                    Math.Abs(expected.Bottom - actual.Bottom) <= tolerance,
            $"{what}: expected {expected}, got {actual}");

    [Fact]
    public void Font_Matching_Matches_Skia()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Expects the fonts of the test container.");

        var defaultFamily = Skia.Fonts.GetDefaultFontFamilyName();
        Assert.Equal(defaultFamily, ImpellerFonts.GetDefaultFontFamilyName());
        Assert.Equal(Skia.Fonts.GetInstalledFontFamilyNames().OrderBy(n => n),
            ImpellerFonts.GetInstalledFontFamilyNames().OrderBy(n => n));

        foreach (var (style, weight) in new[] { (FontStyle.Normal, FontWeight.Normal), (FontStyle.Normal, FontWeight.Bold),
                     (FontStyle.Italic, FontWeight.Normal) })
        {
            Assert.True(Skia.Fonts.TryCreateGlyphTypeface(defaultFamily, style, weight, FontStretch.Normal,
                out var skia));
            Assert.True(ImpellerFonts.TryCreateGlyphTypeface(defaultFamily, style, weight, FontStretch.Normal,
                out var impeller));
            Assert.Equal(skia!.FamilyName, impeller!.FamilyName);
            Assert.Equal(skia.Weight, impeller.Weight);
            Assert.Equal(skia.Style, impeller.Style);
        }

        Assert.False(ImpellerFonts.TryCreateGlyphTypeface("No Such Family 12345", FontStyle.Normal,
            FontWeight.Normal, FontStretch.Normal, out _));

        foreach (var codepoint in new[] { 'A', 0x0416, 0x05D0, 0x4E2D })
        {
            var hasSkia = Skia.Fonts.TryMatchCharacter(codepoint, FontStyle.Normal, FontWeight.Normal,
                FontStretch.Normal, defaultFamily, null, out var skia);
            var hasImpeller = ImpellerFonts.TryMatchCharacter(codepoint, FontStyle.Normal, FontWeight.Normal,
                FontStretch.Normal, defaultFamily, null, out var impeller);
            Assert.Equal(hasSkia, hasImpeller);
            if (hasSkia)
                Assert.Equal(skia!.FamilyName, impeller!.FamilyName);
        }

        foreach (var family in new[] { defaultFamily, "DejaVu Sans" })
        {
            var hasSkia = Skia.Fonts.TryGetFamilyTypefaces(family, out var skiaFaces);
            Assert.Equal(hasSkia, ImpellerFonts.TryGetFamilyTypefaces(family, out var impellerFaces));
            if (hasSkia)
                Assert.Equal(skiaFaces!.Select(Describe).Order(), impellerFaces!.Select(Describe).Order());
        }
        Assert.False(ImpellerFonts.TryGetFamilyTypefaces("No Such Family 12345", out _));
    }

    private static string Describe(Typeface typeface) =>
        $"{typeface.FontFamily.Name} {typeface.Style} {(int)typeface.Weight} {typeface.Stretch}";

    [Fact]
    public void Font_Stretch_Is_Matched_Like_Skia()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Expects the fonts of the test container.");
        const string family = "DejaVu Sans";
        Assert.SkipUnless(ImpellerFonts.TryGetFamilyTypefaces(family, out var faces) &&
                          faces.Any(f => f.Stretch < FontStretch.Normal), $"{family} Condensed isn't installed.");

        foreach (var stretch in new[] { FontStretch.Condensed, FontStretch.SemiCondensed, FontStretch.Normal,
                     FontStretch.Expanded })
        foreach (var style in new[] { FontStyle.Normal, FontStyle.Oblique, FontStyle.Italic })
        {
            Assert.True(Skia.Fonts.TryCreateGlyphTypeface(family, style, FontWeight.Normal, stretch, out var skia));
            Assert.True(ImpellerFonts.TryCreateGlyphTypeface(family, style, FontWeight.Normal, stretch,
                out var impeller));
            Assert.Equal((skia!.Stretch, skia.Style, skia.Weight), (impeller!.Stretch, impeller.Style, impeller.Weight));
        }

        Assert.True(ImpellerFonts.TryCreateGlyphTypeface(family, FontStyle.Normal, FontWeight.Normal,
            FontStretch.Condensed, out var condensed));
        Assert.Equal(FontStretch.SemiCondensed, condensed!.Stretch);
    }
}
