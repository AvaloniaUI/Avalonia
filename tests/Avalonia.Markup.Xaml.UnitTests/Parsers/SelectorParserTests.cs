using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Parsers;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Parsers
{
    public class SelectorParserTests
    {
        [Fact]
        public void Parses_Boolean_Property_Selector()
        {
            var target = new SelectorParser((type, ns) => typeof(TextBlock));
            var result = target.Parse("TextBlock[IsPointerOver=True]");
        }
    }
}
