using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

internal static class EastAsianWidthClassTrieGenerator
{
    public static UnicodeTrie Execute(string outputDir, out List<(uint start, uint end, EastAsianWidthClass)> values)
    {
        var trie = GenerateTrie(out values);

        UnicodeDataGenerator.GenerateTrieClass(outputDir, "EastAsianWidth", trie);

        return trie;
    }

    private static UnicodeTrie GenerateTrie(out List<(uint start, uint end, EastAsianWidthClass)> values)
    {
        var trieBuilder = new UnicodeTrieBuilder((uint)EastAsianWidthClass.Neutral);

        var data = ReadData("EastAsianWidth.txt");

        values = new List<(uint start, uint end, EastAsianWidthClass)>(data.Count);

        foreach (var (start, end, tag) in data)
        {
            EastAsianWidthClass value;

            switch (tag.Replace("_", ""))
            {
                case "A":
                    value = EastAsianWidthClass.Ambiguous;
                    break;
                case "F":
                    value = EastAsianWidthClass.Fullwidth;
                    break;
                case "H":
                    value = EastAsianWidthClass.Halfwidth;
                    break;
                case "N":
                    value = EastAsianWidthClass.Neutral;
                    break;
                case "Na":
                    value = EastAsianWidthClass.Narrow;
                    break;
                case "W":
                    value = EastAsianWidthClass.Wide;
                    break;
                default:
                    continue;
            }

            if (start == end)
            {
                trieBuilder.Set(start, (uint)value);
            }
            else
            {
                trieBuilder.SetRange(start, end, (uint)value);
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
