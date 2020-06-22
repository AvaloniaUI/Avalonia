using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    /// <summary>
    ///     This class is intended for use when the Unicode spec changes. Otherwise the containing tests are redundant.
    ///     To update the <c>GraphemeBreak.trie</c> run the <see cref="Should_Generate_Trie"/> test.
    /// </summary>
    public class GraphemeBreakClassTrieGeneratorTests
    {
        [Theory(Skip = "Only run when we update the trie.")]
        [ClassData(typeof(GraphemeEnumeratorTestDataGenerator))]
        public void Should_Enumerate(string text, int expectedLength)
        {
            var enumerator = new GraphemeEnumerator(text.AsMemory());

            Assert.True(enumerator.MoveNext());

            Assert.Equal(expectedLength, enumerator.Current.Text.Length);
        }

        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Enumerate_Other()
        {
            const string text = "ABCDEFGHIJ";

            var enumerator = new GraphemeEnumerator(text.AsMemory());

            var count = 0;

            while (enumerator.MoveNext())
            {
                Assert.Equal(1, enumerator.Current.Text.Length);

                count++;
            }

            Assert.Equal(10, count);
        }

        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Generate_Trie()
        {
            GraphemeBreakClassTrieGenerator.Execute();
        }

        public class GraphemeEnumeratorTestDataGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _testData;

            public GraphemeEnumeratorTestDataGenerator()
            {
                _testData = ReadTestData();
            }

            public IEnumerator<object[]> GetEnumerator()
            {
                return _testData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static List<object[]> ReadTestData()
            {
                var testData = new List<object[]>();

                using (var client = new HttpClient())
                {
                    using (var result = client.GetAsync("https://www.unicode.org/Public/UNIDATA/auxiliary/GraphemeBreakTest.txt").GetAwaiter().GetResult())
                    {
                        if (!result.IsSuccessStatusCode)
                            return testData;

                        using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        using (var reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();

                                if (line == null)
                                {
                                    break;
                                }

                                if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                                {
                                    continue;
                                }

                                var elements = line.Split('#')[0].Replace("÷\t", "÷").Trim('÷').Split('÷');

                                var chars = elements[0].Replace(" × ", " ").Split(' ');

                                var codepoints = chars.Where(x => x != "" && x != "×")
                                    .Select(x => Convert.ToInt32(x, 16)).ToArray();

                                var text = string.Join(null, codepoints.Select(char.ConvertFromUtf32));

                                var length = codepoints.Select(x => x > ushort.MaxValue ? 2 : 1).Sum();

                                var data = new object[] { text, length };

                                testData.Add(data);
                            }
                        }
                    }
                }

                return testData;
            }
        }
    }
}
