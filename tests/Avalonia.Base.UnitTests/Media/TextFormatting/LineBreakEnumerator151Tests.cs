using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class LineBreakEnumerator151Tests
    {
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
