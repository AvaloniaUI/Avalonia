using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Base.UnitTests.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    internal class BiDiTestDataGenerator : IEnumerable<BiDiTestData>
    {
        private readonly List<BiDiTestData> _testData;

        public BiDiTestDataGenerator()
        {
            _testData = ReadTestData();
        }

        public IEnumerator<BiDiTestData> GetEnumerator()
        {
            return _testData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static List<BiDiTestData> ReadTestData()
        {
            var testData = new List<BiDiTestData>();

            using (var client = new HttpClient())
            {
                var url = Path.Combine(UnicodeDataGenerator.Ucd, "BidiTest.txt");

                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                        return testData;

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        var lineNumber = 0;

                        // Process each line
                        int[] levels = null;

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            lineNumber++;

                            if (line == null)
                            {
                                break;
                            }

                            if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            // Directive?
                            if (line.StartsWith("@"))
                            {
                                if (line.StartsWith("@Levels:"))
                                {
                                    levels = line.Substring(8).Trim().Split(' ').Where(x => x.Length > 0).Select(x =>
                                    {
                                        if (x == "x")
                                        {
                                            return -1;
                                        }

                                        return int.Parse(x);

                                    }).ToArray();
                                }

                                continue;
                            }

                            // Split data line
                            var parts = line.Split(';');

                            // Get the directions
                            var directions = parts[0].Split(' ').Select(PropertyValueAliasHelper.GetBidiClass)
                                .ToArray();

                            // Get the bit set
                            var bitset = Convert.ToInt32(parts[1].Trim(), 16);

                            for (var bit = 1; bit < 8; bit <<= 1)
                            {
                                if ((bitset & bit) == 0)
                                {
                                    continue;
                                }

                                sbyte paragraphEmbeddingLevel;

                                switch (bit)
                                {
                                    case 1:
                                        paragraphEmbeddingLevel = 2; // Auto
                                        break;

                                    case 2:
                                        paragraphEmbeddingLevel = 0; // LTR
                                        break;

                                    case 4:
                                        paragraphEmbeddingLevel = 1; // RTL
                                        break;

                                    default:
                                        throw new NotSupportedException();
                                }

                                testData.Add(new BiDiTestData
                                {
                                    LineNumber = lineNumber,
                                    Classes = directions,
                                    ParagraphEmbeddingLevel = paragraphEmbeddingLevel,
                                    Levels = levels
                                });

                                break;
                            }
                        }
                    }
                }
            }

            return testData;
        }
    }

    internal class BiDiTestData
    {
        public int LineNumber { get; set; }
        public BidiClass[] Classes { get; set; }
        public sbyte ParagraphEmbeddingLevel { get; set; }
        public int[] Levels { get; set; }
    }
}
