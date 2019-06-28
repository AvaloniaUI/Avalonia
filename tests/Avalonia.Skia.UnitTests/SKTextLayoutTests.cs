﻿using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Media;
using Avalonia.Skia.Text;

using SkiaSharp;

using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class SKTextLayoutTests
    {
        private static readonly string s_singleLineText = "0123456789";
        private static readonly string s_multiLineText = "012345678\r\r0123456789";

        [Fact]
        public void ShouldApplyTextStyleSpanToTextInBetween()
        {
            var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(1, 2, foreground: foreground)
                        };

            var layout = new SKTextLayout(
                s_multiLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.TextPointer.Length);

            var actual = s_multiLineText.Substring(textRun.TextPointer.StartingIndex, textRun.TextPointer.Length);

            Assert.Equal("12", actual);

            Assert.Equal(foreground, textRun.Foreground);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtStart()
        {
            var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(0, 2, foreground: foreground)
                        };

            var layout = new SKTextLayout(
                s_singleLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(2, textRun.TextPointer.Length);

            var actual = s_singleLineText.Substring(textRun.TextPointer.StartingIndex, textRun.TextPointer.Length);

            Assert.Equal("01", actual);

            Assert.Equal(foreground, textRun.Foreground);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToTextAtEnd()
        {
            var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(8, 2, foreground: foreground)
                        };

            var layout = new SKTextLayout(
                s_singleLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(2, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.TextPointer.Length);

            var actual = s_singleLineText.Substring(textRun.TextPointer.StartingIndex, textRun.TextPointer.Length);

            Assert.Equal("89", actual);

            Assert.Equal(foreground, textRun.Foreground);
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToSingleCharacter()
        {
            var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(0, 1, foreground: foreground)
                        };

            var layout = new SKTextLayout(
                "0",
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(1, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[0];

            Assert.Equal(1, textRun.TextPointer.Length);

            Assert.Equal(foreground, textRun.Foreground);
        }

        [Fact]
        public void ShouldApplyTextSpanToUnicodeStringInBetween()
        {
            const string Text = "😄😄😄😄";

            var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(4, 2, foreground: foreground)
                        };

            var layout = new SKTextLayout(
                Text,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity),
                spans);

            var textLine = layout.TextLines[0];

            Assert.Equal(3, textLine.TextRuns.Count);

            var textRun = textLine.TextRuns[1];

            Assert.Equal(2, textRun.TextPointer.Length);

            var actual = Text.Substring(textRun.TextPointer.StartingIndex, textRun.TextPointer.Length);

            Assert.Equal("😄", actual);

            Assert.Equal(foreground, textRun.Foreground);
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextLineLengthSum()
        {
            var layout = new SKTextLayout(
                s_multiLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(s_multiLineText.Length, layout.TextLines.Sum(x => x.TextPointer.Length));
        }

        [Fact]
        public void TextLengthShouldBeEqualToTextRunTextLengthSum()
        {
            var layout = new SKTextLayout(
                s_multiLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(
                s_multiLineText.Length,
                layout.TextLines.Select(textLine => textLine.TextRuns.Sum(textRun => textRun.TextPointer.Length)).Sum());
        }

        [Fact]
        public void ShouldApplyTextStyleSpanToMultiLine()
        {
            var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

            var spans = new List<FormattedTextStyleSpan>
                        {
                            new FormattedTextStyleSpan(5, 20, foreground: foreground)
                        };

            var layout = new SKTextLayout(
                s_multiLineText,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(200, 125),
                spans);

            Assert.Equal(foreground, layout.TextLines[0].TextRuns[1].Foreground);
            Assert.Equal(foreground, layout.TextLines[1].TextRuns[0].Foreground);
            Assert.Equal(foreground, layout.TextLines[2].TextRuns[0].Foreground);
        }

        [Fact(Skip = "Currently fails on Linux because of not present Emojis font")]
        public void ShouldHitTestSurrogatePair()
        {
            const string Text = "😄";

            var layout = new SKTextLayout(
                Text,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            var lineMetrics = layout.TextLines[0].LineMetrics;

            var width = lineMetrics.Size.Width;

            var hitTestResult = layout.HitTestPoint(new Point(width, lineMetrics.BaselineOrigin.Y));

            Assert.Equal(0, hitTestResult.TextPosition);

            Assert.Equal(2, hitTestResult.Length);
        }

        [Theory]
        [InlineData("abcde\r\n")]
        [InlineData("abcde\n\r")]
        public void ShouldBreakWithBreakCharPair(string text)
        {
            var layout = new SKTextLayout(
                text,
                SKTypeface.Default,
                12.0f,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(2, layout.TextLines.Count);

            Assert.Equal(1, layout.TextLines[0].TextRuns.Count);

            Assert.Equal(6, layout.TextLines[0].TextRuns[0].GlyphRun.GlyphClusters.Count);

            Assert.Equal(2, layout.TextLines[0].TextRuns[0].GlyphRun.GlyphClusters[5].Length);
        }

        [Theory]
        [InlineData("\r\r", 3)]
        [InlineData("\n\n", 3)]
        [InlineData("abcde\r\n", 2)]
        [InlineData("abcde\n\r", 2)]
        [InlineData("abcde\r\nabcde\r\n", 3)]
        [InlineData("abcde\n\rabcde\r\n", 3)]
        [InlineData("abcde\r\nabcde\r\nabcde\r\n", 4)]
        [InlineData("abcde\n\rabcde\r\nabcde\r\n", 4)]
        public void ShouldBreak(string text, int numberOfLines)
        {
            var breaker = LineBreakIterator.Create(text.AsSpan());

            var lines = breaker.ToArray();

            Assert.Equal(numberOfLines, lines.Length);
        }

        [Theory]
        [InlineData("0123", 1)]
        [InlineData("\r\n", 1)]
        [InlineData("👍b", 2)]
        [InlineData("a👍b", 3)]
        [InlineData("a👍子b", 4)]
        public void ShouldProduceRuns(string text, int numberOfRuns)
        {
            var breaker = TextRunIterator.Create(text.AsSpan(), SKTypeface.Default, new SKTextPointer(0, text.Length));

            var runs = breaker.ToArray();

            Assert.Equal(numberOfRuns, runs.Length);
        }
    }
}
