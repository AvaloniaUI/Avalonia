using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Tmds.DBus.SourceGenerator
{
    public partial class DBusSourceGenerator
    {
        private static string Pascalize(string name, bool camel = false)
        {
            bool upperizeNext = !camel;
            StringBuilder sb = new(name.Length);
            foreach (char och in name)
            {
                char ch = och;
                if (ch is '_' or '.')
                {
                    upperizeNext = true;
                }
                else
                {
                    if (upperizeNext)
                    {
                        ch = char.ToUpperInvariant(ch);
                        upperizeNext = false;
                    }

                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static string? ParseSignature(IReadOnlyList<DBusValue>? dBusValues)
        {
            if (dBusValues is null || dBusValues.Count == 0)
                return null;
            StringBuilder sb = new();
            foreach (DBusValue dBusValue in dBusValues.Where(static argument => argument.Type is not null))
                sb.Append(dBusValue.Type);
            return sb.ToString();
        }

        private static string? ParseReturnType(IReadOnlyList<DBusValue>? dBusValues) => dBusValues?.Count switch
        {
            0 or null => null,
            1 => dBusValues[0].DotNetType,
            _ => TupleOf(dBusValues.Select(static (x, i) => $"{x.DotNetType} {(x.Name is not null ? SanitizeIdentifier(x.Name) : $"Item{i + 1}")}"))
        };

        private static string ParseTaskReturnType(DBusValue dBusValue) => ParseTaskReturnType(new[] { dBusValue });

        private static string ParseTaskReturnType(IReadOnlyList<DBusValue>? dBusValues) => dBusValues?.Count switch
        {
            0 or null => "Task",
            1 => $"Task<{dBusValues[0].DotNetType}>",
            _ => $"Task<{TupleOf(dBusValues.Select(static (x, i) => $"{x.DotNetType} {(x.Name is not null ? SanitizeIdentifier(x.Name) : $"Item{i + 1}")}"))}>"
        };

        private static ParameterListSyntax ParseParameterList(IEnumerable<DBusValue> inArgs) => ParameterList(
            SeparatedList(
                inArgs.Select(static (x, i) =>
                    Parameter(Identifier(x.Name is not null ? SanitizeIdentifier(x.Name) : $"arg{i}")).WithType(ParseTypeName(x.DotNetType)))));

        private static string SanitizeSignature(string signature) =>
            signature.Replace('{', 'e').Replace("}", null).Replace('(', 'r').Replace(')', 'z');

        private static string SanitizeIdentifier(string identifier) => $"@{identifier}";

        internal static (string DotnetType, string[] InnerDotNetTypes, DBusValue DBusValue, DBusValue[] InnerDBusTypes, DBusType DBusType) ParseDBusValue(string signature) =>
            SignatureReader.Transform<(string, string[], DBusValue, DBusValue[], DBusType)>(Encoding.ASCII.GetBytes(signature), MapDBusToDotNet);

        private static (string, string[], DBusValue, DBusValue[], DBusType) MapDBusToDotNet(DBusType dBusType, (string, string[], DBusValue, DBusValue[], DBusType)[] inner)
        {
            string[] innerDotNetTypes = inner.Select(static x => x.Item1).ToArray();
            DBusValue[] innerDBusTypes = inner.Select(static x => x.Item3).ToArray();
            return dBusType switch
            {
                DBusType.Byte => ("byte", innerDotNetTypes, new DBusValue { Type = "y" }, innerDBusTypes, dBusType),
                DBusType.Bool => ("bool", innerDotNetTypes, new DBusValue { Type = "b" }, innerDBusTypes, dBusType),
                DBusType.Int16 => ("short", innerDotNetTypes, new DBusValue { Type = "n" }, innerDBusTypes, dBusType),
                DBusType.UInt16 => ("ushort", innerDotNetTypes, new DBusValue { Type = "q" }, innerDBusTypes, dBusType),
                DBusType.Int32 => ("int", innerDotNetTypes, new DBusValue { Type = "i" }, innerDBusTypes, dBusType),
                DBusType.UInt32 => ("uint", innerDotNetTypes, new DBusValue { Type = "u" }, innerDBusTypes, dBusType),
                DBusType.Int64 => ("long", innerDotNetTypes, new DBusValue { Type = "x" }, innerDBusTypes, dBusType),
                DBusType.UInt64 => ("ulong", innerDotNetTypes, new DBusValue { Type = "t" }, innerDBusTypes, dBusType),
                DBusType.Double => ("double", innerDotNetTypes, new DBusValue { Type = "d" }, innerDBusTypes, dBusType),
                DBusType.String => ("string", innerDotNetTypes, new DBusValue { Type = "s" }, innerDBusTypes, dBusType),
                DBusType.ObjectPath => ("ObjectPath", innerDotNetTypes, new DBusValue { Type = "o" }, innerDBusTypes, dBusType),
                DBusType.Signature => ("Signature", innerDotNetTypes, new DBusValue { Type = "g" }, innerDBusTypes, dBusType),
                DBusType.Variant => ("DBusVariantItem", innerDotNetTypes, new DBusValue { Type = "v" }, innerDBusTypes, dBusType),
                DBusType.UnixFd => ("SafeHandle", innerDotNetTypes, new DBusValue { Type = "h" }, innerDBusTypes, dBusType),
                DBusType.Array => ($"{innerDotNetTypes[0]}[]", innerDotNetTypes, new DBusValue { Type = $"a{ParseSignature(innerDBusTypes)}" }, innerDBusTypes, dBusType),
                DBusType.DictEntry => ($"Dictionary<{innerDotNetTypes[0]}, {innerDotNetTypes[1]}>", innerDotNetTypes, new DBusValue { Type = $"a{{{ParseSignature(innerDBusTypes)}}}" }, innerDBusTypes, dBusType),
                DBusType.Struct when innerDotNetTypes.Length == 1 => ($"ValueTuple<{innerDotNetTypes[0]}>", innerDotNetTypes, new DBusValue { Type = $"({ParseSignature(innerDBusTypes)}" }, innerDBusTypes, dBusType),
                DBusType.Struct => ($"{TupleOf(innerDotNetTypes)}", innerDotNetTypes, new DBusValue { Type = $"({ParseSignature(innerDBusTypes)})" }, innerDBusTypes, dBusType),
                _ => throw new ArgumentOutOfRangeException(nameof(dBusType), dBusType, null)
            };
        }
    }
}
