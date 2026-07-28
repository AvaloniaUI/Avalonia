using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

internal static class GraphemeBreakClassTrieGenerator
{
    // Mirrors the values of IndicConjunctBreakClass in Avalonia.Base. Hardcoded
    // here so the generator doesn't take a reference on the enum type — if the
    // enum gains a new value the generator and Avalonia.Base must be updated
    // together. Order/values must stay in sync with IndicConjunctBreakClass.cs.
    private static readonly Dictionary<string, uint> s_indicConjunctBreakMap = new()
    {
        ["Linker"] = 1,
        ["Consonant"] = 2,
        ["Extend"] = 3,
    };

    public static UnicodeTrie Execute(string outputDir, out Dictionary<int, uint> values)
    {
        var entries = UnicodeEnumsGenerator.CreateGraphemeBreakTypeEnum(outputDir);

        // Map UCD names/tags to packed values. ExtendedPictographic isn't in
        // PropertyValueAliases (it lives in emoji-data.txt as its own property)
        // but is emitted as the last enum member by CreateGraphemeBreakTypeEnum,
        // so its index is entries.Count.
        var mappings = UnicodeDataGenerator.CreateNameAndTagToIndexMappings(entries);
        var extendedPictographicIndex = entries.Count;
        mappings["Extended_Pictographic"] = extendedPictographicIndex;
        mappings["ExtendedPictographic"] = extendedPictographicIndex;

        var trie = GenerateBreakTypeTrie(mappings, out values);

        UnicodeDataGenerator.GenerateTrieClass(outputDir, "GraphemeBreak", trie);

        return trie;
    }

    private static UnicodeTrie GenerateBreakTypeTrie(
        IReadOnlyDictionary<string, int> graphemeBreakMappings,
        out Dictionary<int, uint> values)
    {
        // "Other" (XX) is the GCB fallback class for codepoints with no explicit
        // assignment. Without an explicit initialValue, an unset trie slot would
        // resolve to whatever GCB class lands at index 0 in UCD's alphabetical
        // ordering instead.
        var trieBuilder = new UnicodeTrieBuilder((uint)graphemeBreakMappings["XX"]);
        values = new Dictionary<int, uint>();

        var graphemeBreakData = ReadBreakData("auxiliary/GraphemeBreakProperty.txt");
        var emojiBreakData = ReadBreakData("emoji/emoji-data.txt");

        foreach (var breakData in new[] { graphemeBreakData, emojiBreakData })
        {
            foreach (var (start, end, graphemeBreakType) in breakData)
            {
                if (!graphemeBreakMappings.TryGetValue(graphemeBreakType.Replace("_", ""), out var index))
                {
                    continue;
                }

                AddRange(
                    values,
                    start,
                    end,
                    (uint)index,
                    0,
                    UnicodeData.GRAPHEMEBREAK_MASK);
            }
        }

        foreach (var (start, end, indicConjunctBreakType) in ReadIndicConjunctBreakData())
        {
            if (!s_indicConjunctBreakMap.TryGetValue(indicConjunctBreakType, out var value))
            {
                continue;
            }

            AddRange(
                values,
                start,
                end,
                value,
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
