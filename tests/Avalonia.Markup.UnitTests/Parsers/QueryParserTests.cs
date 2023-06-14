using System;
using Avalonia.Controls;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class QueryParserTests
    {
        [Fact]
        public void Parses_Or_Queries()
        {
            var target = new MediaQueryParser();
            var result = target.Parse("orientation:portrait , width > 0");
        }

        [Fact]
        public void Parses_And_Queries()
        {
            var target = new MediaQueryParser();
            var result = target.Parse("orientation:portrait and width > 0");
        }
    }
}
