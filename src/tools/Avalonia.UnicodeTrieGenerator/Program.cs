using System;
using System.IO;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

internal static class Program
{
    private static int Main(string[] args)
    {
        var outputDir = ParseArg(args, "--output");
        var cacheDir = ParseArg(args, "--cache");

        if (outputDir is null)
        {
            Console.Error.WriteLine("Usage: Avalonia.UnicodeTrieGenerator --output <path> [--cache <path>]");
            return 1;
        }

        if (cacheDir is not null)
        {
            UcdDownloader.CacheRoot = cacheDir;
        }

        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(UcdDownloader.CacheRoot);

        Console.WriteLine($"Unicode version: {UcdDownloader.UnicodeVersion}");
        Console.WriteLine($"Output directory: {outputDir}");
        Console.WriteLine($"Cache directory:  {UcdDownloader.CacheRoot}");
        Console.WriteLine();

        Console.WriteLine("Generating UnicodeData trie...");
        var unicodeDataTrie = UnicodeDataGenerator.GenerateUnicodeDataTrie(
            outputDir, out var unicodeDataEntries, out var unicodeData);
        VerifyUnicodeDataTrie(unicodeDataTrie, unicodeData);

        Console.WriteLine("Generating BiDi trie...");
        var biDiTrie = UnicodeDataGenerator.GenerateBiDiTrie(
            outputDir, out var biDiDataEntries, out var biDiData);
        VerifyBiDiTrie(biDiTrie, biDiData);

        Console.WriteLine("Generating EastAsianWidth trie...");
        UnicodeEnumsGenerator.CreateEastAsianWidthClassEnum(outputDir);
        var eawTrie = EastAsianWidthClassTrieGenerator.Execute(outputDir, out var eawValues);
        VerifyEastAsianWidthTrie(eawTrie, eawValues);

        Console.WriteLine("Generating GraphemeBreak trie...");
        UnicodeEnumsGenerator.CreateGraphemeBreakTypeEnum(outputDir);
        GraphemeBreakClassTrieGenerator.Execute(outputDir);

        Console.WriteLine("Generating PropertyValueAliasHelper...");
        UnicodeEnumsGenerator.CreatePropertyValueAliasHelper(outputDir, unicodeDataEntries, biDiDataEntries);

        Console.WriteLine();
        Console.WriteLine("Done.");
        Console.WriteLine("If new enum values were introduced, rebuild Avalonia.Base and rerun this generator");
        Console.WriteLine("so the trie data reflects the updated enums.");
        return 0;
    }

    private static string? ParseArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static void VerifyUnicodeDataTrie(
        UnicodeTrie trie,
        System.Collections.Generic.Dictionary<int, UnicodeDataGenerator.UnicodeDataItem> data)
    {
        foreach (var value in data.Values)
        {
            var packed = trie.Get((uint)value.Codepoint);

            Expect(value.GeneralCategory, GetValue(packed, 0, UnicodeData.CATEGORY_MASK), "GeneralCategory", value.Codepoint);
            Expect(value.Script, GetValue(packed, UnicodeData.SCRIPT_SHIFT, UnicodeData.SCRIPT_MASK), "Script", value.Codepoint);
            Expect(value.LineBreakClass, GetValue(packed, UnicodeData.LINEBREAK_SHIFT, UnicodeData.LINEBREAK_MASK), "LineBreakClass", value.Codepoint);
            Expect(value.WordBreakClass, GetValue(packed, UnicodeData.WORDBREAK_SHIFT, UnicodeData.WORDBREAK_MASK), "WordBreakClass", value.Codepoint);
        }
    }

    private static void VerifyBiDiTrie(
        UnicodeTrie trie,
        System.Collections.Generic.Dictionary<int, UnicodeDataGenerator.BiDiDataItem> data)
    {
        foreach (var value in data.Values)
        {
            var packed = trie.Get((uint)value.Codepoint);

            Expect(value.Bracket, GetValue(packed, 0, UnicodeData.BIDIPAIREDBRACKED_MASK), "Bracket", value.Codepoint);
            Expect(value.BracketType, GetValue(packed, UnicodeData.BIDIPAIREDBRACKEDTYPE_SHIFT, UnicodeData.BIDIPAIREDBRACKEDTYPE_MASK), "BracketType", value.Codepoint);
            Expect(value.BiDiClass, GetValue(packed, UnicodeData.BIDICLASS_SHIFT, UnicodeData.BIDICLASS_MASK), "BiDiClass", value.Codepoint);
        }
    }

    private static void VerifyEastAsianWidthTrie(
        UnicodeTrie trie,
        System.Collections.Generic.List<(uint start, uint end, EastAsianWidthClass)> values)
    {
        foreach (var (start, _, value) in values)
        {
            var expected = (uint)value;
            var actual = trie.Get(start);

            if (expected != actual)
            {
                throw new InvalidOperationException(
                    $"EastAsianWidth trie mismatch at U+{start:X4}: expected {expected}, got {actual}.");
            }
        }
    }

    private static int GetValue(uint value, int shift, int mask)
        => (int)((value >> shift) & mask);

    private static void Expect(int expected, int actual, string field, int codepoint)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"{field} mismatch at U+{codepoint:X4}: expected {expected}, got {actual}.");
        }
    }
}
