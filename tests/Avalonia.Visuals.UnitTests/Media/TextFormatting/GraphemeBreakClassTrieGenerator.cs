using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    public static class GraphemeBreakClassTrieGenerator
    {
        public static void Execute()
        {
            using (var stream = File.Create("Generated\\GraphemeBreak.trie"))
            {
                var trie = GenerateBreakTypeTrie();

                trie.Save(stream);
            }
        }

        private static UnicodeTrie GenerateBreakTypeTrie()
        {
            var graphemeBreakClassValues = UnicodeEnumsGenerator.GetPropertyValueAliases("# Grapheme_Cluster_Break (GCB)");

            var graphemeBreakClassMapping = graphemeBreakClassValues.Select(x => x.name).ToList();

            var trieBuilder = new UnicodeTrieBuilder();

            var graphemeBreakData = ReadBreakData(
                "https://www.unicode.org/Public/UCD/latest/ucd/auxiliary/GraphemeBreakProperty.txt");

            foreach (var (start, end, graphemeBreakType) in graphemeBreakData)
            {
                if (!graphemeBreakClassMapping.Contains(graphemeBreakType))
                {
                    continue;
                }

                if (start == end)
                {
                    trieBuilder.Set(start, (uint)graphemeBreakClassMapping.IndexOf(graphemeBreakType));
                }
                else
                {
                    trieBuilder.SetRange(start, end, (uint)graphemeBreakClassMapping.IndexOf(graphemeBreakType));
                }
            }

            var emojiBreakData = ReadBreakData("https://unicode.org/Public/emoji/12.0/emoji-data.txt");

            foreach (var (start, end, graphemeBreakType) in emojiBreakData)
            {
                if (!graphemeBreakClassMapping.Contains(graphemeBreakType))
                {
                    continue;
                }

                if (start == end)
                {
                    trieBuilder.Set(start, (uint)graphemeBreakClassMapping.IndexOf(graphemeBreakType));
                }
                else
                {
                    trieBuilder.SetRange(start, end, (uint)graphemeBreakClassMapping.IndexOf(graphemeBreakType));
                }
            }

            return trieBuilder.Freeze();
        }

        public static List<(int, int, string)> ReadBreakData(string file)
        {
            var data = new List<(int, int, string)>();

            var rx = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(\w+)\s*#.*", RegexOptions.Compiled);

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

                            data.Add((start, end, match.Groups[3].Value));
                        }
                    }
                }
            }

            return data;
        }
    }
}
