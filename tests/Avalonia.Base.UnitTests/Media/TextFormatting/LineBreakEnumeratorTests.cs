using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;
using static Avalonia.Media.TextFormatting.Unicode.LineBreakEnumerator;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class LineBreakEnumeratorTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public LineBreakEnumeratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void BasicLatinTest()
        {
            var lineBreaker = new LineBreakEnumerator("Hello World\r\nThis is a test.");
            LineBreak lineBreak;

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(6, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(13, lineBreak.PositionWrap);
            Assert.True(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(18, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(21, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(23, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.True(lineBreaker.MoveNext(out lineBreak));
            Assert.Equal(28, lineBreak.PositionWrap);
            Assert.False(lineBreak.Required);

            Assert.False(lineBreaker.MoveNext(out lineBreak));
        }

        [InlineData("Hello\nWorld", 5, 6)]
        [InlineData("Hello\rWorld", 5, 6 )]
        [InlineData("Hello\r\nWorld", 5 , 7)]
        [Theory]
        public void ShouldFindMandatoryBreaks(string text, int positionMeasure, int positionWrap)
        {
            var lineBreaker = new LineBreakEnumerator(text);
            
            var breaks = GetBreaks(lineBreaker);

            Assert.Equal(2, breaks.Count);

            var firstBreak = breaks[0];

            Assert.True(firstBreak.Required);

            Assert.Equal(positionMeasure, firstBreak.PositionMeasure);

            Assert.Equal(positionWrap, firstBreak.PositionWrap);
        }

        [Fact]
        public void ForwardTextWithOuterWhitespace()
        {
            var lineBreaker = new LineBreakEnumerator(" Apples Pears Bananas   ");
            var positionsF = GetBreaks(lineBreaker);
            Assert.Equal(1, positionsF[0].PositionWrap);
            Assert.Equal(0, positionsF[0].PositionMeasure);
            Assert.Equal(8, positionsF[1].PositionWrap);
            Assert.Equal(7, positionsF[1].PositionMeasure);
            Assert.Equal(14, positionsF[2].PositionWrap);
            Assert.Equal(13, positionsF[2].PositionMeasure);
            Assert.Equal(24, positionsF[3].PositionWrap);
            Assert.Equal(21, positionsF[3].PositionMeasure);
        }

        [Theory(Skip = "Only runs when the spec changes")]
        [ClassData(typeof(LineBreakTestDataGenerator))]
        public void ShouldFindBreaks(int lineNumber, int[] codePoints, int[] breakPoints, string rules)
        {
            var text = string.Join(null, codePoints.Select(char.ConvertFromUtf32));

            var lineBreaker = new LineBreakEnumerator(text);

            var foundBreaks = new List<int>();

            while (lineBreaker.MoveNext(out var lineBreak))
            {
                foundBreaks.Add(lineBreak.PositionWrap);
            }

            // Check the same
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
                _outputHelper.WriteLine($"     Char Props: {string.Join(" ", codePoints.Select(x => new Codepoint((uint)x).LineBreakClass))}");
                _outputHelper.WriteLine($"     Rules: {rules}");
                _outputHelper.WriteLine("");
            }

            Assert.True(pass);
        }

        private static List<LineBreak> GetBreaks(LineBreakEnumerator lineBreaker)
        {
            var breaks = new List<LineBreak>();

            while (lineBreaker.MoveNext(out var lineBreak))
            {
                breaks.Add(lineBreak);
            }

            return breaks;
        }

        public class LineBreakTestDataGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _testData;

            public LineBreakTestDataGenerator()
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
                // Process each line
                var tests = new List<object[]>();

                // Read the test file
                var url = Path.Combine(UnicodeDataGenerator.Ucd, "auxiliary/LineBreakTest.txt");

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



                            // Get the line, remove comments
                            var segments = line.Split('#');

                            // Ignore blank/comment only lines
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

                // Parse the test
                var p = 0;

                while (p < line.Length)
                {
                    // Ignore white space
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
                        breakPoints.Add(codePoints.Select(x => x > ushort.MaxValue ? 2 : 1).Sum());
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
