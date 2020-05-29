using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    internal static class UnicodeDataGenerator
    {
        public static void Execute()
        {
            var codepoints = new Dictionary<int, UnicodeDataItem>();

            var generalCategoryValues = UnicodeEnumsGenerator.CreateGeneralCategoryEnum();

            var generalCategoryMappings = CreateTagToIndexMappings(generalCategoryValues);

            var generalCategoryData = ReadGeneralCategoryData();

            foreach (var (range, name) in generalCategoryData)
            {
                var generalCategory = generalCategoryMappings[name];

                AddGeneralCategoryRange(codepoints, range, generalCategory);
            }

            var scriptValues = UnicodeEnumsGenerator.CreateScriptEnum();

            var scriptMappings = CreateNameToIndexMappings(scriptValues);

            var scriptData = ReadScriptData();

            foreach (var (range, name) in scriptData)
            {
                var script = scriptMappings[name.Replace("_", "")];

                AddScriptRange(codepoints, range, script);

            }

            var biDiClassValues = UnicodeEnumsGenerator.CreateBiDiClassEnum();

            var biDiClassMappings = CreateTagToIndexMappings(biDiClassValues);

            var biDiData = ReadBiDiData();

            foreach (var (range, name) in biDiData)
            {
                var biDiClass = biDiClassMappings[name];

                AddBiDiClassRange(codepoints, range, biDiClass);
            }

            var lineBreakClassValues = UnicodeEnumsGenerator.CreateLineBreakClassEnum();

            var lineBreakClassMappings = CreateTagToIndexMappings(lineBreakClassValues);

            var lineBreakClassData = ReadLineBreakClassData();

            foreach (var (range, name) in lineBreakClassData)
            {
                var lineBreakClass = lineBreakClassMappings[name];

                AddLineBreakClassRange(codepoints, range, lineBreakClass);
            }

            const int initialValue = ((int)LineBreakClass.Unknown << UnicodeData.LINEBREAK_SHIFT) |
                                      ((int)BiDiClass.LeftToRight << UnicodeData.BIDI_SHIFT) |
                                      ((int)Script.Unknown << UnicodeData.SCRIPT_SHIFT) | (int)GeneralCategory.Other;

            var builder = new UnicodeTrieBuilder(initialValue);

            foreach (var properties in codepoints.Values)
            {
                //[line break]|[biDi]|[script]|[category]
                var value = (properties.LineBreakClass << UnicodeData.LINEBREAK_SHIFT) |
                            (properties.BiDiClass << UnicodeData.BIDI_SHIFT) |
                            (properties.Script << UnicodeData.SCRIPT_SHIFT) | properties.GeneralCategory;

                builder.Set(properties.Codepoint, (uint)value);
            }

            using (var stream = File.Create("Generated\\UnicodeData.trie"))
            {
                var trie = builder.Freeze();

                trie.Save(stream);
            }
        }

        private static Dictionary<string, int> CreateTagToIndexMappings(List<(string name, string tag, string comment)> values)
        {
            var mappings = new Dictionary<string, int>();

            for (var i = 0; i < values.Count; i++)
            {
                mappings.Add(values[i].tag, i);
            }

            return mappings;
        }

        private static Dictionary<string, int> CreateNameToIndexMappings(List<(string name, string tag, string comment)> values)
        {
            var mappings = new Dictionary<string, int>();

            for (var i = 0; i < values.Count; i++)
            {
                mappings.Add(values[i].name, i);
            }

            return mappings;
        }

        private static void AddGeneralCategoryRange(Dictionary<int, UnicodeDataItem> codepoints, CodepointRange range,
            int generalCategory)
        {
            for (var i = range.Start; i <= range.End; i++)
            {
                if (!codepoints.ContainsKey(i))
                {
                    codepoints.Add(i, new UnicodeDataItem { Codepoint = i, GeneralCategory = generalCategory });
                }
                else
                {
                    codepoints[i].GeneralCategory = generalCategory;
                }
            }
        }

        private static void AddScriptRange(Dictionary<int, UnicodeDataItem> codepoints, CodepointRange range,
            int script)
        {
            for (var i = range.Start; i <= range.End; i++)
            {
                if (!codepoints.ContainsKey(i))
                {
                    codepoints.Add(i, new UnicodeDataItem { Codepoint = i, Script = script });
                }
                else
                {
                    codepoints[i].Script = script;
                }
            }
        }

        private static void AddBiDiClassRange(Dictionary<int, UnicodeDataItem> codepoints, CodepointRange range,
            int biDiClass)
        {
            for (var i = range.Start; i <= range.End; i++)
            {
                if (!codepoints.ContainsKey(i))
                {
                    codepoints.Add(i, new UnicodeDataItem { Codepoint = i, BiDiClass = biDiClass });
                }
                else
                {
                    codepoints[i].BiDiClass = biDiClass;
                }
            }
        }

        private static void AddLineBreakClassRange(Dictionary<int, UnicodeDataItem> codepoints, CodepointRange range,
            int lineBreakClass)
        {
            for (var i = range.Start; i <= range.End; i++)
            {
                if (!codepoints.ContainsKey(i))
                {
                    codepoints.Add(i, new UnicodeDataItem { Codepoint = i, LineBreakClass = lineBreakClass });
                }
                else
                {
                    codepoints[i].LineBreakClass = lineBreakClass;
                }
            }
        }

        public static List<(CodepointRange, string)> ReadGeneralCategoryData()
        {
            return ReadUnicodeData(
                "https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedGeneralCategory.txt");
        }

        public static List<(CodepointRange, string)> ReadScriptData()
        {
            return ReadUnicodeData("https://www.unicode.org/Public/UCD/latest/ucd/Scripts.txt");
        }

        public static List<(CodepointRange, string)> ReadBiDiData()
        {
            return ReadUnicodeData("https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedBidiClass.txt");
        }

        public static List<(CodepointRange, string)> ReadLineBreakClassData()
        {
            return ReadUnicodeData(
                "https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedLineBreak.txt");
        }

        private static List<(CodepointRange, string)> ReadUnicodeData(string file)
        {
            var data = new List<(CodepointRange, string)>();

            var rx = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s+;\s+(\w+)\s+#.*", RegexOptions.Compiled);

            using (var client = new HttpClient())
            {
                using (var result = client.GetAsync(file).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        return data;
                    }

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            var match = rx.Match(line);

                            if (!match.Success)
                            {
                                continue;
                            }

                            var start = Convert.ToInt32(match.Groups[1].Value, 16);

                            var end = start;

                            if (!string.IsNullOrEmpty(match.Groups[2].Value))
                            {
                                end = Convert.ToInt32(match.Groups[2].Value, 16);
                            }

                            data.Add((new CodepointRange(start, end), match.Groups[3].Value));
                        }
                    }
                }
            }

            return data;
        }

        internal class UnicodeDataItem
        {
            public int Codepoint { get; set; }

            public int Script { get; set; }

            public int GeneralCategory { get; set; }

            public int BiDiClass { get; set; }

            public int LineBreakClass { get; set; }
        }
    }

    internal readonly struct CodepointRange
    {
        public CodepointRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }
        public int End { get; }
    }
}
