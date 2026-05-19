using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class WordBreakEnumeratorTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public WordBreakEnumeratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory(Skip = "Only run when we update Unicode data.")]
        [ClassData(typeof(WordBreakTestDataGenerator))]
        public void ShouldFindBreaks(int lineNumber, int[] codePoints, int[] breakPoints, string rules)
        {
            var text = string.Join(null, codePoints.Select(char.ConvertFromUtf32));

            var wordBreaker = new WordBreakEnumerator(text);

            var foundBreaks = new List<int> { 0 };
            var currentPosition = 0;

            while (wordBreaker.MoveNext(out var segment))
            {
                currentPosition += segment.CodepointLength;
                foundBreaks.Add(currentPosition);
            }

            var pass = true;

            if (foundBreaks.Count != breakPoints.Length)
            {
                pass = false;
            }
            else
            {
                for (var i = 0; i < foundBreaks.Count; i++)
                {
                    if (foundBreaks[i] != breakPoints[i])
                    {
                        pass = false;
                    }
                }
            }

            if (!pass)
            {
                _outputHelper.WriteLine($"Failed test on line {lineNumber}");
                _outputHelper.WriteLine("");
                _outputHelper.WriteLine($"    Code Points: {string.Join(" ", codePoints)}");
                _outputHelper.WriteLine($"Expected Breaks: {string.Join(" ", breakPoints)}");
                _outputHelper.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                _outputHelper.WriteLine($"           Text: {text}");
                _outputHelper.WriteLine($"     Char Props: {string.Join(" ", codePoints.Select(x => new Codepoint((uint)x).WordBreakClass))}");
                _outputHelper.WriteLine($"     Rules: {rules}");
                _outputHelper.WriteLine("");
            }

            Assert.True(pass);
        }

        public class WordBreakTestDataGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _testData;

            public WordBreakTestDataGenerator()
            {
                _testData = GenerateTestData();
            }

            public IEnumerator<object[]> GetEnumerator()
            {
                return _testData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static List<object[]> GenerateTestData()
            {
                var tests = new List<object[]>();

                var url = Path.Combine(UnicodeDataGenerator.Ucd, "auxiliary/WordBreakTest.txt");

                using (var client = new HttpClient())
                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        return tests;
                    }

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        var lineNumber = 1;

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            if (line is null)
                            {
                                break;
                            }

                            var segments = line.Split('#');

                            if (string.IsNullOrWhiteSpace(segments[0]))
                            {
                                lineNumber++;
                                continue;
                            }

                            var lineData = ReadLineData(segments[0].Trim());

                            tests.Add([lineNumber, lineData.Item1, lineData.Item2, segments[1]]);

                            lineNumber++;
                        }
                    }
                }

                return tests;
            }

            public static (int[], int[]) ReadLineData(string line)
            {
                var codePoints = new List<int>();
                var breakPoints = new List<int>();
                var p = 0;

                while (p < line.Length)
                {
                    if (char.IsWhiteSpace(line[p]))
                    {
                        p++;
                        continue;
                    }

                    if (line[p] == '×')
                    {
                        p++;
                        continue;
                    }

                    if (line[p] == '÷')
                    {
                        breakPoints.Add(codePoints.Count);
                        p++;
                        continue;
                    }

                    var codePointPos = p;

                    while (p < line.Length && IsHexDigit(line[p]))
                    {
                        p++;
                    }

                    var codePointStr = line.Substring(codePointPos, p - codePointPos);
                    var codePoint = Convert.ToInt32(codePointStr, 16);

                    codePoints.Add(codePoint);
                }

                return (codePoints.ToArray(), breakPoints.ToArray());
            }

            private static bool IsHexDigit(char ch)
            {
                return char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
            }
        }
    }
}
