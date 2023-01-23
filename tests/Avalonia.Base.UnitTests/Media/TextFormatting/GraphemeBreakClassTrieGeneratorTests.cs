using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Visuals.UnitTests.Media.TextFormatting;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    /// <summary>
    ///     This class is intended for use when the Unicode spec changes. Otherwise the containing tests are redundant.
    ///     To update the <c>GraphemeBreak.trie</c> run the <see cref="Should_Generate_Trie"/> test.
    /// </summary>
    public class GraphemeBreakClassTrieGeneratorTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public GraphemeBreakClassTrieGeneratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact(/*Skip = "Only run when we update the trie."*/)]
        public void Should_Enumerate()
        {
            var generator = new GraphemeBreakTestDataGenerator();

            foreach (var testData in generator)
            {
                Assert.True(Run(testData));
            }
        }

        private bool Run(GraphemeBreakData t)
        {
            var text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(t.Codepoints).ToArray());
            var grapheme = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(t.Grapheme).ToArray()).AsSpan();

            var enumerator = new GraphemeEnumerator(text);

            enumerator.MoveNext(out var g);

            var actual = text.AsSpan(g.Offset, g.Length);

            bool pass = actual.Length == grapheme.Length;

            if (pass)
            {
                for (int i = 0; i < grapheme.Length; i++)
                {
                    var a = grapheme[i];
                    var b = actual[i];
                
                    if (a != b)
                    {
                        pass = false;

                        break;
                    }
                }
            }      

            if (!pass)
            {
                _outputHelper.WriteLine($"Failed line {t.LineNumber}");
                _outputHelper.WriteLine($"       Text: {text}");
                _outputHelper.WriteLine($" Codepoints: {string.Join(" ", t.Codepoints)}");
                _outputHelper.WriteLine($"   Grapheme: {string.Join(" ", t.Grapheme)}");
                _outputHelper.WriteLine($"       Line: {t.Line}");

                return false;
            }

          
            return true;
        }

        [Fact(/*Skip = "Only run when we update the trie."*/)]
        public void Should_Enumerate_Other()
        {
            const string text = "ABCDEFGHIJ";

            var enumerator = new GraphemeEnumerator(text);

            var count = 0;

            while (enumerator.MoveNext(out var grapheme))
            {
                Assert.Equal(1, grapheme.Length);

                count++;
            }

            Assert.Equal(10, count);
        }

        [Fact(/*Skip = "Only run when we update the trie."*/)]
        public void Should_Generate_Trie()
        {
            GraphemeBreakClassTrieGenerator.Execute();
        }
    }
}
