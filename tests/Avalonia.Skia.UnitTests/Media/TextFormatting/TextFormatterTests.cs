﻿using System;
using System.Collections.Generic;
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

                Assert.Equal(text.Length, textRun.Text.Length);
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

                Assert.Equal(50, textLine.TextRange.Length);
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

                var textSource = new FormattableTextSource(text, defaultProperties, GenericTextRunPropertiesRuns);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(text.Length, textLine.TextRange.Length);

                for (var i = 0; i < GenericTextRunPropertiesRuns.Length; i++)
                {
                    var GenericTextRunPropertiesRun = GenericTextRunPropertiesRuns[i];

                    var textRun = textLine.TextRuns[i];

                    Assert.Equal(GenericTextRunPropertiesRun.Length, textRun.Text.Length);
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
        public void Should_Split_Run_On_Script()
        {
            using (Start())
            {
                const string text = "1234الدولي";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.Equal(4, textLine.TextRuns[0].Text.Length);
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

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var numberOfLines = 0;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 1,
                            new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.WrapWithOverflow));

                    if (text.Length - currentPosition > expectedCharactersPerLine)
                    {
                        Assert.Equal(expectedCharactersPerLine, textLine.TextRange.Length);
                    }

                    currentPosition += textLine.TextRange.Length;

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
                var lineBreaker = new LineBreakEnumerator(text.AsMemory());

                var expected = new List<int>();

                while (lineBreaker.MoveNext())
                {
                    expected.Add(lineBreaker.Current.PositionWrap - 1);
                }

                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#" +
                                            familyName);

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var glyph = typeface.GlyphTypeface.GetGlyph('a');

                var advance = typeface.GlyphTypeface.GetGlyphAdvance(glyph) *
                              (12.0 / typeface.GlyphTypeface.DesignEmHeight);

                var paragraphWidth = advance * numberOfCharactersPerLine;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, paragraphWidth,
                            new GenericTextParagraphProperties(defaultProperties, textWrapping: TextWrapping.Wrap));

                    Assert.True(expected.Contains(textLine.TextRange.End));

                    var index = expected.IndexOf(textLine.TextRange.End);

                    for (var i = 0; i <= index; i++)
                    {
                        expected.RemoveAt(0);
                    }

                    currentPosition += textLine.TextRange.Length;
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

                Assert.Equal(50, textLine.LineMetrics.Size.Height);
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
