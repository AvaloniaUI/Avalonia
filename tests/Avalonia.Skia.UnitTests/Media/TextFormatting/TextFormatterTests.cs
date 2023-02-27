﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextFormatterTests
    {
        [Fact]
        public void Should_Format_TextRuns_With_Default_Style()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Single(textLine.TextRuns);

                var textRun = textLine.TextRuns[0];

                Assert.Equal(defaultProperties.Typeface, textRun.Properties.Typeface);

                Assert.Equal(defaultProperties.ForegroundBrush, textRun.Properties.ForegroundBrush);

                Assert.Equal(text.Length, textRun.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_Multiple_Buffers()
        {
            using (Start())
            {
                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new MultiBufferTextSource(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(50, textLine.Length);
            }
        }

        private class TextSourceWithDummyRuns : ITextSource
        {
            private readonly TextRunProperties _properties;
            private readonly List<ValueSpan<TextRun>> _textRuns;

            public TextSourceWithDummyRuns(TextRunProperties properties)
            {
                _properties = properties;

                _textRuns = new List<ValueSpan<TextRun>>
                {
                    new ValueSpan<TextRun>(0, 5, new TextCharacters("Hello", _properties)),
                    new ValueSpan<TextRun>(5, 1, new DummyRun()),
                    new ValueSpan<TextRun>(6, 1, new DummyRun()),
                    new ValueSpan<TextRun>(7, 6, new TextCharacters(" World", _properties))
                };
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                foreach (var run in _textRuns)
                {
                    if (textSourceIndex < run.Start + run.Length)
                    {
                        return run.Value;
                    }
                }

                return new TextEndOfParagraph();
            }

            private class DummyRun : TextRun
            {
                public DummyRun()
                {
                    Length = DefaultTextSourceLength;
                }

                public override int Length { get; }
            }
        }

        [Fact]
        public void Should_Format_TextLine_With_Non_Text_TextRuns()
        {
            using (Start())
            {
                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new TextSourceWithDummyRuns(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(14, textLine.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_TextRunStyles()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var GenericTextRunPropertiesRuns = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 3, defaultProperties),
                    new ValueSpan<TextRunProperties>(3, 3,
                        new GenericTextRunProperties(Typeface.Default, 13, foregroundBrush: Brushes.Black)),
                    new ValueSpan<TextRunProperties>(6, 3,
                        new GenericTextRunProperties(Typeface.Default, 14, foregroundBrush: Brushes.Black)),
                    new ValueSpan<TextRunProperties>(9, 1, defaultProperties)
                };

                var textSource = new FormattedTextSource(text, defaultProperties, GenericTextRunPropertiesRuns);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(text.Length, textLine.Length);

                for (var i = 0; i < GenericTextRunPropertiesRuns.Length; i++)
                {
                    var GenericTextRunPropertiesRun = GenericTextRunPropertiesRuns[i];

                    var textRun = textLine.TextRuns[i];

                    Assert.Equal(GenericTextRunPropertiesRun.Length, textRun.Length);
                }
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
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(numberOfRuns, textLine.TextRuns.Count);
            }
        }

        [Fact]
        public void Should_Produce_A_Single_Fallback_Run()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                const string text = "👍 👍 👍 👍";

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(1, textLine.TextRuns.Count);
            }
        }

        [Fact]
        public void Should_Split_Run_On_Script()
        {
            using (Start())
            {
                const string text = "ABCDالدولي";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                var firstRun = textLine.TextRuns[0];

                Assert.Equal(4, firstRun.Length);
            }
        }

        [InlineData("𐐷𐐷𐐷𐐷𐐷", 10, 1)]
        [InlineData("01234 56789 01234 56789", 6, 4)]
        [Theory]
        public void Should_Wrap_With_Overflow(string text, int expectedCharactersPerLine, int expectedNumberOfLines)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.WrapWithOverflow);

                var textSource = new SingleBufferTextSource("ABCDEFHFFHFJHKHFK", defaultProperties, true);

                var formatter = new TextFormatterImpl();

                var line = formatter.FormatLine(textSource, 0, 33, paragraphProperties);

                textSource = new SingleBufferTextSource(text, defaultProperties);

                var numberOfLines = 0;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 1, paragraphProperties);

                    if (text.Length - currentPosition > expectedCharactersPerLine)
                    {
                        Assert.Equal(expectedCharactersPerLine, textLine.Length);
                    }

                    currentPosition += textLine.Length;

                    numberOfLines++;
                }

                Assert.Equal(expectedNumberOfLines, numberOfLines);
            }
        }

        [InlineData("Whether to turn off HTTPS. This option only applies if Individual, " +
                    "IndividualB2C, SingleOrg, or MultiOrg aren't used for &#8209;&#8209;auth."
            , "Noto Sans", 40)]
        [InlineData("01234 56789 01234 56789", "Noto Mono", 7)]
        [Theory]
        public void Should_Wrap(string text, string familyName, int numberOfCharactersPerLine)
        {
            using (Start())
            {
                var lineBreaker = new LineBreakEnumerator(text);

                var expected = new List<int>();

                while (lineBreaker.MoveNext(out var lineBreak))
                {
                    expected.Add(lineBreak.PositionWrap - 1);
                }

                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#" +
                                            familyName);

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var glyph = typeface.GlyphTypeface.GetGlyph('a');

                var advance = typeface.GlyphTypeface.GetGlyphAdvance(glyph) *
                              (12.0 / typeface.GlyphTypeface.Metrics.DesignEmHeight);

                var paragraphWidth = advance * numberOfCharactersPerLine;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, paragraphWidth,
                            new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap));

                    var end = textLine.FirstTextSourceIndex + textLine.Length - 1;

                    Assert.True(expected.Contains(end));

                    var index = expected.IndexOf(end);

                    for (var i = 0; i <= index; i++)
                    {
                        expected.RemoveAt(0);
                    }

                    currentPosition += textLine.Length;
                }
            }
        }

        [Fact]
        public void Should_Produce_Fixed_Height_Lines()
        {
            using (Start())
            {
                const string text = "012345";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties, lineHeight: 50));

                Assert.Equal(50, textLine.Height);
            }
        }

        [Fact]
        public void Should_Not_Produce_TextLine_Wider_Than_ParagraphWidth()
        {
            using (Start())
            {
                const string text =
                    "Multiline TextBlock with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. " +
                    "Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. " +
                    "Vivamus pretium ornare est.";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textSourceIndex = 0;

                while (textSourceIndex < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textSourceIndex, 200, paragraphProperties);

                    Assert.True(textLine.Width <= 200);

                    textSourceIndex += textLine.Length;
                }
            }
        }

        [Fact]
        public void Wrap_Should_Not_Produce_Empty_Lines()
        {
            using (Start())
            {
                const string text = "012345";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textSourceIndex = 0;

                while (textSourceIndex < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textSourceIndex, 3, paragraphProperties);

                    Assert.NotEqual(0, textLine.Length);

                    textSourceIndex += textLine.Length;
                }

                Assert.Equal(text.Length, textSourceIndex);
            }
        }

        [InlineData("Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor",
            new[] { "Lorem ipsum ", "dolor sit amet, ", "consectetur ", "adipisicing elit, ", "sed do eiusmod " })]

        [Theory]
        public void Should_Produce_Wrapped_And_Trimmed_Lines(string text, string[] expectedLines)
        {
            using (Start())
            {
                var typeface = new Typeface("Verdana");

                var defaultProperties = new GenericTextRunProperties(typeface, 32, foregroundBrush: Brushes.Black);

                var styleSpans = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 5,
                        new GenericTextRunProperties(typeface, 48)),
                    new ValueSpan<TextRunProperties>(6, 11,
                        new GenericTextRunProperties(new Typeface("Verdana", weight: FontWeight.Bold), 32)),
                    new ValueSpan<TextRunProperties>(28, 28,
                        new GenericTextRunProperties(new Typeface("Verdana", FontStyle.Italic),32))
                };

                var textSource = new FormattedTextSource(text, defaultProperties, styleSpans);

                var formatter = new TextFormatterImpl();

                var currentPosition = 0;

                var currentHeight = 0d;

                var currentLineIndex = 0;

                while (currentPosition < text.Length && currentLineIndex < expectedLines.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 300,
                            new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.WrapWithOverflow));

                    currentPosition += textLine.Length;

                    if (textLine.Width > 300 || currentHeight + textLine.Height > 240)
                    {
                        textLine = textLine.Collapse(new TextTrailingWordEllipsis(TextTrimming.DefaultEllipsisChar, 300, defaultProperties, FlowDirection.LeftToRight));
                    }

                    currentHeight += textLine.Height;

                    var currentText = text.Substring(textLine.FirstTextSourceIndex, textLine.Length);

                    Assert.Equal(expectedLines[currentLineIndex], currentText);

                    currentLineIndex++;
                }

                Assert.Equal(expectedLines.Length, currentLineIndex);
            }
        }

        [InlineData("0123456789", TextAlignment.Left, FlowDirection.LeftToRight)]
        [InlineData("0123456789", TextAlignment.Center, FlowDirection.LeftToRight)]
        [InlineData("0123456789", TextAlignment.Right, FlowDirection.LeftToRight)]

        [InlineData("0123456789", TextAlignment.Left, FlowDirection.RightToLeft)]
        [InlineData("0123456789", TextAlignment.Center, FlowDirection.RightToLeft)]
        [InlineData("0123456789", TextAlignment.Right, FlowDirection.RightToLeft)]

        [InlineData("שנבגק", TextAlignment.Left, FlowDirection.RightToLeft)]
        [InlineData("שנבגק", TextAlignment.Center, FlowDirection.RightToLeft)]
        [InlineData("שנבגק", TextAlignment.Right, FlowDirection.RightToLeft)]

        [Theory]
        public void Should_Align_TextLine(string text, TextAlignment textAlignment, FlowDirection flowDirection)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(flowDirection, textAlignment, true, true,
                    defaultProperties, TextWrapping.NoWrap, 0, 0, 0);

                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 100, paragraphProperties);

                var expectedOffset = 0d;

                switch (textAlignment)
                {
                    case TextAlignment.Center:
                        expectedOffset = 50 - textLine.Width / 2;
                        break;
                    case TextAlignment.Right:
                        expectedOffset = 100 - textLine.WidthIncludingTrailingWhitespace;
                        break;
                }

                Assert.Equal(expectedOffset, textLine.Start);
            }
        }

        [Fact]
        public void Should_Wrap_Syriac()
        {
            using (Start())
            {
                const string text =
                    "܀ ܁ ܂ ܃ ܄ ܅ ܆ ܇ ܈ ܉ ܊ ܋ ܌ ܍ ܏ ܐ ܑ ܒ ܓ ܔ ܕ ܖ ܗ ܘ ܙ ܚ ܛ ܜ ܝ ܞ ܟ ܠ ܡ ܢ ܣ ܤ ܥ ܦ ܧ ܨ ܩ ܪ ܫ ܬ ܰ ܱ ܲ ܳ ܴ ܵ ܶ ܷ ܸ ܹ ܺ ܻ ܼ ܽ ܾ ܿ ݀ ݁ ݂ ݃ ݄ ݅ ݆ ݇ ݈ ݉ ݊";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties =
                    new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textPosition = 87;
                TextLineBreak lastBreak = null;

                while (textPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textPosition, 50, paragraphProperties, lastBreak);

                    Assert.Equal(textLine.Length, textLine.TextRuns.Sum(x => x.Length));

                    textPosition += textLine.Length;

                    lastBreak = textLine.TextLineBreak;
                }
            }
        }

        [Fact]
        public void Should_FormatLine_With_Emergency_Breaks()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource("0123456789_0123456789_0123456789_0123456789", defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 33, paragraphProperties);

                var remainingRunsLineBreak = Assert.IsType<WrappingTextLineBreak>(textLine.TextLineBreak);
                var remainingRuns = remainingRunsLineBreak.AcquireRemainingRuns();
                Assert.NotNull(remainingRuns);
                Assert.NotEmpty(remainingRuns);
            }
        }

        [InlineData("פעילות הבינאום, W3C!")]
        [InlineData("abcABC")]
        [InlineData("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן")]
        [InlineData("טטטט abcDEF טטטט")]
        [Theory]
        public void Should_Not_Alter_TextRuns_After_TextStyles_Were_Applied(string text)
        {
            using (Start())
            {
                var formatter = new TextFormatterImpl();

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties =
                    new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.NoWrap);

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var expectedTextLine = formatter.FormatLine(new SingleBufferTextSource(text, defaultProperties),
                    0, double.PositiveInfinity, paragraphProperties);

                var expectedRuns = expectedTextLine.TextRuns.Cast<ShapedTextRun>().ToList();

                var expectedGlyphs = expectedRuns
                    .SelectMany(run => run.GlyphRun.GlyphInfos, (_, glyph) => glyph.GlyphIndex)
                    .ToList();

                for (var i = 0; i < text.Length; i++)
                {
                    for (var j = 1; i + j < text.Length; j++)
                    {
                        var spans = new[]
                        {
                            new ValueSpan<TextRunProperties>(i, j,
                                new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                        };

                        var textSource = new FormattedTextSource(text, defaultProperties, spans);

                        var textLine =
                            formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                        var shapedRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

                        var actualGlyphs = shapedRuns
                            .SelectMany(x => x.GlyphRun.GlyphInfos, (_, glyph) => glyph.GlyphIndex)
                            .ToList();

                        Assert.Equal(expectedGlyphs, actualGlyphs);
                    }
                }
            }
        }

        [Fact]
        public void Should_FormatLine_With_DrawableRuns()
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
            var textSource = new CustomTextSource("Hello World ->");

            using (Start())
            {
                var textLine =
                    TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.Equal(3, textLine.TextRuns.Count);

                Assert.True(textLine.TextRuns[1] is RectangleRun);
            }
        }

        [Fact]
        public void Should_Format_With_EndOfLineRun()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
                var textSource = new EndOfLineTextSource();

                var textLine =
                    TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(textLine.TextLineBreak);

                Assert.Equal(TextRun.DefaultTextSourceLength, textLine.Length);
            }
        }

        [Fact]
        public void Should_Return_Null_For_Empty_TextSource()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
                var textSource = new EmptyTextSource();

                var textLine = TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.Null(textLine);
            }
        }

        [Fact]
        public void Should_Retain_TextEndOfParagraph_With_TextWrapping()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrap: TextWrapping.Wrap);

                var text = "Hello World";

                var textSource = new SimpleTextSource(text, defaultRunProperties);

                var pos = 0;

                TextLineBreak previousLineBreak = null;
                TextLine textLine = null;

                while (pos < text.Length)
                {
                    textLine = TextFormatter.Current.FormatLine(textSource, pos, 30, paragraphProperties, previousLineBreak);

                    pos += textLine.Length;

                    previousLineBreak = textLine.TextLineBreak;
                }

                Assert.NotNull(textLine);

                Assert.NotNull(textLine.TextLineBreak.TextEndOfLine);
            }
        }

        protected readonly record struct SimpleTextSource : ITextSource
        {
            private readonly string _text;
            private readonly TextRunProperties _defaultProperties;

            public SimpleTextSource(string text, TextRunProperties defaultProperties)
            {
                _text = text;
                _defaultProperties = defaultProperties;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex > _text.Length)
                {
                    return new TextEndOfParagraph();
                }

                var runText = _text.AsMemory(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfParagraph();
                }

                return new TextCharacters(runText, _defaultProperties);
            }
        }

        private class EmptyTextSource : ITextSource
        {
            public TextRun GetTextRun(int textSourceIndex)
            {
                return null;
            }
        }

        private class EndOfLineTextSource : ITextSource
        {
            public TextRun GetTextRun(int textSourceIndex)
            {
                return new TextEndOfLine();
            }
        }

        private class CustomTextSource : ITextSource
        {
            private readonly string _text;

            public CustomTextSource(string text)
            {
                _text = text;
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _text.Length + TextRun.DefaultTextSourceLength + _text.Length)
                {
                    return null;
                }

                if (textSourceIndex == _text.Length)
                {
                    return new RectangleRun(new Rect(0, 0, 50, 50), Brushes.Green);
                }

                return new TextCharacters(_text, new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));
            }
        }

        private class RectangleRun : DrawableTextRun
        {
            private readonly Rect _rect;
            private readonly IBrush _fill;

            public RectangleRun(Rect rect, IBrush fill)
            {
                _rect = rect;
                _fill = fill;
            }

            public override Size Size => _rect.Size;
            public override double Baseline => 0;
            public override void Draw(DrawingContext drawingContext, Point origin)
            {
                using (drawingContext.PushPreTransform(Matrix.CreateTranslation(new Vector(origin.X, 0))))
                {
                    drawingContext.FillRectangle(_fill, _rect);
                }
            }
        }

        public static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(),
                    textShaperImpl: new TextShaperImpl()));

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(new FontManager(new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
