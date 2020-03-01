using System;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Avalonia.Utility;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class SimpleTextFormatterTests
    {
        [Fact]
        public void Should_Format_TextRuns_With_Default_Style()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultTextRunStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

                var textSource = new SimpleTextSource(text, defaultTextRunStyle);

                var formatter = new SimpleTextFormatter();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, new TextParagraphProperties());

                Assert.Single(textLine.TextRuns);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(defaultTextRunStyle.TextFormat, textRun.Style.TextFormat);

                Assert.Equal(defaultTextRunStyle.Foreground, textRun.Style.Foreground);

                Assert.Equal(text.Length, textRun.Text.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_Multiple_Buffers()
        {
            using (Start())
            {
                var defaultTextRunStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

                var textSource = new MultiBufferTextSource(defaultTextRunStyle);

                var formatter = new SimpleTextFormatter();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new TextParagraphProperties(defaultTextRunStyle));

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(50, textLine.Text.Length);
            }
        }

        private class MultiBufferTextSource : ITextSource
        {
            private readonly string[] _runTexts;
            private readonly TextStyle _defaultStyle;

            public MultiBufferTextSource(TextStyle defaultStyle)
            {
                _defaultStyle = defaultStyle;

                _runTexts = new[] { "A123456789", "B123456789", "C123456789", "D123456789", "E123456789" };
            }

            public TextPointer TextPointer => new TextPointer(0, 50);

            public TextRun GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex == 50)
                {
                    return new TextEndOfParagraph();
                }

                var index = textSourceIndex / 10;

                var runText = _runTexts[index];

                return new TextCharacters(
                    new ReadOnlySlice<char>(runText.AsMemory(), textSourceIndex, runText.Length), _defaultStyle);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_TextRunStyles()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

                var textStyleRuns = new[]
                {
                    new TextStyleRun(new TextPointer(0, 3), defaultStyle ),
                    new TextStyleRun(new TextPointer(3, 3), new TextStyle(Typeface.Default, 13, Brushes.Black) ),
                    new TextStyleRun(new TextPointer(6, 3), new TextStyle(Typeface.Default, 14, Brushes.Black) ),
                    new TextStyleRun(new TextPointer(9, 1), defaultStyle )
                };

                var textSource = new FormattableTextSource(text, defaultStyle, textStyleRuns);

                var formatter = new SimpleTextFormatter();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, new TextParagraphProperties());

                Assert.Equal(text.Length, textLine.Text.Length);

                for (var i = 0; i < textStyleRuns.Length; i++)
                {
                    var textStyleRun = textStyleRuns[i];

                    var textRun = textLine.TextRuns[i];

                    Assert.Equal(textStyleRun.TextPointer.Length, textRun.Text.Length);
                }
            }
        }

        private class FormattableTextSource : ITextSource
        {
            private readonly ReadOnlySlice<char> _text;
            private readonly TextStyle _defaultStyle;
            private ReadOnlySlice<TextStyleRun> _textStyleRuns;

            public FormattableTextSource(string text, TextStyle defaultStyle, ReadOnlySlice<TextStyleRun> textStyleRuns)
            {
                _text = text.AsMemory();

                _defaultStyle = defaultStyle;

                _textStyleRuns = textStyleRuns;
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                if (_textStyleRuns.IsEmpty)
                {
                    return new TextEndOfParagraph();
                }

                var styleRun = _textStyleRuns[0];

                _textStyleRuns = _textStyleRuns.Skip(1);

                return new TextCharacters(_text.AsSlice(styleRun.TextPointer.Start, styleRun.TextPointer.Length),
                    _defaultStyle);
            }
        }

        [Theory]
        [InlineData("0123", 1)]
        [InlineData("\r\n", 1)]
        [InlineData("👍b", 2)]
        [InlineData("a👍b", 3)]
        [InlineData("a👍子b", 4)]
        public void Should_Produce_Unique_Runs(string text, int numberOfRuns)
        {
            using (Start())
            {
                var textSource = new SimpleTextSource(text, new TextStyle(Typeface.Default));

                var formatter = new SimpleTextFormatter();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity, new TextParagraphProperties());

                Assert.Equal(numberOfRuns, textLine.TextRuns.Count);
            }
        }

        private class SimpleTextSource : ITextSource
        {
            private readonly ReadOnlySlice<char> _text;
            private readonly TextStyle _defaultTextStyle;

            public SimpleTextSource(string text, TextStyle defaultText)
            {
                _text = text.AsMemory();
                _defaultTextStyle = defaultText;
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                var runText = _text.Skip(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfParagraph();
                }

                return new TextCharacters(runText, _defaultTextStyle);
            }
        }

        [Fact]
        public void Should_Split_Run_On_Script()
        {
            using (Start())
            {
                const string text = "1234الدولي";

                var textSource = new SimpleTextSource(text, new TextStyle(Typeface.Default));

                var formatter = new SimpleTextFormatter();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity, new TextParagraphProperties());

                Assert.Equal(4, textLine.TextRuns[0].Text.Length);
            }
        }

        [Fact]
        public void Should_Get_Distance_From_CharacterHit()
        {
            using (Start())
            {
                var textSource = new MultiBufferTextSource(new TextStyle(Typeface.Default));

                var formatter = new SimpleTextFormatter();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity, new TextParagraphProperties());

                var currentDistance = 0.0;

                foreach (var run in textLine.TextRuns)
                {
                    var textRun = (ShapedTextRun)run;

                    var glyphRun = textRun.GlyphRun;

                    for (var i = 0; i < glyphRun.GlyphClusters.Length; i++)
                    {
                        var cluster = glyphRun.GlyphClusters[i];

                        var advance = glyphRun.GlyphAdvances[i];

                        var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(cluster));

                        Assert.Equal(currentDistance, distance);

                        currentDistance += advance;
                    }
                }

                Assert.Equal(currentDistance, textLine.GetDistanceFromCharacterHit(new CharacterHit(textSource.TextPointer.Length)));
            }
        }

        [Fact]
        public void Should_Get_CharacterHit_From_Distance()
        {
            using (Start())
            {
                var textSource = new MultiBufferTextSource(new TextStyle(Typeface.Default));

                var formatter = new SimpleTextFormatter();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity, new TextParagraphProperties());

                var currentDistance = 0.0;

                CharacterHit characterHit;

                foreach (var run in textLine.TextRuns)
                {
                    var textRun = (ShapedTextRun)run;

                    var glyphRun = textRun.GlyphRun;

                    for (var i = 0; i < glyphRun.GlyphClusters.Length; i++)
                    {
                        var cluster = glyphRun.GlyphClusters[i];

                        var advance = glyphRun.GlyphAdvances[i];

                        characterHit = textLine.GetCharacterHitFromDistance(currentDistance);

                        Assert.Equal(cluster, characterHit.FirstCharacterIndex + characterHit.TrailingLength);

                        currentDistance += advance;
                    }
                }

                characterHit = textLine.GetCharacterHitFromDistance(textLine.LineMetrics.Size.Width);

                Assert.Equal(textSource.TextPointer.End, characterHit.FirstCharacterIndex);
            }
        }

        public static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    textShaperImpl: new TextShaperImpl()));

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(new FontManager(new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
