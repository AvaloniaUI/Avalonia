using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    internal class SentenceBreakTestDataGenerator : IEnumerable<object[]>
    {
        private readonly List<object[]> _testData;

        public SentenceBreakTestDataGenerator()
        {
            _testData = GenerateTestData();
        }

        public IEnumerator<object[]> GetEnumerator() => _testData.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static List<object[]> GenerateTestData()
        {
            var tests = new List<object[]>();

            var url = Path.Combine(UnicodeDataSource.Ucd, "auxiliary/SentenceBreakTest.txt");

            using var client = new HttpClient();
            using var result = client.GetAsync(url).GetAwaiter().GetResult();

            if (!result.IsSuccessStatusCode)
            {
                return tests;
            }

            using var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);

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

                var (codePoints, breakPoints) = WordBreakEnumeratorTests.WordBreakTestDataGenerator.ReadLineData(segments[0].Trim());

                tests.Add([lineNumber, codePoints, breakPoints, segments.Length > 1 ? segments[1] : ""]);

                lineNumber++;
            }

            return tests;
        }
    }
}
