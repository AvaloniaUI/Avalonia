using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextLayoutIndentTests
    {
        private const string WrappingText = "aaaa bbbb cccc dddd eeee ffff gggg hhhh iiii jjjj";
        private const double Indent = 20;
        private const int Precision = 4;

        [Fact]
        public void First_Line_Starts_At_Indent_And_Following_Lines_At_Leading_Edge()
        {
            using (Start())
            {
                var layout = CreateWrappingLayout(Indent, out _);

                Assert.True(layout.TextLines.Count > 2);
                Assert.Equal(Indent, layout.TextLines[0].Start, Precision);

                for (var i = 1; i < layout.TextLines.Count; i++)
                {
                    Assert.Equal(0, layout.TextLines[i].Start, Precision);
                }
            }
        }

        [Fact]
        public void First_Line_Wraps_Against_Width_Minus_Indent()
        {
            using (Start())
            {
                var unindented = CreateWrappingLayout(0, out var maxWidth);
                var indent = maxWidth / 2;
                var indented = CreateWrappingLayout(indent, out _);

                var firstLine = indented.TextLines[0];

                Assert.True(firstLine.Length < unindented.TextLines[0].Length);
                Assert.False(firstLine.HasOverflowed);
                Assert.True(firstLine.Start + firstLine.Width <= maxWidth);
            }
        }

        [Fact]
        public void ParagraphIndent_Offsets_Every_Line()
        {
            using (Start())
            {
                var layout = CreateWrappingLayout(0, out _, paragraphIndent: 15);

                Assert.True(layout.TextLines.Count > 2);

                foreach (var line in layout.TextLines)
                {
                    Assert.Equal(15, line.Start, Precision);
                }
            }
        }

        [Fact]
        public void Negative_Indent_Hangs_First_Line_Before_Paragraph_Indent()
        {
            using (Start())
            {
                var layout = CreateWrappingLayout(-Indent, out _, paragraphIndent: Indent);

                Assert.Equal(0, layout.TextLines[0].Start, Precision);

                for (var i = 1; i < layout.TextLines.Count; i++)
                {
                    Assert.Equal(Indent, layout.TextLines[i].Start, Precision);
                }
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Line_After_Hard_Line_Break_Is_Not_Indented(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("aaaa\rbbbb", Indent, 500, textWrapping);

                Assert.Equal(2, layout.TextLines.Count);
                Assert.Equal(Indent, layout.TextLines[0].Start, Precision);
                Assert.Equal(0, layout.TextLines[1].Start, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Empty_Line_After_Trailing_Line_Break_Is_Not_Indented(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("aaaa\r", Indent, 500, textWrapping);

                Assert.Equal(2, layout.TextLines.Count);
                Assert.Equal(Indent, layout.TextLines[0].Start, Precision);
                Assert.Equal(0, layout.TextLines[1].Start, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Indent_Is_Ignored_When_Not_First_Line_In_Paragraph(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("aaaa", Indent, 500, textWrapping, firstLineInParagraph: false);

                Assert.Equal(0, layout.TextLines[0].Start, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Empty_Paragraph_Places_Caret_At_Indent(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("", Indent, 500, textWrapping);

                Assert.Equal(Indent, layout.HitTestTextPosition(0).X, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Hit_Test_Inside_Indent_Maps_To_First_Character(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("aaaa bbbb", Indent, 500, textWrapping);

                var result = layout.HitTestPoint(new Point(Indent / 2, layout.TextLines[0].Height / 2));

                Assert.Equal(0, result.TextPosition);
                Assert.Equal(Indent, layout.HitTestTextPosition(0).X, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Right_To_Left_Paragraph_Is_Indented_From_The_Right(TextWrapping textWrapping)
        {
            using (Start())
            {
                const double maxWidth = 500;

                var layout = CreateLayout("abcd", Indent, maxWidth, textWrapping, flowDirection: FlowDirection.RightToLeft,
                    textAlignment: TextAlignment.Start);

                var line = layout.TextLines[0];

                Assert.Equal(maxWidth - Indent, line.Start + line.WidthIncludingTrailingWhitespace, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Centered_First_Line_Is_Centered_In_Remaining_Width(TextWrapping textWrapping)
        {
            using (Start())
            {
                const double maxWidth = 500;

                var layout = CreateLayout("abcd", Indent, maxWidth, textWrapping, textAlignment: TextAlignment.Center);

                var line = layout.TextLines[0];

                Assert.Equal(Indent + (maxWidth - Indent - line.Width) / 2, line.Start, Precision);
            }
        }

        [Fact]
        public void Justified_First_Line_Ends_At_Margin()
        {
            using (Start())
            {
                var layout = CreateWrappingLayout(Indent, out var maxWidth, textAlignment: TextAlignment.Justify);

                var firstLine = layout.TextLines[0];

                Assert.Equal(Indent, firstLine.Start, Precision);
                Assert.Equal(maxWidth, firstLine.Start + firstLine.Width, 2);
            }
        }

        [Fact]
        public void Trimmed_First_Line_Fits_Remaining_Width()
        {
            using (Start())
            {
                var unwrapped = CreateLayout(WrappingText, 0, double.PositiveInfinity, TextWrapping.NoWrap);
                var maxWidth = unwrapped.Width / 2;

                var layout = CreateLayout(WrappingText, Indent, maxWidth, TextWrapping.NoWrap,
                    textTrimming: TextTrimming.CharacterEllipsis);

                var line = layout.TextLines[0];

                Assert.True(line.HasCollapsed);
                Assert.True(line.Start + line.Width <= maxWidth);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Layout_Width_Includes_Indent(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("abcd", Indent, double.PositiveInfinity, textWrapping);

                var line = layout.TextLines[0];

                Assert.Equal(Indent, line.Start, Precision);
                Assert.Equal(Indent + line.Width, layout.Width, Precision);
                Assert.Equal(Indent + line.WidthIncludingTrailingWhitespace, layout.WidthIncludingTrailingWhitespace,
                    Precision);
            }
        }

        [Fact]
        public void Unwrapped_First_Line_Overflows_Against_Width_Minus_Indent()
        {
            using (Start())
            {
                var natural = CreateLayout("abcd", 0, double.PositiveInfinity, TextWrapping.NoWrap).TextLines[0].Width;

                // The line fits the paragraph width but not the width left after the indent.
                var maxWidth = natural + Indent / 2;

                var unindented = CreateLayout("abcd", 0, maxWidth, TextWrapping.NoWrap);
                var indented = CreateLayout("abcd", Indent, maxWidth, TextWrapping.NoWrap);

                Assert.False(unindented.TextLines[0].HasOverflowed);
                Assert.True(indented.TextLines[0].HasOverflowed);
                Assert.Equal(Indent, indented.TextLines[0].Start, Precision);
                Assert.Equal(1, indented.TextLines.Count);
            }
        }

        [Fact]
        public void Unwrapped_Lines_Take_ParagraphIndent_And_Only_The_First_Takes_Indent()
        {
            using (Start())
            {
                var layout = CreateLayout("aaaa\rbbbb\rcccc", Indent, 500, TextWrapping.NoWrap, paragraphIndent: 15);

                Assert.Equal(3, layout.TextLines.Count);
                Assert.Equal(15 + Indent, layout.TextLines[0].Start, Precision);
                Assert.Equal(15, layout.TextLines[1].Start, Precision);
                Assert.Equal(15, layout.TextLines[2].Start, Precision);
            }
        }

        [Theory]
        [InlineData(TextWrapping.Wrap)]
        [InlineData(TextWrapping.NoWrap)]
        public void Zero_Width_Layout_Ignores_Indent(TextWrapping textWrapping)
        {
            using (Start())
            {
                var layout = CreateLayout("abcd", Indent, 0, textWrapping, paragraphIndent: 15);

                Assert.Equal(0, layout.TextLines[0].Start, Precision);
                Assert.Equal(0, layout.Width, Precision);
            }
        }

        [Fact]
        public void Line_Ended_By_A_Newline_Character_Reports_A_Line_Break()
        {
            using (Start())
            {
                var runProperties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
                var paragraphProperties = new GenericTextParagraphProperties(runProperties, textWrapping: TextWrapping.Wrap);

                using var textLine = TextFormatter.Current.FormatLine(
                    new SingleBufferTextSource("aaaa\rbbbb", runProperties), 0, 500, paragraphProperties);

                Assert.NotNull(textLine);
                Assert.True(textLine.NewLineLength > 0);

                var lineBreak = Assert.IsType<TextLineBreak>(textLine.TextLineBreak);

                Assert.Null(lineBreak.TextEndOfLine);
                Assert.False(lineBreak.IsSplit);
            }
        }

        [Fact]
        public void Formatting_Line_By_Line_Indents_Only_The_First_Line()
        {
            using (Start())
            {
                var runProperties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
                var paragraphProperties = new IndentedParagraphProperties(runProperties, FlowDirection.LeftToRight,
                    TextAlignment.Left, TextWrapping.Wrap, Indent, 0, true);
                var textSource = new SingleBufferTextSource("aaaa bbbb cccc\rdddd eeee\rffff", runProperties);

                var starts = new List<double>();
                var textSourceIndex = 0;
                TextLineBreak? previousLineBreak = null;

                while (TextFormatter.Current.FormatLine(textSource, textSourceIndex, 60, paragraphProperties,
                           previousLineBreak) is { } textLine)
                {
                    starts.Add(textLine.Start);
                    textSourceIndex += textLine.Length;
                    previousLineBreak = textLine.TextLineBreak;
                    textLine.Dispose();
                }

                Assert.True(starts.Count > 3);
                Assert.Equal(Indent, starts[0], Precision);

                for (var i = 1; i < starts.Count; i++)
                {
                    Assert.Equal(0, starts[i], Precision);
                }
            }
        }

        private static TextLayout CreateWrappingLayout(double indent, out double maxWidth,
            double paragraphIndent = 0, TextAlignment textAlignment = TextAlignment.Left)
        {
            var unwrapped = CreateLayout(WrappingText, 0, double.PositiveInfinity, TextWrapping.NoWrap);

            maxWidth = unwrapped.Width / 3;

            return CreateLayout(WrappingText, indent, maxWidth, TextWrapping.Wrap, paragraphIndent: paragraphIndent,
                textAlignment: textAlignment);
        }

        private static TextLayout CreateLayout(string text, double indent, double maxWidth, TextWrapping textWrapping,
            double paragraphIndent = 0,
            FlowDirection flowDirection = FlowDirection.LeftToRight,
            TextAlignment textAlignment = TextAlignment.Left,
            TextTrimming? textTrimming = null,
            bool firstLineInParagraph = true)
        {
            var runProperties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

            var paragraphProperties = new IndentedParagraphProperties(runProperties, flowDirection, textAlignment,
                textWrapping, indent, paragraphIndent, firstLineInParagraph);

            return new TextLayout(new SingleBufferTextSource(text, runProperties), paragraphProperties,
                textTrimming, maxWidth);
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));
        }

        private sealed class IndentedParagraphProperties : TextParagraphProperties
        {
            public IndentedParagraphProperties(TextRunProperties defaultTextRunProperties, FlowDirection flowDirection,
                TextAlignment textAlignment, TextWrapping textWrapping, double indent, double paragraphIndent,
                bool firstLineInParagraph)
            {
                DefaultTextRunProperties = defaultTextRunProperties;
                FlowDirection = flowDirection;
                TextAlignment = textAlignment;
                TextWrapping = textWrapping;
                Indent = indent;
                ParagraphIndent = paragraphIndent;
                FirstLineInParagraph = firstLineInParagraph;
            }

            public override FlowDirection FlowDirection { get; }

            public override TextAlignment TextAlignment { get; }

            public override double LineHeight => double.NaN;

            public override bool FirstLineInParagraph { get; }

            public override TextRunProperties DefaultTextRunProperties { get; }

            public override TextWrapping TextWrapping { get; }

            public override double Indent { get; }

            public override double ParagraphIndent { get; }
        }
    }
}
