using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Parsers
{
    public partial class BindingExpressionGrammarTests
    {
        [Fact]
        public void Identifier_Cannot_Start_With_Digit()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("1Foo"));
        }

        [Fact]
        public void Identifier_Cannot_Start_With_Symbol()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.%Bar"));
        }

        [Fact]
        public void Identifier_Cannot_Start_With_QuestionMark()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("?Foo"));
        }

        [Fact]
        public void Identifier_Cannot_Start_With_NullConditional()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("?.Foo"));
        }

        [Fact]
        public void Expression_Cannot_End_With_Period()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar."));
        }
        
        [Fact]
        public void Expression_Cannot_End_With_QuestionMark()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar?"));
        }

        [Fact]
        public void Expression_Cannot_End_With_Null_Conditional()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar?."));
        }

        [Fact]
        public void Expression_Cannot_Start_With_Period_Then_Token()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse(".Bar"));
        }

        [Fact]
        public void Expression_Cannot_Have_Empty_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar[]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_At_Start_Of_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar[,3,4]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_In_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar[3,,4]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_At_End_Of_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar[3,4,]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Digit_After_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar[3,4]5"));
        }

        [Fact]
        public void Expression_Cannot_Have_Letter_After_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => Parse("Foo.Bar[3,4]A"));
        }
    }
}
