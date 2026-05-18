using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Avalonia.UnicodeTrieGenerator;

internal static class UnicodeEnumsGenerator
{
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
        // Common WB classes seeded in hot-path order; ParseDataEntries appends
        // anything new UCD introduces.
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
        // LB classes that participate in the pair-table are the hot path; seeding
        // them up front keeps their packed trie values in a dense low range, then
        // the default Unknown, then everything else UCD declares.
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

        WriteEnumFile(outputDir, "BidiClass", "BidiClass", entries);

        return entries;
    }

    public static List<DataEntry> CreateBiDiPairedBracketTypeEnum(string outputDir)
    {
        var entries = new List<DataEntry> { new("None", "n", string.Empty) };

        ParseDataEntries("# Bidi_Paired_Bracket_Type (bpt)", entries);

        WriteEnumFile(outputDir, "BidiPairedBracketType", "BidiPairedBracketType", entries);

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

    private static void WriteEnumFile(string outputDir, string fileBaseName, string typeName, IEnumerable<DataEntry> entries)
    {
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
