﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    internal static class UnicodeDataGenerator
    {
        public const string Ucd = "https://www.unicode.org/Public/13.0.0/ucd/";

        public static UnicodeTrie GenerateBiDiTrie(out BiDiDataEntries biDiDataEntries,out Dictionary<int, BiDiDataItem> biDiData)
        {
            biDiData = new Dictionary<int, BiDiDataItem>();

            var biDiClassEntries =
                UnicodeEnumsGenerator.CreateBiDiClassEnum();

            var biDiClassMappings = CreateTagToIndexMappings(biDiClassEntries);

            var biDiClassData = ReadBiDiData();

            foreach (var (range, name) in biDiClassData)
            {
                var biDiClass = biDiClassMappings[name];

                AddBiDiClassRange(biDiData, range, biDiClass);
            }

            var biDiPairedBracketTypeEntries = UnicodeEnumsGenerator.CreateBiDiPairedBracketTypeEnum();

            var biDiPairedBracketTypeMappings = CreateTagToIndexMappings(biDiPairedBracketTypeEntries);

            var biDiPairedBracketData = ReadBiDiPairedBracketData();
            
            foreach (var (range, name) in biDiPairedBracketData)
            {
                var bracketType = biDiPairedBracketTypeMappings[name];

                AddBiDiBracket(biDiData, range, bracketType);
            }
            
            var biDiTrieBuilder = new UnicodeTrieBuilder(/*initialValue*/);
            
            foreach (var properties in biDiData.Values)
            {
                //[bracket]|[bracketType]|[biDiClass]
                var value = (properties.BiDiClass << UnicodeData.BIDICLASS_SHIFT) |
                            (properties.BracketType << UnicodeData.BIDIPAIREDBRACKEDTYPE_SHIFT) | properties.Bracket;

                biDiTrieBuilder.Set(properties.Codepoint, (uint)value);
            }

            biDiDataEntries = new BiDiDataEntries()
            {
                PairedBracketTypes = biDiPairedBracketTypeEntries, BiDiClasses = biDiClassEntries
            };
            
            using (var stream = File.Create("Generated\\BiDi.trie"))
            {
                var trie = biDiTrieBuilder.Freeze();

                trie.Save(stream);

                return trie;
            }
        }

        public static UnicodeTrie GenerateUnicodeDataTrie(out UnicodeDataEntries dataEntries, out Dictionary<int, UnicodeDataItem> unicodeData)
        {
            var generalCategoryEntries =
                UnicodeEnumsGenerator.CreateGeneralCategoryEnum();

            var generalCategoryMappings = CreateTagToIndexMappings(generalCategoryEntries);
            
            var scriptEntries = UnicodeEnumsGenerator.CreateScriptEnum();

            var scriptMappings = CreateNameToIndexMappings(scriptEntries);
            
            var lineBreakClassEntries =
                UnicodeEnumsGenerator.CreateLineBreakClassEnum();

            var lineBreakClassMappings = CreateTagToIndexMappings(lineBreakClassEntries);

            unicodeData = GetUnicodeData(generalCategoryMappings, scriptMappings, lineBreakClassMappings);
            
            var unicodeDataTrieBuilder = new UnicodeTrieBuilder(/*initialValue*/);
            
            foreach (var properties in unicodeData.Values)
            {
                //[line break]|[biDi]|[script]|[category]
                var value = (properties.LineBreakClass << UnicodeData.LINEBREAK_SHIFT) |
                            (properties.Script << UnicodeData.SCRIPT_SHIFT) | properties.GeneralCategory;

                unicodeDataTrieBuilder.Set(properties.Codepoint, (uint)value);
            }

            dataEntries = new UnicodeDataEntries
            {
                Scripts = scriptEntries,
                GeneralCategories = generalCategoryEntries,
                LineBreakClasses = lineBreakClassEntries
            };

            using (var stream = File.Create("Generated\\UnicodeData.trie"))
            {
                var trie = unicodeDataTrieBuilder.Freeze();

                trie.Save(stream);
                
                return trie;
            }
        }

        private static Dictionary<int, UnicodeDataItem> GetUnicodeData(IReadOnlyDictionary<string, int> generalCategoryMappings, 
            IReadOnlyDictionary<string, int> scriptMappings, IReadOnlyDictionary<string, int> lineBreakClassMappings)
        {
            var unicodeData = new Dictionary<int, UnicodeDataItem>();
            
            var generalCategoryData = ReadGeneralCategoryData();

            foreach (var (range, name) in generalCategoryData)
            {
                var generalCategory = generalCategoryMappings[name];

                AddGeneralCategoryRange(unicodeData, range, generalCategory);
            }
            
            var scriptData = ReadScriptData();

            foreach (var (range, name) in scriptData)
            {
                var script = scriptMappings[name];

                AddScriptRange(unicodeData, range, script);
            }
            
            var lineBreakClassData = ReadLineBreakClassData();

            foreach (var (range, name) in lineBreakClassData)
            {
                var lineBreakClass = lineBreakClassMappings[name];

                AddLineBreakClassRange(unicodeData, range, lineBreakClass);
            }

            return unicodeData;
        }

        private static Dictionary<string, int> CreateTagToIndexMappings(IReadOnlyList<DataEntry> entries)
        {
            var mappings = new Dictionary<string, int>();

            for (var i = 0; i < entries.Count; i++)
            {
                mappings.Add(entries[i].Tag, i);
            }

            return mappings;
        }

        private static Dictionary<string, int> CreateNameToIndexMappings(IReadOnlyList<DataEntry> entries)
        {
            var mappings = new Dictionary<string, int>();

            for (var i = 0; i < entries.Count; i++)
            {
                mappings.Add(entries[i].Name, i);
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

        private static void AddBiDiClassRange(Dictionary<int, BiDiDataItem> codepoints, CodepointRange range,
            int biDiClass)
        {
            for (var i = range.Start; i <= range.End; i++)
            {
                if (!codepoints.ContainsKey(i))
                {
                    codepoints.Add(i, new BiDiDataItem { Codepoint = i, BiDiClass = biDiClass });
                }
                else
                {
                    codepoints[i].BiDiClass = biDiClass;
                }
            }
        }

        private static void AddBiDiBracket(Dictionary<int, BiDiDataItem> codepoints, CodepointRange range,
            int bracketType)
        {
            if (!codepoints.ContainsKey(range.Start))
            {
                codepoints.Add(range.Start,
                    new BiDiDataItem { Codepoint = range.Start, Bracket = range.End, BracketType = bracketType });
            }
            else
            {
                var codepoint = codepoints[range.Start];

                codepoint.Bracket = range.End;
                codepoint.BracketType = bracketType;
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
            return ReadUnicodeData("extracted/DerivedGeneralCategory.txt");
        }

        public static List<(CodepointRange, string)> ReadScriptData()
        {
            return ReadUnicodeData("Scripts.txt");
        }

        public static List<(CodepointRange, string)> ReadBiDiData()
        {
            return ReadUnicodeData("extracted/DerivedBidiClass.txt");
        }

        public static List<(CodepointRange, string)> ReadLineBreakClassData()
        {
            return ReadUnicodeData("extracted/DerivedLineBreak.txt");
        }
        
        public static List<(CodepointRange, string)> ReadBiDiPairedBracketData()
        {
            const string file = "BidiBrackets.txt";
        
            var data = new List<(CodepointRange, string)>();
            
            var regex = new Regex(@"^([0-9A-F]+);\s([0-9A-F]+);\s([ocn])");

            using (var client = new HttpClient())
            {
                var url = Path.Combine(Ucd, file);

                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
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

                            var match = regex.Match(line);

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

        private static List<(CodepointRange, string)> ReadUnicodeData(string file)
        {
            var data = new List<(CodepointRange, string)>();

            var regex = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s+;\s+(\w+)\s+#.*", RegexOptions.Compiled);

            using (var client = new HttpClient())
            {
                var url = Path.Combine(Ucd, file);

                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
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

                            var match = regex.Match(line);

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
        
        internal class BiDiDataItem
        {
            public int Codepoint { get; set; }

            public int Bracket { get; set; }

            public int BracketType { get; set; }

            public int BiDiClass { get; set; }
        }
        

    }
    
    internal class UnicodeDataEntries
    {
        public IReadOnlyList<DataEntry> Scripts { get; set; }
        public IReadOnlyList<DataEntry> GeneralCategories{ get; set; }
        public IReadOnlyList<DataEntry> LineBreakClasses{ get; set; }
    }
    
    internal class BiDiDataEntries
    {
        public IReadOnlyList<DataEntry> PairedBracketTypes { get; set; }
        public IReadOnlyList<DataEntry> BiDiClasses{ get; set; }
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
