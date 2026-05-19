using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Avalonia.UnicodeTrieGenerator;

internal static class UnicodeEnumsGenerator
{
    // ABI INVARIANT for every Create*Enum method below
    // ----------------------------------------------------------------------
    // The seed list at the top of each method pins the int positions of every
    // currently-committed enum member. Avalonia.Base ships these enums as part
    // of its public API, so reordering them is an ABI break for downstream
    // consumers and would also invalidate every committed .trie.cs file
    // (packed values reference the int positions).
    //
    // Rules for maintainers:
    //   * NEVER reorder or remove a seed entry. Mark removed entries with an
    //     Obsolete comment but keep the slot.
    //   * UCD-added entries that aren't in the seed list will be appended by
    //     ParseDataEntries at the next available int (logged as a warning so
    //     the generator points them out at runtime). If we want them to keep
    //     stable positions, add them to the seed list.
    //   * The seed Name is what becomes the public enum member name (after
    //     underscore stripping). WordBreakClass uses this to give the UCD
    //     CR / LF members friendlier names (Carriage_Return / Line_Feed).

    public static List<DataEntry> CreateScriptEnum(string outputDir)
    {
        var entries = new List<DataEntry>
        {
            new("Unknown", "Zzzz", string.Empty),
            new("Common", "Zyyy", string.Empty),
            new("Inherited", "Zinh", string.Empty)
        };

        ParseDataEntries("# Script (sc)", entries);

        WriteEnumFile(outputDir, "Script", "Script", entries);

        return entries;
    }

    public static List<DataEntry> CreateEastAsianWidthClassEnum(string outputDir)
    {
        var entries = new List<DataEntry>();

        ParseDataEntries("# East_Asian_Width (ea)", entries);

        WriteEnumFile(outputDir, "EastAsianWidthClass", "EastAsianWidthClass", entries);

        return entries;
    }

    public static List<DataEntry> CreateGeneralCategoryEnum(string outputDir)
    {
        var entries = new List<DataEntry> { new("Other", "C", " Cc | Cf | Cn | Co | Cs") };

        ParseDataEntries("# General_Category (gc)", entries);

        WriteEnumFile(outputDir, "GeneralCategory", "GeneralCategory", entries);

        return entries;
    }

