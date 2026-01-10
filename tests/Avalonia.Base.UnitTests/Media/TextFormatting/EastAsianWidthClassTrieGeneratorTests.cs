using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class EastAsianWidthClassTrieGeneratorTests
    {
        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Generate()
        {
            UnicodeEnumsGenerator.CreateEastAsianWidthClassEnum();

            var trie = EastAsianWidthClassTrieGenerator.Execute(out var values);

            foreach (var (start, end, value) in values)
            {
                var expected = (uint)value;
                var actual = trie.Get(start);

                Assert.Equal(expected, actual);
            }
        }
    }
}
