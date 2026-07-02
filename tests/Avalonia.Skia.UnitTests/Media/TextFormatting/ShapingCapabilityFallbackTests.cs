#nullable enable

using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class ShapingCapabilityFallbackTests
    {
        private const string MonoFont = "Avalonia.Skia.UnitTests.Assets.NotoMono-Regular.ttf";
        private const string ArabicFont = "Avalonia.Skia.UnitTests.Assets.NotoSansArabic-Regular.ttf";

        // Tiny Noto Sans Arabic subset with the layout tables stripped: it has the Arabic cmap glyphs
        // but no GSUB/GPOS, so it cannot actually shape Arabic. Renamed so it doesn't collide with the
        // full font.
        private const string ArabicNoLayoutFont = "Avalonia.Skia.UnitTests.Fonts.NotoSansArabic-NoLayout.ttf";

        // For a complex script, a primary that has the cmap glyphs but can't shape it (no GSUB/GPOS) is
        // upgraded to a shaping-capable font. This is unconditional — there is no longer a mode toggle.
        [Fact]
        public void CmapOnly_Complex_Script_Primary_Is_Upgraded_To_A_Shaping_Capable_Font()
        {
            // A shaping-capable Arabic font is present, so the cmap-only primary is replaced by it.
            Assert.Equal("Noto Sans Arabic",
                ResolveArabicRunFamily(MonoFont, ArabicFont, ArabicNoLayoutFont));
        }

        // When no shaping-capable font for the script exists, the cmap-only font is kept (the capability
        // tier finds nothing, the cmap tier then accepts it) — we never reject more than before.
        [Fact]
        public void CmapOnly_Complex_Script_Primary_Is_Kept_When_No_Capable_Font_Exists()
        {
            Assert.Equal("Noto Sans Arabic NoLayout",
                ResolveArabicRunFamily(MonoFont, ArabicNoLayoutFont));
        }

        private static string ResolveArabicRunFamily(params string[] fontResourceNames)
        {
            using (Start(fontResourceNames))
            {
                var fontManager = FontManager.Current;

                // Primary run typeface: the cmap-only (no-layout) Arabic font.
                var defaultProperties = new GenericTextRunProperties(
                    new Typeface("fonts:SystemFonts#Noto Sans Arabic NoLayout"));

                var text = char.ConvertFromUtf32(0x0627).AsMemory(); // U+0627 ARABIC LETTER ALEF

                var textCharacters = new TextCharacters(text, defaultProperties);

                var results = FormattingObjectPool.Instance.TextRunLists.Rent();

                try
                {
                    TextRunProperties? previousProperties = null;

                    textCharacters.GetShapeableCharacters(text, 0, fontManager, ref previousProperties, results);

                    Assert.Single(results);
                    Assert.True(fontManager.TryGetGlyphTypeface(results[0].Properties!.Typeface, out var runGlyphTypeface));

                    return runGlyphTypeface.FamilyName;
                }
                finally
                {
                    FormattingObjectPool.RentedList<TextRun>? toReturn = results;
                    FormattingObjectPool.Instance.TextRunLists.Return(ref toReturn);
                }
            }
        }

        private static IDisposable Start(string[] fontResourceNames)
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
