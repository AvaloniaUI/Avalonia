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
    /// <summary>
    /// A shaper hides the default ignorables it substitutes for line breaks behind the font's space
    /// glyph. A font that has no space glyph leaves it no way to do that, so those glyphs are deleted
    /// instead and a run holding nothing but a line break shapes to an empty glyph buffer. The run
    /// still owns its characters and has to survive, otherwise the line covers no text at all.
    /// </summary>
    public class EmptyShapedBufferTests
    {
        // The headless platform's default font: four glyphs, no space, no coverage for anything else.
        private const string GlyphlessFont = "Avalonia.Skia.UnitTests.Fonts.BareMinimum.ttf";

        private static readonly Typeface s_typeface = new("fonts:SystemFonts#BareMinimum");

        [Fact]
        public void Line_Break_That_Shapes_To_No_Glyphs_Keeps_Its_Characters()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(s_typeface, 12);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(new SingleBufferTextSource("\r\nfoo", defaultProperties), 0, 100,
                    new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap));

                Assert.NotNull(textLine);

                var run = Assert.IsType<ShapedTextRun>(Assert.Single(textLine.TextRuns));

                Assert.Equal(2, run.Length);
                Assert.Equal(2, textLine.Length);

                // The premise of the test: shaping really did produce nothing for these characters.
                Assert.Empty(run.ShapedBuffer);
            }
        }

        [Fact]
        public void Should_Wrap_Text_That_Starts_With_A_Line_Break()
        {
            using (Start())
            {
                // MaxLines bounds the layout loop: a line that covers no text never advances the text
                // source, so without it a regression here hangs the test run instead of failing it.
                var layout = new TextLayout("\r\nPassword update failed", s_typeface, 12, Brushes.Black,
                    textWrapping: TextWrapping.Wrap, maxWidth: 290, maxLines: 5);

                Assert.Equal(2, layout.TextLines.Count);

                Assert.Equal(2, layout.TextLines[0].Length);
                Assert.Equal(22, layout.TextLines[1].Length);
            }
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface()));

            var fontManagerImpl = new CustomFontManagerImpl();

            AvaloniaLocator.CurrentMutable
                .Bind<IFontManagerImpl>().ToConstant(fontManagerImpl);

            var fontManager = new FontManager(fontManagerImpl);

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(fontManager);

            fontManager.AddFontCollection(new GlyphlessSystemFontCollection());

            return disposable;
        }

        private sealed class GlyphlessSystemFontCollection : FontCollectionBase
        {
            public GlyphlessSystemFontCollection()
            {
                TryAddFontSource(new Uri($"resm:{GlyphlessFont}?assembly=Avalonia.Skia.UnitTests"));
            }

            public override Uri Key => FontManager.SystemFontsKey;
        }
    }
}
