// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Perspex.Markup.Binding;
using Xunit;

namespace Perspex.Markup.UnitTests.Binding
{
    public class ExpressionNodeBuilderTests
    {
        [Fact]
        public void Should_Build_Single_Property()
        {
            var result = ToList(ExpressionNodeBuilder.Build("Foo"));

            Assert.Equal(1, result.Count);
            Assert.IsType<PropertyAccessorNode>(result[0]);
        }

        [Fact]
        public void Should_Build_Property_Chain()
        {
            var result = ToList(ExpressionNodeBuilder.Build("Foo.Bar.Baz"));

            Assert.Equal(3, result.Count);
            Assert.IsType<PropertyAccessorNode>(result[0]);
        }

        private List<ExpressionNode> ToList(ExpressionNode node)
        {
            var result = new List<ExpressionNode>();
            
            while (node != null)
            {
                result.Add(node);
                node = node.Next;
            }

            return result;
        }
    }
}
