using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class QueryGrammarTests
    {
        [Fact]
        public void Width()
        {
            var result = MediaQueryGrammar.Parse("width >= 100");

            Assert.Equal(
                new[] { new MediaQueryGrammar.WidthSyntax()
                {
                    Right = 100,
                    RightOperator = Styling.QueryComparisonOperator.GreaterThanOrEquals
                }, },
                result);

            result = MediaQueryGrammar.Parse("100 <= width");

            Assert.Equal(
                new[] { new MediaQueryGrammar.WidthSyntax()
                {
                    Left = 100,
                    LeftOperator = Styling.QueryComparisonOperator.GreaterThanOrEquals
                }, },
                result);

            result = MediaQueryGrammar.Parse("100 <= width < 200");

            Assert.Equal(
                new[] { new MediaQueryGrammar.WidthSyntax()
                {
                    Left = 100,
                    LeftOperator = Styling.QueryComparisonOperator.GreaterThanOrEquals,
                    Right = 200,
                    RightOperator = Styling.QueryComparisonOperator.LessThan
                }, },
                result);
        }

        [Fact]
        public void Height()
        {
            var result = MediaQueryGrammar.Parse("height >= 100");

            Assert.Equal(
                new[] { new MediaQueryGrammar.HeightSyntax()
                {
                    Right = 100,
                    RightOperator = Styling.QueryComparisonOperator.GreaterThanOrEquals
                }, },
                result);

            result = MediaQueryGrammar.Parse("100 <= height");

            Assert.Equal(
                new[] { new MediaQueryGrammar.HeightSyntax()
                {
                    Left = 100,
                    LeftOperator = Styling.QueryComparisonOperator.GreaterThanOrEquals
                }, },
                result);

            result = MediaQueryGrammar.Parse("100 <= height < 200");

            Assert.Equal(
                new[] { new MediaQueryGrammar.HeightSyntax()
                {
                    Left = 100,
                    LeftOperator = Styling.QueryComparisonOperator.GreaterThanOrEquals,
                    Right = 200,
                    RightOperator = Styling.QueryComparisonOperator.LessThan
                }, },
                result);
        }

        [Fact]
        public void Orientation()
        {
            var result = MediaQueryGrammar.Parse("orientation:portrait");

            Assert.Equal(
                new[] { new MediaQueryGrammar.OrientationSyntax()
                {
                    Argument = Styling.MediaOrientation.Portrait
                }, },
                result);

            result = MediaQueryGrammar.Parse("orientation:landscape");

            Assert.Equal(
                new[] { new MediaQueryGrammar.OrientationSyntax()
                {
                    Argument = Styling.MediaOrientation.Landscape
                }, },
                result);
        }
    }
}
