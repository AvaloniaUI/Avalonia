#nullable enable

using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class GlyphTypefaceShapingTests
    {
        private const string MonoFont = "Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf";
        private const string ArabicFont = "Avalonia.Skia.UnitTests.Assets.NotoSansArabic-Regular.ttf";

        // A tiny Noto Sans Arabic subset with the GSUB/GPOS layout tables stripped: it keeps Arabic
        // cmap glyphs but cannot shape Arabic. Renamed so it doesn't collide with the full font.
        private const string ArabicNoLayoutFont = "Avalonia.Skia.UnitTests.Fonts.NotoSansArabic-NoLayout.ttf";

        // P0 — CanShapeScript gates complex scripts on GSUB/GPOS script coverage, not cmap. This is
        // the primitive the F3 capability fallback (Strategy A) builds on.
        [Fact]
        public void CanShapeScript_Gates_Complex_Scripts_On_Layout_Coverage_Not_Cmap()
        {
            using (Start(MonoFont, ArabicFont, ArabicNoLayoutFont))
            {
                var fontManager = FontManager.Current;

                Assert.True(fontManager.TryGetGlyphTypeface(
                    new Typeface("fonts:SystemFonts#Noto Mono"), out var mono));
                Assert.True(fontManager.TryGetGlyphTypeface(
                    new Typeface("fonts:SystemFonts#Noto Sans Arabic"), out var arabic));
                Assert.True(fontManager.TryGetGlyphTypeface(
                    new Typeface("fonts:SystemFonts#Noto Sans Arabic NoLayout"), out var arabicNoLayout));

                // Simple scripts never require layout tables — always true, regardless of the font.
                Assert.True(mono.CanShapeScript(Script.Latin));
                Assert.True(arabic.CanShapeScript(Script.Latin));

                // The real Arabic font declares the 'arab' GSUB script, so it can shape Arabic.
                Assert.True(arabic.CanShapeScript(Script.Arabic));

                // A Latin-only font has no Arabic layout coverage.
                Assert.False(mono.CanShapeScript(Script.Arabic));

                // The crux: the stripped font HAS Arabic cmap glyphs but no GSUB/GPOS, so it cannot
                // shape Arabic even though TryGetGlyph succeeds. cmap coverage is not shaping capability.
                Assert.True(arabicNoLayout.CharacterToGlyphMap.TryGetGlyph(0x0627, out _)); // ا is mapped
                Assert.False(arabicNoLayout.CanShapeScript(Script.Arabic));

                // A complex script the Arabic font does not declare is rejected too.
                Assert.False(arabic.CanShapeScript(Script.Devanagari));
            }
        }

        private static IDisposable Start(params string[] fontResourceNames)
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface()));

            var fontManagerImpl = new CustomFontManagerImpl();

            AvaloniaLocator.CurrentMutable
                .Bind<IFontManagerImpl>().ToConstant(fontManagerImpl);

            var fontManager = new FontManager(fontManagerImpl);

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(fontManager);

            fontManager.AddFontCollection(new CuratedSystemFontCollection(fontResourceNames));

            return disposable;
        }

        private sealed class CuratedSystemFontCollection : FontCollectionBase
        {
            public CuratedSystemFontCollection(string[] fontResourceNames)
            {
                foreach (var name in fontResourceNames)
                {
                    TryAddFontSource(new Uri($"resm:{name}?assembly=Avalonia.Skia.UnitTests"));
                }
            }

            public override Uri Key => FontManager.SystemFontsKey;
        }
    }
}
