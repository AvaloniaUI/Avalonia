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
            var result = MediaQueryGrammar.Parse("min-width:100");

            Assert.Equal(
                new[] { new MediaQueryGrammar.WidthSyntax()
                {
                    Value = 100,
                    Operator = Styling.QueryComparisonOperator.GreaterThanOrEquals
                }, },
                result);

            result = MediaQueryGrammar.Parse("max-width:100");

            Assert.Equal(
                new[] { new MediaQueryGrammar.WidthSyntax()
                {
                    Value = 100,
                    Operator = Styling.QueryComparisonOperator.LessThanOrEquals
                }, },
                result);
        }

        [Fact]
        public void Height()
        {
            var result = MediaQueryGrammar.Parse("min-height:100");

            Assert.Equal(
                new[] { new MediaQueryGrammar.HeightSyntax()
                {
                    Value = 100,
                    Operator = Styling.QueryComparisonOperator.GreaterThanOrEquals
                }, },
                result);

            result = MediaQueryGrammar.Parse("max-height:100");

            Assert.Equal(
                new[] { new MediaQueryGrammar.HeightSyntax()
                {
                    Value = 100,
                    Operator = Styling.QueryComparisonOperator.LessThanOrEquals
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
