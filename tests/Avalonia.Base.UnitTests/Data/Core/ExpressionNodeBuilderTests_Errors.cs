// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionNodeBuilderTests_Errors
    {
        [Fact]
        public void Identifier_Cannot_Start_With_Digit()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("1Foo"));
        }

        [Fact]
        public void Identifier_Cannot_Start_With_Symbol()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.%Bar"));
        }

        [Fact]
        public void Expression_Cannot_End_With_Period()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar."));
        }

        [Fact]
        public void Expression_Cannot_Have_Empty_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar[]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_At_Start_Of_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar[,3,4]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_In_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar[3,,4]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Extra_Comma_At_End_Of_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar[3,4,]"));
        }

        [Fact]
        public void Expression_Cannot_Have_Digit_After_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar[3,4]5"));
        }

        [Fact]
        public void Expression_Cannot_Have_Letter_After_Indexer()
        {
            Assert.Throws<ExpressionParseException>(
                () => ExpressionNodeBuilder.Build("Foo.Bar[3,4]A"));
        }
    }
}
