using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

/// <summary>
/// Generates the consolidated SegmentationTrie that packs all five UAX-29/UAX-14 break
/// properties into a single 32-bit trie slot:
///   bits  0– 4: GraphemeBreak          (5 bits, shift  0)
///   bits  5– 6: IndicConjunctBreak     (2 bits, shift  5)
///   bits  7–11: WordBreak              (5 bits, shift  7)
///   bits 12–17: LineBreak              (6 bits, shift 12)
///   bits 18–22: SentenceBreak          (5 bits, shift 18)
/// Replaces the former GraphemeBreakTrie and SentenceBreakTrie (standalone) and removes
/// LineBreak and WordBreak from UnicodeDataTrie.
/// </summary>
internal static class SegmentationTrieGenerator
{
    // Mirrors IndicConjunctBreakClass in Avalonia.Base. Must stay in sync.
    private static readonly Dictionary<string, uint> s_indicConjunctBreakMap = new()
    {
        ["Linker"]    = 1,
        ["Consonant"] = 2,
        ["Extend"]    = 3,
    };

    public static UnicodeTrie Execute(string outputDir, out Dictionary<int, uint> values)
    {
        // Generate all enum .cs files and get the index mappings.
        var graphemeEntries      = UnicodeEnumsGenerator.CreateGraphemeBreakTypeEnum(outputDir);
        var wordBreakEntries     = UnicodeEnumsGenerator.CreateWordBreakClassEnum(outputDir);
        var lineBreakEntries     = UnicodeEnumsGenerator.CreateLineBreakClassEnum(outputDir);
        var sentenceBreakEntries = UnicodeEnumsGenerator.CreateSentenceBreakClassEnum(outputDir);

        var graphemeMappings      = UnicodeDataGenerator.CreateNameAndTagToIndexMappings(graphemeEntries);
        // ExtendedPictographic is not in PropertyValueAliases; it is emitted as the last member.
        var extPicIndex = graphemeEntries.Count;
        graphemeMappings["Extended_Pictographic"] = extPicIndex;
        graphemeMappings["ExtendedPictographic"]  = extPicIndex;

        var wordBreakMappings     = UnicodeDataGenerator.CreateNameAndTagToIndexMappings(wordBreakEntries);
        var lineBreakMappings     = UnicodeDataGenerator.CreateNameAndTagToIndexMappings(lineBreakEntries);
        var sentenceBreakMappings = UnicodeDataGenerator.CreateNameAndTagToIndexMappings(sentenceBreakEntries);

        // Default packed value: every break property at its "unassigned/other" fallback.
        // IndicConjunctBreak default is 0 (None) — no explicit field needed.
        var initialValue =
            ((uint)graphemeMappings["XX"]     << UnicodeData.GRAPHEMEBREAK_SHIFT)   |
            ((uint)wordBreakMappings["Other"] << UnicodeData.WORDBREAK_SHIFT)       |
            ((uint)lineBreakMappings["XX"]    << UnicodeData.LINEBREAK_SHIFT)       |
            ((uint)sentenceBreakMappings["XX"] << UnicodeData.SENTENCEBREAK_SHIFT);

        var trieBuilder = new UnicodeTrieBuilder(initialValue);
        values = new Dictionary<int, uint>();

        // ── GraphemeBreak ─────────────────────────────────────────────────────────
        foreach (var (start, end, breakType) in ReadBreakData("auxiliary/GraphemeBreakProperty.txt"))
        {
            if (!graphemeMappings.TryGetValue(breakType.Replace("_", ""), out var idx))
                continue;
            AddRange(values, start, end, (uint)idx,
                UnicodeData.GRAPHEMEBREAK_SHIFT, UnicodeData.GRAPHEMEBREAK_MASK, initialValue);
        }

        foreach (var (start, end, breakType) in ReadBreakData("emoji/emoji-data.txt"))
        {
            if (!graphemeMappings.TryGetValue(breakType.Replace("_", ""), out var idx))
                continue;
            AddRange(values, start, end, (uint)idx,
                UnicodeData.GRAPHEMEBREAK_SHIFT, UnicodeData.GRAPHEMEBREAK_MASK, initialValue);
        }

        // ── IndicConjunctBreak ────────────────────────────────────────────────────
        foreach (var (start, end, breakType) in ReadIndicConjunctBreakData())
        {
            if (!s_indicConjunctBreakMap.TryGetValue(breakType, out var value))
                continue;
            AddRange(values, start, end, value,
                UnicodeData.INDICCONJUNCTBREAK_SHIFT, UnicodeData.INDICCONJUNCTBREAK_MASK, initialValue);
        }

        // ── WordBreak ─────────────────────────────────────────────────────────────
        foreach (var (range, name) in UnicodeDataGenerator.ReadWordBreakClassData())
        {
            if (!wordBreakMappings.TryGetValue(name, out var idx) &&
                !wordBreakMappings.TryGetValue(name.Replace("_", ""), out idx))
                continue;
            AddRange(values, range.Start, range.End, (uint)idx,
                UnicodeData.WORDBREAK_SHIFT, UnicodeData.WORDBREAK_MASK, initialValue);
        }

        // ── LineBreak (missing-defaults first, then explicit assignments) ─────────
        foreach (var (range, name) in UnicodeDataGenerator.ReadLineBreakClassData())
        {
            if (!lineBreakMappings.TryGetValue(name, out var idx) &&
                !lineBreakMappings.TryGetValue(name.Replace("_", ""), out idx))
                continue;
            AddRange(values, range.Start, range.End, (uint)idx,
                UnicodeData.LINEBREAK_SHIFT, UnicodeData.LINEBREAK_MASK, initialValue);
        }

        // ── SentenceBreak ─────────────────────────────────────────────────────────
        foreach (var (start, end, sentenceBreakType) in ReadBreakData("auxiliary/SentenceBreakProperty.txt"))
        {
            if (!sentenceBreakMappings.TryGetValue(sentenceBreakType.Replace("_", ""), out var idx) &&
                !sentenceBreakMappings.TryGetValue(sentenceBreakType, out idx))
                continue;
            AddRange(values, start, end, (uint)idx,
                UnicodeData.SENTENCEBREAK_SHIFT, UnicodeData.SENTENCEBREAK_MASK, initialValue);
        }

        foreach (var (codepoint, value) in values)
        {
            trieBuilder.Set(codepoint, value);
        }

        var trie = trieBuilder.Freeze();
        UnicodeDataGenerator.GenerateTrieClass(outputDir, "Segmentation", trie);
        return trie;
    }

