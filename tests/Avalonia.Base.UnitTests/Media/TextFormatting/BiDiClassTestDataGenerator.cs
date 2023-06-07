using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Base.UnitTests.Media.TextFormatting;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    internal class BiDiClassTestDataGenerator : IEnumerable<BiDiClassData>
    {
        private readonly List<BiDiClassData> _testData;

        public BiDiClassTestDataGenerator()
        {
            _testData = ReadData();
        }
        
        public IEnumerator<BiDiClassData> GetEnumerator()
        {
            return _testData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        private static List<BiDiClassData> ReadData()
        {
            var testData = new List<BiDiClassData>();
            
            using (var client = new HttpClient())
            {
                var url = Path.Combine(UnicodeDataGenerator.Ucd, "BidiCharacterTest.txt");

                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                        return testData;

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        var lineNumber = 0;
                        
                        // Process each line
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
                            
                            // Split into fields
                            var fields = line.Split(';');

                            // Parse field 0 - code points
                            var codePoints = fields[0].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x, 16)).ToArray();

                            // Parse field 1 - paragraph level
                            var paragraphLevel = sbyte.Parse(fields[1]);

                            // Parse field 2 - resolved paragraph level
                            var resolvedParagraphLevel = sbyte.Parse(fields[2]);

                            // Parse field 3 - resolved levels
                            var resolvedLevels = fields[3].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x == "x" ? (sbyte)-1 : Convert.ToSByte(x)).ToArray();

                            // Parse field 4 - resolved levels
                            var resolvedOrder = fields[4].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x)).ToArray();

                            testData.Add(new BiDiClassData
                            {
                                LineNumber = lineNumber,
                                CodePoints = codePoints,
                                ParagraphLevel = paragraphLevel,
                                ResolvedParagraphLevel = resolvedParagraphLevel,
                                ResolvedLevels = resolvedLevels,
                                ResolvedOrder = resolvedOrder
                            });
                        }
                    }
                }
            }

            return testData;
        }
        
       
    }
    
    internal struct BiDiClassData
    {
        public int LineNumber { get; set; }
        public int[] CodePoints{ get; set; }
        public sbyte ParagraphLevel{ get; set; }
        public  sbyte ResolvedParagraphLevel{ get; set; }
        public sbyte[] ResolvedLevels{ get; set; }
        public int[] ResolvedOrder{ get; set; }
    }
}
