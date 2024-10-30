using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class LineBreakEnumerator151Tests
    {
        private readonly ITestOutputHelper _outputHelper;

        public LineBreakEnumerator151Tests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void BasicLatinTest()
        {
            var lineBreaker = new LineBreakEnumeratorV151("Hello World\r\nThis is a test.");
            LineBreak lineBreak;

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(6, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(13, lineBreak.PositionWrap);
            Assert.True(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(18, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(21, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(23, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(28, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.False(lineBreaker.MoveNext(out lineBreak));
        }

        [InlineData("Hello\nWorld", 5, 6)]
        [InlineData("Hello\rWorld", 5, 6 )]
        [InlineData("Hello\r\nWorld", 5 , 7)]
        [Theory]
        public void ShouldFindMandatoryBreaks(string text, int positionMeasure, int positionWrap)
        {
            var lineBreaker = new LineBreakEnumeratorV151(text);
            
            var breaks = GetBreaks(lineBreaker);

            Assert.Equal(2, breaks.Count);

            var firstBreak = breaks[0];

            Assert.True(firstBreak.Required);

            Assert.Equal(positionMeasure, firstBreak.PositionMeasure);

            Assert.Equal(positionWrap, firstBreak.PositionWrap);
        }

        [Fact]
        public void ForwardTextWithOuterWhitespace()
        {
            var lineBreaker = new LineBreakEnumeratorV151(" Apples Pears Bananas   ");
            var positionsF = GetBreaks(lineBreaker);
            Assert.Equal(1, positionsF[0].PositionWrap);
            Assert.Equal(0, positionsF[0].PositionMeasure);
            Assert.Equal(8, positionsF[1].PositionWrap);
            Assert.Equal(7, positionsF[1].PositionMeasure);
            Assert.Equal(14, positionsF[2].PositionWrap);
            Assert.Equal(13, positionsF[2].PositionMeasure);
            Assert.Equal(24, positionsF[3].PositionWrap);
            Assert.Equal(21, positionsF[3].PositionMeasure);
        }

        [Theory]
        [ClassData(typeof(LineBreakTestDataGenerator))]
        public void ShouldFindBreaks(int lineNumber, int[] codePoints, int[] breakPoints)
        {
            var text = string.Join(null, codePoints.Select(char.ConvertFromUtf32));

            var lineBreaker = new LineBreakEnumeratorV151(text);

            var foundBreaks = new List<int>();

            while (lineBreaker.MoveNext(out var lineBreak))
            {
                foundBreaks.Add(lineBreak.PositionWrap);
            }

            // Check the same
            var pass = true;

            if (foundBreaks.Count != breakPoints.Length)
            {
                pass = false;
            }
            else
            {
                for (var i = 0; i < foundBreaks.Count; i++)
                {
                    if (foundBreaks[i] != breakPoints[i])
                    {
                        pass = false;
                    }
                }
            }

            if (!pass)
            {
                _outputHelper.WriteLine($"Failed test on line {lineNumber}");
                _outputHelper.WriteLine("");
                _outputHelper.WriteLine($"    Code Points: {string.Join(" ", codePoints)}");
                _outputHelper.WriteLine($"Expected Breaks: {string.Join(" ", breakPoints)}");
                _outputHelper.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                _outputHelper.WriteLine($"           Text: {text}");
                _outputHelper.WriteLine($"     Char Props: {string.Join(" ", codePoints.Select(x => new Codepoint((uint)x).LineBreakClass))}");
                _outputHelper.WriteLine("");
            }

            Assert.True(pass);
        }

        private static List<LineBreak> GetBreaks(LineBreakEnumeratorV151 lineBreaker)
        {
            var breaks = new List<LineBreak>();

            while (lineBreaker.MoveNext(out var lineBreak))
            {
                breaks.Add(lineBreak);
            }

            return breaks;
        }
    }
}
