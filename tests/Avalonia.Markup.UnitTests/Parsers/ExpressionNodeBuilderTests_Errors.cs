// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class ExpressionObserverBuilderTests_Errors
    {
        [Fact]
        public void Identifier_Cannot_Start_With_Digit()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("1Foo"));
        }

        [Fact]
        public void Identifier_Cannot_Start_With_Symbol()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.%Bar"));
        }

        [Fact]
        public void Expression_Cannot_End_With_Period()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar."));
        }

        [Fact]
        public void Expression_Cannot_Start_With_Period_Then_Token()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse(".Bar"));
        }

        [Fact]
        public void Expression_Cannot_Have_Empty_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar[]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_At_Start_Of_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar[,3,4]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_In_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar[3,,4]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_At_End_Of_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar[3,4,]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Digit_After_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar[3,4]5"));
        }

        [Fact]
        public void Expression_Cannot_Have_Letter_After_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionObserverBuilder.Parse("Foo.Bar[3,4]A"));
        }
    }
}
