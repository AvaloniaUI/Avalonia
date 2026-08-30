using System;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class TextFormatterTests : ScopedTestBase
    {
        [Fact]
        public void Should_Consume_Line_Break_That_Produces_No_Shaped_Runs()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties,
                    textWrapping: TextWrapping.Wrap);
                var textSource = new SingleBufferTextSource("\r\nABC", defaultRunProperties);

                var textLine = TextFormatter.Current.FormatLine(textSource, 0, 200, paragraphProperties);

                Assert.NotNull(textLine);

                Assert.Equal(2, textLine.Length);
            }
        }

        [Fact]
        public void Should_Wrap_Text_Starting_With_A_Line_Break_That_Produces_No_Shaped_Runs()
        {
            using (Start())
            {
                var textLayout = new TextLayout("\r\nABC", Typeface.Default, 12, Brushes.Black,
                    textWrapping: TextWrapping.Wrap, maxWidth: 200);

                Assert.Equal(2, textLayout.TextLines.Count);

                Assert.Equal(2, textLayout.TextLines[0].Length);

                Assert.Equal(3, textLayout.TextLines[1].Length);
            }
        }

        [Fact]
        public void Should_Reuse_Cached_Line_Break_That_Produces_No_Shaped_Runs()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties,
                    textWrapping: TextWrapping.Wrap);
                var textSource = new SingleBufferTextSource("\r\nABC", defaultRunProperties);

                using var textRunCache = new TextRunCache();

                _ = new TextLayout(textSource, paragraphProperties, maxWidth: 200, textRunCache: textRunCache);

                var textLayout = new TextLayout(textSource, paragraphProperties, maxWidth: 200,
                    textRunCache: textRunCache);

                Assert.Equal(2, textLayout.TextLines.Count);

                Assert.Equal(2, textLayout.TextLines[0].Length);

                Assert.Equal(3, textLayout.TextLines[1].Length);
            }
        }

        // The headless font has no glyphs for CR/LF, so shaping a line break produces no runs.
        private static IDisposable Start()
        {
            var fontManagerImpl = new HeadlessFontManagerStub();

            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: fontManagerImpl));

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(new FontManager(fontManagerImpl));

            return disposable;
        }

        private class SingleBufferTextSource : ITextSource
        {
            private readonly string _text;
            private readonly TextRunProperties _defaultProperties;

            public SingleBufferTextSource(string text, TextRunProperties defaultProperties)
            {
                _text = text;
                _defaultProperties = defaultProperties;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _text.Length)
                {
                    return null;
                }

                return new TextCharacters(_text.AsMemory(textSourceIndex), _defaultProperties);
            }
        }
    }
}
