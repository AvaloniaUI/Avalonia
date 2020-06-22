using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    internal static class UnicodeEnumsGenerator
    {
        public static List<(string name, string tag, string comment)> CreateScriptEnum()
        {
            var scriptValues = GetPropertyValueAliases("# Script (sc)");

            using (var stream = File.Create("Generated\\Script.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum Script");
                writer.WriteLine("    {");

                foreach (var (name, tag, comment) in scriptValues)
                {
                    writer.WriteLine("        " + name + ", //" + tag +
                                     (string.IsNullOrEmpty(comment) ? string.Empty : "#" + comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return scriptValues;
        }

        public static List<(string name, string tag, string comment)> CreateGeneralCategoryEnum()
        {
            var generalCategoryValues = GetPropertyValueAliases("# General_Category (gc)");

            using (var stream = File.Create("Generated\\GeneralCategory.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum GeneralCategory");
                writer.WriteLine("    {");

                foreach (var (name, tag, comment) in generalCategoryValues)
                {
                    writer.WriteLine("        " + name + ", //" + tag +
                                     (string.IsNullOrEmpty(comment) ? string.Empty : "#" + comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return generalCategoryValues;
        }

        public static List<(string name, string tag, string comment)> CreateGraphemeBreakTypeEnum()
        {
            var graphemeClusterBreakValues = GetPropertyValueAliases("# Grapheme_Cluster_Break (GCB)");

            using (var stream = File.Create("Generated\\GraphemeBreakClass.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum GraphemeBreakClass");
                writer.WriteLine("    {");

                foreach (var (name, tag, comment) in graphemeClusterBreakValues)
                {
                    writer.WriteLine("        " + name + ", //" + tag +
                                     (string.IsNullOrEmpty(comment) ? string.Empty : "#" + comment));
                }

                writer.WriteLine("        ExtendedPictographic");

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return graphemeClusterBreakValues;
        }

        private static List<string> GenerateBreakPairTable()
        {
            var rows = new List<string[]>();

            using (var stream =
                typeof(UnicodeEnumsGenerator).Assembly.GetManifestResourceStream(
                    "Avalonia.Visuals.UnitTests.Media.TextFormatting.BreakPairTable.txt"))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    var columns = line.Split('\t');

                    rows.Add(columns);
                }
            }

            using (var stream = File.Create("Generated\\BreakPairTable.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    internal static class BreakPairTable");
                writer.WriteLine("    {");

                writer.WriteLine("        private static readonly byte[][] s_breakPairTable = ");
                writer.WriteLine("            {");

                for (var i = 1; i < rows.Count; i++)
                {
                    writer.Write("             new byte[] {");

                    writer.Write($"{GetBreakPairType(rows[i][1])}");

                    for (var index = 2; index < rows[i].Length; index++)
                    {
                        var column = rows[i][index];

                        writer.Write($",{GetBreakPairType(column)}");
                    }

                    writer.Write("},");
                    writer.Write(Environment.NewLine);
                }
                writer.WriteLine("        };");

                writer.WriteLine();

                writer.WriteLine("        public static PairBreakType Map(LineBreakClass first, LineBreakClass second)");
                writer.WriteLine("        {");
                writer.WriteLine("            return (PairBreakType)s_breakPairTable[(int)first][(int)second];");
                writer.WriteLine("        }");

                writer.WriteLine("    }");

                writer.WriteLine();

                writer.WriteLine("    internal enum PairBreakType : byte");
                writer.WriteLine("    {");
                writer.WriteLine("        DI = 0, // Direct break opportunity");
                writer.WriteLine("        IN = 1, // Indirect break opportunity");
                writer.WriteLine("        CI = 2, // Indirect break opportunity for combining marks");
                writer.WriteLine("        CP = 3, // Prohibited break for combining marks");
                writer.WriteLine("        PR = 4 // Prohibited break");
                writer.WriteLine("    }");


                writer.WriteLine("}");
            }

            return rows[0].Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public const byte DI_BRK = 0; // Direct break opportunity
        public const byte IN_BRK = 1; // Indirect break opportunity
        public const byte CI_BRK = 2; // Indirect break opportunity for combining marks
        public const byte CP_BRK = 3; // Prohibited break for combining marks
        public const byte PR_BRK = 4; // Prohibited break

        private static byte GetBreakPairType(string type)
        {
            switch (type)
            {
                case "_":
                    return DI_BRK;
                case "%":
                    return IN_BRK;
                case "#":
                    return CI_BRK;
                case "@":
                    return CP_BRK;
                case "^":
                    return PR_BRK;
                default:
                    return byte.MaxValue;
            }
        }

        public static List<(string name, string tag, string comment)> CreateLineBreakClassEnum()
        {
            var usedLineBreakClasses = GenerateBreakPairTable();

            var lineBreakValues = GetPropertyValueAliases("# Line_Break (lb)");

            var lineBreakClassMappings = lineBreakValues.ToDictionary(x => x.tag, x => (x.name, x.tag, x.comment));

            var orderedLineBreakValues = usedLineBreakClasses.Select(x =>
            {
                var value = lineBreakClassMappings[x];
                lineBreakClassMappings.Remove(x);
                return value;
            }).ToList();

            using (var stream = File.Create("Generated\\LineBreakClass.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum LineBreakClass");
                writer.WriteLine("    {");

                foreach (var (name, tag, comment) in orderedLineBreakValues)
                {
                    writer.WriteLine("        " + name + ", //" + tag +
                                     (string.IsNullOrEmpty(comment) ? string.Empty : "#" + comment));
                }

                writer.WriteLine();

                foreach (var (name, tag, comment) in lineBreakClassMappings.Values)
                {
                    writer.WriteLine("        " + name + ", //" + tag +
                                     (string.IsNullOrEmpty(comment) ? string.Empty : "#" + comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            orderedLineBreakValues.AddRange(lineBreakClassMappings.Values);

            return orderedLineBreakValues;
        }

        public static List<(string name, string tag, string comment)> CreateBiDiClassEnum()
        {
            var biDiClassValues = GetPropertyValueAliases("# Bidi_Class (bc)");

            using (var stream = File.Create("Generated\\BiDiClass.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum BiDiClass");
                writer.WriteLine("    {");

                foreach (var (name, tag, comment) in biDiClassValues)
                {
                    writer.WriteLine("        " + name + ", //" + tag +
                                     (string.IsNullOrEmpty(comment) ? string.Empty : "#" + comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return biDiClassValues;
        }

        public static void CreatePropertyValueAliasHelper(List<(string name, string tag, string comment)> scriptValues,
            List<(string name, string tag, string comment)> generalCategoryValues,
            List<(string name, string tag, string comment)> biDiClassValues,
            List<(string name, string tag, string comment)> lineBreakValues)
        {
            using (var stream = File.Create("Generated\\PropertyValueAliasHelper.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine();

                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public static class PropertyValueAliasHelper");
                writer.WriteLine("    {");

                WritePropertyValueAliasGetTag(writer, scriptValues, "Script", "Zzzz");

                WritePropertyValueAlias(writer, scriptValues, "Script", "Unknown");

                WritePropertyValueAlias(writer, generalCategoryValues, "GeneralCategory", "Other");

                WritePropertyValueAlias(writer, biDiClassValues, "BiDiClass", "LeftToRight");

                WritePropertyValueAlias(writer, lineBreakValues, "LineBreakClass", "Unknown");

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }
        }

        public static List<(string name, string tag, string comment)> GetPropertyValueAliases(string property)
        {
            var data = new List<(string name, string tag, string comment)>();

            using (var client = new HttpClient())
            {
                using (var result = client.GetAsync("https://www.unicode.org/Public/UCD/latest/ucd/PropertyValueAliases.txt").GetAwaiter().GetResult())
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

                            var name = elements[0].Trim().Replace("_", string.Empty);

                            var comment = string.Empty;

                            if (elements.Length > 1)
                            {
                                comment = elements[1];
                            }

                            data.Add((name, tag, comment));
                        }
                    }
                }
            }

            return data;
        }

        private static void WritePropertyValueAliasGetTag(TextWriter writer,
            IEnumerable<(string name, string tag, string comment)> values, string typeName, string defaultValue)
        {
            writer.WriteLine($"        private static readonly Dictionary<{typeName}, string> s_{typeName.ToLower()}ToTag = ");
            writer.WriteLine($"            new Dictionary<{typeName}, string>{{");

            foreach (var (name, tag, comment) in values)
            {
                writer.WriteLine($"                {{ {typeName}.{name}, \"{tag}\"}},");
            }

            writer.WriteLine("        };");

            writer.WriteLine();

            writer.WriteLine($"        public static string GetTag({typeName} {typeName.ToLower()})");
            writer.WriteLine("        {");
            writer.WriteLine($"            if(!s_{typeName.ToLower()}ToTag.ContainsKey({typeName.ToLower()}))");
            writer.WriteLine("            {");
            writer.WriteLine($"                return \"{defaultValue}\";");
            writer.WriteLine("            }");
            writer.WriteLine($"            return s_{typeName.ToLower()}ToTag[{typeName.ToLower()}];");
            writer.WriteLine("        }");

            writer.WriteLine();
        }

        private static void WritePropertyValueAlias(TextWriter writer,
            IEnumerable<(string name, string tag, string comment)> values, string typeName, string defaultValue)
        {
            writer.WriteLine($"        private static readonly Dictionary<string, {typeName}> s_tagTo{typeName} = ");
            writer.WriteLine($"            new Dictionary<string,{typeName}>{{");

            foreach (var (name, tag, comment) in values)
            {
                writer.WriteLine($"                {{ \"{tag}\", {typeName}.{name}}},");
            }

            writer.WriteLine("        };");

            writer.WriteLine();

            writer.WriteLine($"        public static {typeName} Get{typeName}(string tag)");
            writer.WriteLine("        {");
            writer.WriteLine($"            if(!s_tagTo{typeName}.ContainsKey(tag))");
            writer.WriteLine("            {");
            writer.WriteLine($"                return {typeName}.{defaultValue};");
            writer.WriteLine("            }");
            writer.WriteLine($"            return s_tagTo{typeName}[tag];");
            writer.WriteLine("        }");

            writer.WriteLine();
        }
    }
}
