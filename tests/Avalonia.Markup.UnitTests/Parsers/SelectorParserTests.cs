using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Parsers;
using Avalonia.Styling;
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
    }
}
