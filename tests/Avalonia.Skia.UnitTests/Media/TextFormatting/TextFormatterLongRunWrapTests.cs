#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextFormatterLongRunWrapTests
    {
        private const double ParagraphWidth = 200;

        [Fact]
        public void Wrapping_The_Start_Of_A_Long_Unbroken_Run_Does_Not_Scan_The_Whole_Run_For_Every_Line()
        {
            using (TextFormatterTests.Start())
            {
                var text = new string('x', 900_000);
                var properties = CreateParagraph(TextWrapping.Wrap, out var runProperties);
                var source = new SingleBufferTextSource(text, runProperties);
                var formatter = new TextFormatterImpl();

                var timer = Stopwatch.StartNew();
                var position = 0;
                TextLineBreak? previous = null;

                for (var i = 0; i < 100; i++)
                {
                    using var line = formatter.FormatLine(source, position, ParagraphWidth, properties, previous);

                    Assert.NotNull(line);
                    Assert.True(line!.Length > 0);
                    Assert.True(line.Width <= ParagraphWidth + 1);

                    position += line.Length;
                    previous = line.TextLineBreak;
                }

                // Scanning the remaining run for every line takes several seconds for 100 lines.
                Assert.True(timer.Elapsed.TotalSeconds < 3, $"Wrapping 100 lines took {timer.Elapsed}.");
            }
        }

        [Fact]
        public void WrapWithOverflow_Breaks_A_Word_Without_Nearby_Opportunity_At_The_Fitting_Length()
        {
            using (TextFormatterTests.Start())
            {
                var text = "ab " + new string('x', 20_000) + " cd";

                var overflow = WrapAll(text, TextWrapping.WrapWithOverflow);
                var wrap = WrapAll(text, TextWrapping.Wrap);

                Assert.Equal(text.Length, overflow.Sum());

                // Once the end of the word is within reach it overflows like any short word.
                var start = 0;
                var index = 0;

                while (start + 8_000 < text.Length - 3)
                {
                    Assert.Equal(wrap[index], overflow[index]);

                    start += wrap[index++];
                }

                Assert.True(index > 100);
            }
        }

        [Fact]
        public void WrapWithOverflow_Still_Overflows_A_Short_Word()
        {
            using (TextFormatterTests.Start())
            {
                var word = new string('x', 100);
                var text = "ab " + word + " cd";

                var lengths = WrapAll(text, TextWrapping.WrapWithOverflow, width: 40);

                Assert.Equal(text.Length, lengths.Sum());
                Assert.Contains(lengths, length => length >= word.Length);
            }
        }

        [Fact]
        public void Wrap_Honors_A_Required_Break_Inside_A_Long_Run()
        {
            using (TextFormatterTests.Start())
            {
                var text = new string('x', 20_000) + "\n" + new string('y', 20_000);

                var lengths = WrapAll(text, TextWrapping.Wrap);

                Assert.Equal(text.Length, lengths.Sum());

                var newline = 0;

                foreach (var length in lengths)
                {
                    newline += length;

                    if (newline == 20_001)
                    {
                        return;
                    }
                }

                Assert.Fail("No line ends right after the required break.");
            }
        }

        private static GenericTextParagraphProperties CreateParagraph(TextWrapping wrapping,
            out GenericTextRunProperties runProperties)
        {
            runProperties = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

            return new GenericTextParagraphProperties(runProperties, textWrapping: wrapping);
        }

        private static List<int> WrapAll(string text, TextWrapping wrapping, double width = ParagraphWidth)
        {
            var properties = CreateParagraph(wrapping, out var runProperties);
            var source = new SingleBufferTextSource(text, runProperties);
            var formatter = new TextFormatterImpl();
            var lengths = new List<int>();
            var position = 0;
            TextLineBreak? previous = null;

            while (position < text.Length)
            {
                using var line = formatter.FormatLine(source, position, width, properties, previous);

                Assert.NotNull(line);
                Assert.True(line!.Length > 0);

                lengths.Add(line.Length);
                position += line.Length;
                previous = line.TextLineBreak;
            }

            return lengths;
        }
    }
}
