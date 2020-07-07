using System;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    public class LineBreakerTests
    {
        [Fact]
        public void Should_Split_Text_By_Explicit_Breaks()
        {
            //ABC [0 3]
            //DEF\r[4 7]
            //\r[8]
            //Hello\r\n[9 15]
            const string text = "ABC DEF\r\rHELLO\r\n";

            var buffer = new ReadOnlySlice<char>(text.AsMemory());

            var lineBreaker = new LineBreakEnumerator(buffer);

            var current = 0;

            Assert.True(lineBreaker.MoveNext());

            var a = text.Substring(current, lineBreaker.Current.PositionMeasure - current + 1);

            Assert.Equal("ABC ", a);

            current += a.Length;

            Assert.True(lineBreaker.MoveNext());

            var b = text.Substring(current, lineBreaker.Current.PositionMeasure - current + 1);

            Assert.Equal("DEF\r", b);

            current += b.Length;

            Assert.True(lineBreaker.MoveNext());

            var c = text.Substring(current, lineBreaker.Current.PositionMeasure - current + 1);

            Assert.Equal("\r", c);

            current += c.Length;

            Assert.True(lineBreaker.MoveNext());

            var d = text.Substring(current, text.Length - current);

            Assert.Equal("HELLO\r\n", d);
        }
    }
}