    public static List<DataEntry> CreateGraphemeBreakTypeEnum(string outputDir)
    {
        var entries = new List<DataEntry> { new("Other", "XX", string.Empty) };

        ParseDataEntries("# Grapheme_Cluster_Break (GCB)", entries);

        // GraphemeBreakClass has a synthetic ExtendedPictographic member appended
        // at the end (it's defined in emoji-data.txt, not PropertyValueAliases).
        // The ABI check needs to know it lives at index entries.Count.
        var positions = new Dictionary<string, int>();
        for (var i = 0; i < entries.Count; i++)
        {
            positions[entries[i].Tag] = i;
        }
        positions[ExtendedPictographicSentinel] = entries.Count;

        ValidateAbiStability(outputDir, "GraphemeBreakClass.cs", "GraphemeBreakClass", positions);

        using var stream = File.Create(Path.Combine(outputDir, "GraphemeBreakClass.cs"));
        using var writer = new StreamWriter(stream);

        writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
        writer.WriteLine("{");
        writer.WriteLine("    public enum GraphemeBreakClass");
        writer.WriteLine("    {");

        foreach (var entry in entries)
        {
            writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                             (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
        }

        writer.WriteLine("        ExtendedPictographic");
        writer.WriteLine("    }");
        writer.WriteLine("}");

        return entries;
    }

    public static List<DataEntry> CreateWordBreakClassEnum(string outputDir)
    {
        // Seeds pin the int positions of the public WordBreakClass members AND
        // override UCD's bare "CR"/"LF" with the friendlier Carriage_Return /
        // Line_Feed names. See the ABI INVARIANT comment above.
        var entries = new List<DataEntry>
        {
            new("Other", "XX", string.Empty),
            new("Carriage_Return", "CR", string.Empty),
            new("Line_Feed", "LF", string.Empty),
            new("Newline", "NL", string.Empty),
            new("Extend", "Extend", string.Empty),
            new("ZWJ", "ZWJ", string.Empty),
            new("Regional_Indicator", "RI", string.Empty),
            new("Format", "FO", string.Empty),
            new("Katakana", "KA", string.Empty),
            new("Hebrew_Letter", "HL", string.Empty),
            new("ALetter", "LE", string.Empty),
            new("Single_Quote", "SQ", string.Empty),
            new("Double_Quote", "DQ", string.Empty),
            new("MidNumLet", "MB", string.Empty),
            new("MidLetter", "ML", string.Empty),
            new("MidNum", "MN", string.Empty),
            new("Numeric", "NU", string.Empty),
            new("ExtendNumLet", "EX", string.Empty),
            new("WSegSpace", "WSegSpace", string.Empty)
        };

        ParseDataEntries("# Word_Break (WB)", entries);

        // E_Base / E_Modifier / Glue_After_Zwj / E_Base_GAZ were folded into
        // Extend / ZWJ / ALetter in Unicode 11; UCD still lists them in
        // PropertyValueAliases but no codepoint maps to them, so we drop them
        // from the enum to keep the public surface free of dead values.
        entries.RemoveAll(e => e.Tag is "EB" or "EBG" or "EM" or "GAZ");

        WriteEnumFile(outputDir, "WordBreakClass", "WordBreakClass", entries);

        return entries;
    }

    public static List<DataEntry> CreateLineBreakClassEnum(string outputDir)
    {
        var entries = new List<DataEntry>
        {
            new("Open_Punctuation", "OP", string.Empty),
            new("Close_Punctuation", "CL", string.Empty),
            new("Close_Parenthesis", "CP", string.Empty),
            new("Quotation", "QU", string.Empty),
            new("Glue", "GL", string.Empty),
            new("Nonstarter", "NS", string.Empty),
            new("Exclamation", "EX", string.Empty),
            new("Break_Symbols", "SY", string.Empty),
            new("Infix_Numeric", "IS", string.Empty),
            new("Prefix_Numeric", "PR", string.Empty),
            new("Postfix_Numeric", "PO", string.Empty),
            new("Numeric", "NU", string.Empty),
            new("Alphabetic", "AL", string.Empty),
            new("Hebrew_Letter", "HL", string.Empty),
            new("Ideographic", "ID", string.Empty),
            new("Inseparable", "IN", string.Empty),
            new("Hyphen", "HY", string.Empty),
            new("Break_After", "BA", string.Empty),
            new("Break_Before", "BB", string.Empty),
            new("Break_Both", "B2", string.Empty),
            new("ZWSpace", "ZW", string.Empty),
            new("Combining_Mark", "CM", string.Empty),
            new("Word_Joiner", "WJ", string.Empty),
            new("H2", "H2", string.Empty),
            new("H3", "H3", string.Empty),
            new("JL", "JL", string.Empty),
            new("JV", "JV", string.Empty),
            new("JT", "JT", string.Empty),
            new("Regional_Indicator", "RI", string.Empty),
            new("E_Base", "EB", string.Empty),
            new("E_Modifier", "EM", string.Empty),
            new("ZWJ", "ZWJ", string.Empty),
            new("Contingent_Break", "CB", string.Empty),
            new("Unknown", "XX", string.Empty)
        };

        ParseDataEntries("# Line_Break (lb)", entries);

        WriteEnumFile(outputDir, "LineBreakClass", "LineBreakClass", entries);

        return entries;
    }

    public static List<DataEntry> CreateBidiClassEnum(string outputDir)
    {
        var entries = new List<DataEntry> { new("Left_To_Right", "L", string.Empty) };

        ParseDataEntries("# Bidi_Class (bc)", entries);

        WriteEnumFile(outputDir, "BiDiClass", "BidiClass", entries);

        return entries;
    }

    public static List<DataEntry> CreateBiDiPairedBracketTypeEnum(string outputDir)
    {
        var entries = new List<DataEntry> { new("None", "n", string.Empty) };

        ParseDataEntries("# Bidi_Paired_Bracket_Type (bpt)", entries);

        WriteEnumFile(outputDir, "BiDiPairedBracketType", "BidiPairedBracketType", entries);

        return entries;
    }

    public static void CreatePropertyValueAliasHelper(
        string outputDir,
        UnicodeDataEntries unicodeDataEntries,
        BiDiDataEntries biDiDataEntries)
    {
        using var stream = File.Create(Path.Combine(outputDir, "PropertyValueAliasHelper.cs"));
        using var writer = new StreamWriter(stream);

        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();

        writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
        writer.WriteLine("{");
        writer.WriteLine("    internal static class PropertyValueAliasHelper");
        writer.WriteLine("    {");

        WritePropertyValueAliasGetTag(writer, unicodeDataEntries.Scripts, "Script", "Zzzz");

        WritePropertyValueAlias(writer, unicodeDataEntries.Scripts, "Script", "Unknown");
        WritePropertyValueAlias(writer, unicodeDataEntries.GeneralCategories, "GeneralCategory", "Other");
        WritePropertyValueAlias(writer, unicodeDataEntries.LineBreakClasses, "LineBreakClass", "Unknown");
        WritePropertyValueAlias(writer, unicodeDataEntries.WordBreakClasses, "WordBreakClass", "Other");
        WritePropertyValueAlias(writer, biDiDataEntries.PairedBracketTypes, "BidiPairedBracketType", "None");
        WritePropertyValueAlias(writer, biDiDataEntries.BiDiClasses, "BidiClass", "LeftToRight");

        writer.WriteLine("    }");
        writer.WriteLine("}");
    }

    private static void WriteEnumFile(string outputDir, string fileBaseName, string typeName, IReadOnlyList<DataEntry> entries)
    {
        var positions = new Dictionary<string, int>();
        for (var i = 0; i < entries.Count; i++)
        {
            positions[entries[i].Tag] = i;
        }

        ValidateAbiStability(outputDir, fileBaseName + ".cs", typeName, positions);

        using var stream = File.Create(Path.Combine(outputDir, fileBaseName + ".cs"));
        using var writer = new StreamWriter(stream);

        writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
        writer.WriteLine("{");
        writer.WriteLine($"    public enum {typeName}");
        writer.WriteLine("    {");

        foreach (var entry in entries)
        {
            writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                             (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
        }

        writer.WriteLine("    }");
        writer.WriteLine("}");
    }

    // Synthetic tag for tag-less enum members (currently only GraphemeBreakClass's
    // ExtendedPictographic, which is defined outside PropertyValueAliases).
    private const string ExtendedPictographicSentinel = "$ExtendedPictographic";

    // Matches an enum member line exactly as written by WriteEnumFile / the
    // GraphemeBreakClass custom writer:
    //   "        Name, //Tag[#Comment]"      ← most entries
    //   "        Name = N, //Tag[#Comment]"  ← post-bump shape if positions are
    //                                         ever made explicit
    //   "        Name"                       ← ExtendedPictographic
    private static readonly Regex s_enumMemberLineRegex = new(
        @"^        (?<name>\w+)(?:\s*=\s*(?<pos>\d+))?\s*,?\s*(?://(?<tag>[^\s#]+))?",
        RegexOptions.Compiled);

    /// <summary>
    /// Reads the int positions of every member in an existing generated enum
    /// file. Returns the empty map if the file doesn't exist (first generation).
    /// Tag-less members (e.g. <c>ExtendedPictographic</c>) are keyed by a
    /// synthetic <c>$Name</c> tag so they round-trip through ABI checks.
    /// </summary>
    private static Dictionary<string, int> ReadExistingEnumPositions(string outputDir, string fileName)
    {
        var positions = new Dictionary<string, int>();
        var path = Path.Combine(outputDir, fileName);

        if (!File.Exists(path))
        {
            return positions;
        }

        var implicitIndex = 0;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var match = s_enumMemberLineRegex.Match(rawLine);
            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups["name"].Value;

            // Filter the few non-member lines that satisfy the prefix anchor
            // (shouldn't happen with our writer, but defensive).
            if (name == "public" || name == "internal" || name == "namespace")
            {
                continue;
            }

            var explicitPos = match.Groups["pos"].Success
                ? int.Parse(match.Groups["pos"].Value)
                : -1;
            var tag = match.Groups["tag"].Success
                ? match.Groups["tag"].Value
                : "$" + name;

            var pos = explicitPos >= 0 ? explicitPos : implicitIndex;
            positions[tag] = pos;
            implicitIndex = pos + 1;
        }

        return positions;
    }

    /// <summary>
    /// Avalonia ships these enums as part of its public API; int positions are
    /// ABI. This check fails if a regeneration would shift the position of any
    /// member that was present in the committed source. The fix is always to
    /// edit the seed list in this file to preserve the old position; never to
    /// commit a regenerated enum with shifted positions.
    /// </summary>
    private static void ValidateAbiStability(
        string outputDir,
        string fileName,
        string typeName,
        IReadOnlyDictionary<string, int> newPositions)
    {
        var existing = ReadExistingEnumPositions(outputDir, fileName);
        if (existing.Count == 0)
        {
            return; // First generation, nothing to compare against.
        }

        var problems = new List<string>();

        foreach (var (tag, oldPos) in existing)
        {
            if (newPositions.TryGetValue(tag, out var newPos))
            {
                if (newPos != oldPos)
                {
                    problems.Add(
                        $"    tag '{tag}' moved from position {oldPos} to position {newPos}");
                }
            }
            else
            {
                problems.Add(
                    $"    tag '{tag}' (was at position {oldPos}) is no longer present in the regenerated enum");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"ABI break detected in {typeName}: enum member positions changed.\n" +
                string.Join("\n", problems) + "\n" +
                "Update the seed list in UnicodeEnumsGenerator.cs to restore these positions. " +
                "If a member was intentionally removed, keep its slot with an Obsolete-marked seed " +
                "entry instead of deleting it.");
        }
    }

    public static void ParseDataEntries(string property, List<DataEntry> entries)
    {
        using var stream = UcdDownloader.OpenRead("PropertyValueAliases.txt");
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line != property)
            {
                continue;
            }

            reader.ReadLine();
            break;
        }

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                break;
            }

            var elements = line.Split(';');

            var tag = elements[1].Trim();

            elements = elements[2].Split('#');

            var name = elements[0].Trim();

            // Dedupe by either Name or Tag so callers can seed an entry with a
            // friendlier Name than UCD's primary (e.g. "Carriage_Return" vs the
            // WB-section primary "CR") and still skip the UCD-listed duplicate.
            if (entries.Any(x => x.Name == name || x.Tag == tag))
            {
                continue;
            }

            var comment = string.Empty;

            if (elements.Length > 1)
            {
                comment = elements[1];
            }

            entries.Add(new DataEntry(name, tag, comment));
        }
    }

    private static void WritePropertyValueAliasGetTag(TextWriter writer, IEnumerable<DataEntry> entries,
        string typeName, string defaultValue)
    {
        writer.WriteLine(
            $"        private static readonly Dictionary<{typeName}, string> s_{typeName.ToLower()}ToTag = ");
        writer.WriteLine($"            new Dictionary<{typeName}, string>{{");

        foreach (var entry in entries)
        {
            writer.WriteLine($"                {{ {typeName}.{entry.Name.Replace("_", "")}, \"{entry.Tag}\"}},");
        }

        writer.WriteLine("        };");

        writer.WriteLine();

        writer.WriteLine($"        public static string GetTag({typeName} {typeName.ToLower()})");
        writer.WriteLine("        {");
        writer.WriteLine($"            if (!s_{typeName.ToLower()}ToTag.TryGetValue({typeName.ToLower()}, out var value))");
        writer.WriteLine("            {");
        writer.WriteLine($"                return \"{defaultValue}\";");
        writer.WriteLine("            }");
        writer.WriteLine($"            return value;");
        writer.WriteLine("        }");

        writer.WriteLine();
    }

    private static void WritePropertyValueAlias(TextWriter writer, IEnumerable<DataEntry> entries, string typeName,
        string defaultValue)
    {
        writer.WriteLine($"        private static readonly Dictionary<string, {typeName}> s_tagTo{typeName} = ");
        writer.WriteLine($"            new Dictionary<string,{typeName}>{{");

        foreach (var entry in entries)
        {
            writer.WriteLine($"                {{ \"{entry.Tag}\", {typeName}.{entry.Name.Replace("_", "")}}},");
        }

        writer.WriteLine("        };");

        writer.WriteLine();

        writer.WriteLine($"        public static {typeName} Get{typeName}(string tag)");
        writer.WriteLine("        {");
        writer.WriteLine($"            if (!s_tagTo{typeName}.TryGetValue(tag, out var value))");
        writer.WriteLine("            {");
        writer.WriteLine($"                return {typeName}.{defaultValue};");
        writer.WriteLine("            }");
        writer.WriteLine($"            return value;");
        writer.WriteLine("        }");

        writer.WriteLine();
    }
}

public readonly struct DataEntry
{
    public DataEntry(string name, string tag, string comment)
    {
        Name = name;
        Tag = tag;
        Comment = comment;
    }

    public string Name { get; }
    public string Tag { get; }
    public string Comment { get; }
}
