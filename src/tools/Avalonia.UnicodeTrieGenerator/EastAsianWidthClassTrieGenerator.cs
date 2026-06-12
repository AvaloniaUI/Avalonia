using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

internal static class EastAsianWidthClassTrieGenerator
{
    public static UnicodeTrie Execute(string outputDir, out List<(uint start, uint end, uint value)> values)
    {
        // CreateEastAsianWidthClassEnum writes the EastAsianWidthClass.cs file
        // and returns the entries in their enum order. We index by tag so we
        // don't have to take a hard reference on the generated enum type.
        var entries = UnicodeEnumsGenerator.CreateEastAsianWidthClassEnum(outputDir);
        var mappings = UnicodeDataGenerator.CreateTagToIndexMappings(entries);

        var trie = GenerateTrie(mappings, out values);

        UnicodeDataGenerator.GenerateTrieClass(outputDir, "EastAsianWidth", trie);

        return trie;
    }

    private static UnicodeTrie GenerateTrie(
        IReadOnlyDictionary<string, int> mappings,
        out List<(uint start, uint end, uint value)> values)
    {
        // "N" (Neutral) is the EAW fallback class for unassigned codepoints.
        var trieBuilder = new UnicodeTrieBuilder((uint)mappings["N"]);

        var data = ReadData("EastAsianWidth.txt");

        values = new List<(uint start, uint end, uint value)>(data.Count);

        foreach (var (start, end, tag) in data)
        {
            if (!mappings.TryGetValue(tag.Replace("_", ""), out var index))
            {
                continue;
            }

            var value = (uint)index;

            if (start == end)
            {
                trieBuilder.Set(start, value);
            }
            else
            {
                trieBuilder.SetRange(start, end, value);
            }

            values.Add(((uint)start, (uint)end, value));
        }

        return trieBuilder.Freeze();
    }

    private static List<(int, int, string)> ReadData(string file)
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
}