    /// <summary>
    /// Merges a single field value into the per-codepoint packed value dictionary.
    /// Entries not yet present are initialised with <paramref name="defaultValue"/> so that
    /// fields other than the one being written keep their correct packed defaults.
    /// </summary>
    private static void AddRange(
        Dictionary<int, uint> values,
        int start, int end,
        uint fieldValue, int shift, int mask,
        uint defaultValue)
    {
        var shiftedMask  = (uint)(mask << shift);
        var shiftedValue = fieldValue << shift;

        for (var codepoint = start; codepoint <= end; codepoint++)
        {
            if (!values.TryGetValue(codepoint, out var existing))
                existing = defaultValue;

            values[codepoint] = (existing & ~shiftedMask) | shiftedValue;
        }
    }

    private static List<(int Start, int End, string Type)> ReadBreakData(string file)
    {
        var data = new List<(int, int, string)>();
        var rx   = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(\w+)\s*#.*", RegexOptions.Compiled);

        using var stream = UcdDownloader.OpenRead(file);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;

            var match = rx.Match(line);
            if (!match.Success)
                continue;

            var start = Convert.ToInt32(match.Groups[1].Value, 16);
            var end   = string.IsNullOrEmpty(match.Groups[2].Value)
                ? start
                : Convert.ToInt32(match.Groups[2].Value, 16);

            data.Add((start, end, match.Groups[3].Value));
        }

        return data;
    }

    private static List<(int Start, int End, string Type)> ReadIndicConjunctBreakData()
    {
        var data = new List<(int, int, string)>();
        var rx   = new Regex(
            @"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*InCB\s*;\s*(\w+)\s*#.*",
            RegexOptions.Compiled);

        using var stream = UcdDownloader.OpenRead("DerivedCoreProperties.txt");
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;

            var match = rx.Match(line);
            if (!match.Success)
                continue;

            var start = Convert.ToInt32(match.Groups[1].Value, 16);
            var end   = string.IsNullOrEmpty(match.Groups[2].Value)
                ? start
                : Convert.ToInt32(match.Groups[2].Value, 16);

            data.Add((start, end, match.Groups[3].Value));
        }

        return data;
    }
}
