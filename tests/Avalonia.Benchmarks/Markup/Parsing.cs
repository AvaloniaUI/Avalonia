using Avalonia.Controls;
using Avalonia.Markup.Parsers;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Benchmarks.Markup
{
    [MemoryDiagnoser]
    public class Parsing
    {
        [Benchmark]
        public void ParseComplexSelector()
        {
            var selectorString = "ListBox > TextBox /template/ TextBlock[IsFocused=True]";
            var parser = new SelectorParser((ns, s) =>
            {
                switch (s)
                {
                    case "ListBox":
                        return typeof(ListBox);
                    case "TextBox":
                        return typeof(TextBox);
                    case "TextBlock":
                        return typeof(TextBlock);
                    default:
                        return null;
                }
            });
            var selector = parser.Parse(selectorString);
        }
    }
}
