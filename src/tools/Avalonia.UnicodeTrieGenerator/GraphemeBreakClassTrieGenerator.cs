using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

internal static class GraphemeBreakClassTrieGenerator
{
    public static void Execute(string outputDir)
    {
        var trie = GenerateBreakTypeTrie();

        UnicodeDataGenerator.GenerateTrieClass(outputDir, "GraphemeBreak", trie);
    }

    private static UnicodeTrie GenerateBreakTypeTrie()
    {
        var trieBuilder = new UnicodeTrieBuilder();
        var values = new Dictionary<int, uint>();

        var graphemeBreakData = ReadBreakData("auxiliary/GraphemeBreakProperty.txt");
        var emojiBreakData = ReadBreakData("emoji/emoji-data.txt");

        foreach (var breakData in new[] { graphemeBreakData, emojiBreakData })
        {
            foreach (var (start, end, graphemeBreakType) in breakData)
            {
                if (!Enum.TryParse<GraphemeBreakClass>(graphemeBreakType.Replace("_", ""), out var value))
                {
                    continue;
                }

                AddRange(
                    values,
                    start,
                    end,
                    (uint)value,
                    0,
                    UnicodeData.GRAPHEMEBREAK_MASK);
            }
        }

        foreach (var (start, end, indicConjunctBreakType) in ReadIndicConjunctBreakData())
        {
            var value = indicConjunctBreakType switch
            {
                "Linker" => IndicConjunctBreakClass.Linker,
                "Consonant" => IndicConjunctBreakClass.Consonant,
                "Extend" => IndicConjunctBreakClass.Extend,
                _ => IndicConjunctBreakClass.None
            };

            if (value == IndicConjunctBreakClass.None)
            {
                continue;
            }

            AddRange(
                values,
                start,
                end,
                (uint)value,
                UnicodeData.INDICCONJUNCTBREAK_SHIFT,
                UnicodeData.INDICCONJUNCTBREAK_MASK);
        }

        foreach (var (codepoint, value) in values)
        {
            trieBuilder.Set(codepoint, value);
        }

        return trieBuilder.Freeze();
    }

    private static void AddRange(Dictionary<int, uint> values, int start, int end, uint value, int shift, int mask)
    {
        var shiftedMask = (uint)(mask << shift);
        var shiftedValue = value << shift;

        for (var codepoint = start; codepoint <= end; codepoint++)
        {
            values.TryGetValue(codepoint, out var existing);
            values[codepoint] = (existing & ~shiftedMask) | shiftedValue;
        }
    }

    private static List<(int, int, string)> ReadBreakData(string file)
    {
        var data = new List<(int, int, string)>();
        var rx = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(\w+)\s*#.*", RegexOptions.Compiled);

        using var stream = UcdDownloader.OpenRead(file);
        using var reader = new StreamReader(stream);

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

        return data;
    }

    private static List<(int, int, string)> ReadIndicConjunctBreakData()
    {
        var data = new List<(int, int, string)>();
        var rx = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*InCB\s*;\s*(\w+)\s*#.*", RegexOptions.Compiled);

        using var stream = UcdDownloader.OpenRead("DerivedCoreProperties.txt");
        using var reader = new StreamReader(stream);

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

        return data;
    }
}
