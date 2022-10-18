using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Base.UnitTests.Media.TextFormatting;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    internal class GraphemeBreakTestDataGenerator : IEnumerable<GraphemeBreakData>
    {
        private readonly List<GraphemeBreakData> _testData;

        public GraphemeBreakTestDataGenerator()
        {
            _testData = ReadData();
        }
        
        public IEnumerator<GraphemeBreakData> GetEnumerator()
        {
            return _testData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        private static List<GraphemeBreakData> ReadData()
        {
            var testData = new List<GraphemeBreakData>();

            var url = Path.Combine(UnicodeDataGenerator.Ucd, "auxiliary/GraphemeBreakTest.txt");

            using (var client = new HttpClient())
            using (var result = client.GetAsync(url).GetAwaiter().GetResult())
            {
                if (!result.IsSuccessStatusCode)
                {
                    return testData;
                }

                using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                using (var reader = new StreamReader(stream))
                {
                    var lineNumber = 0;

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

                        var elements = line.Split('#');

                        elements = elements[0].Replace("÷\t", "÷").Trim('÷').Split('÷');

                        var graphemeChars = elements[0].Replace(" × ", " ").Split(' ');

                        var codepoints = graphemeChars.Where(x => x != "" && x != "×")
                            .Select(x => Convert.ToInt32(x, 16)).ToList();

                        var grapheme = codepoints.ToArray();

                        if(elements.Length > 1)
                        {
                            var remainingChars = elements[1].Replace(" × ", " ").Split(' ');

                            var remaining = remainingChars.Where(x => x != "" && x != "×").Select(x => Convert.ToInt32(x, 16)).ToArray();

                            codepoints.AddRange(remaining);
                        }

                        var data = new GraphemeBreakData
                        {
                            Line = line,
                            LineNumber = lineNumber,
                            Grapheme = grapheme,
                            Codepoints = codepoints.ToArray()
                        };

                        testData.Add(data);
                    }
                }
            }
            return testData;
        }       
    }
    
    internal struct GraphemeBreakData
    {
        public string Line { get; set; }
        public int LineNumber { get; set; }
        public int[] Grapheme { get; set; }
        public int[] Codepoints{ get; set; }
    }
}
