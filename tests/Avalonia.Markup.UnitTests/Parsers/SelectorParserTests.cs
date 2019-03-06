using System;
using Avalonia.Controls;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class SelectorParserTests
    {
        [Fact]
        public void Parses_Boolean_Property_Selector()
        {
            var target = new SelectorParser((ns, type) => typeof(TextBlock));
            var result = target.Parse("TextBlock[IsPointerOver=True]");
        }

        [Fact]
        public void Parses_Comma_Separated_Selectors()
        {
            var target = new SelectorParser((ns, type) => typeof(TextBlock));
            var result = target.Parse("TextBlock, TextBlock:foo");
        }

        [Fact]
        public void Throws_If_OfType_Type_Not_Found()
        {
            var target = new SelectorParser((ns, type) => null);
            Assert.Throws<InvalidOperationException>(() => target.Parse("NotFound"));
        }

        [Fact]
        public void Throws_If_Is_Type_Not_Found()
        {
            var target = new SelectorParser((ns, type) => null);
            Assert.Throws<InvalidOperationException>(() => target.Parse(":is(NotFound)"));
        }
    }
}
