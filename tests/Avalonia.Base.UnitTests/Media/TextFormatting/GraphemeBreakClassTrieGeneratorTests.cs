using System;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Visuals.UnitTests.Media.TextFormatting;
using Xunit;

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

        [ClassData(typeof(GraphemeBreakTestDataGenerator))]
        [Theory(Skip = "Only run when we update the trie.")]
        public void Should_Enumerate(string line, int lineNumber, string grapheme, string text)
        {
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
                _outputHelper.WriteLine($"Failed line {lineNumber}");
                _outputHelper.WriteLine($"       Text: {text}");
                _outputHelper.WriteLine($"   Grapheme: {grapheme}");
                _outputHelper.WriteLine($"       Line: {line}");

                Assert.True(false);
            }
        }

        [Fact(Skip = "Only run when we update the trie.")]
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

        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Generate_Trie()
        {
            GraphemeBreakClassTrieGenerator.Execute();
        }
    }
}
