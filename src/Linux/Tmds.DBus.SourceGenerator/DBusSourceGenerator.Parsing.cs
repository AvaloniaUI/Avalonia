using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Tmds.DBus.SourceGenerator
{
    public partial class DBusSourceGenerator
    {
        private static readonly DBusValue _byteValue = new() { Type = "y" };
        private static readonly DBusValue _boolValue = new() { Type = "b" };
        private static readonly DBusValue _int16Value = new() { Type = "n" };
        private static readonly DBusValue _uInt16Value = new() { Type = "q" };
        private static readonly DBusValue _int32Value = new() { Type = "i" };
        private static readonly DBusValue _uInt32Value = new() { Type = "u" };
        private static readonly DBusValue _int64Value = new() { Type = "x" };
        private static readonly DBusValue _uInt64Value = new() { Type = "t" };
        private static readonly DBusValue _doubleValue = new() { Type = "d" };
        private static readonly DBusValue _stringValue = new() { Type = "s" };
        private static readonly DBusValue _objectPathValue = new() { Type = "o" };
        private static readonly DBusValue _signatureValue = new() { Type = "g" };
        private static readonly DBusValue _variantValue = new() { Type = "v" };
        private static readonly DBusValue _unixFdValue = new() { Type = "h" };

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

        private static string Camelize(string name)
        {
            StringBuilder sb = new(Pascalize(name));
            sb[0] = char.ToLowerInvariant(sb[0]);
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

        private static TypeSyntax? ParseReturnType(IReadOnlyList<DBusValue>? dBusValues, AccessMode accessMode) => dBusValues?.Count switch
        {
            0 or null => null,
            1 => GetDotnetType(dBusValues[0], accessMode),
            _ => TupleType()
                .AddElements(
                    dBusValues.Select((dBusValue, i) => TupleElement(GetDotnetType(dBusValue, accessMode))
                            .WithIdentifier(
                                Identifier(dBusValue.Name is not null ? SanitizeIdentifier(Pascalize(dBusValue.Name)) : $"Item{i + 1}")))
                        .ToArray())
        };

        private static TypeSyntax ParseTaskReturnType(IReadOnlyList<DBusValue>? dBusValues, AccessMode accessMode) => dBusValues?.Count switch
        {
            0 or null => IdentifierName("Task"),
            _ => GenericName("Task")
                .AddTypeArgumentListArguments(
                    ParseReturnType(dBusValues, accessMode)!)
        };

        private static TypeSyntax ParseValueTaskReturnType(IReadOnlyList<DBusValue>? dBusValues, AccessMode accessMode) => dBusValues?.Count switch
        {
            0 or null => IdentifierName("ValueTask"),
            _ => GenericName("ValueTask")
                .AddTypeArgumentListArguments(
                    ParseReturnType(dBusValues, accessMode)!)
        };

        private static TypeSyntax ParseTaskCompletionSourceType(IReadOnlyList<DBusValue>? dBusValues, AccessMode accessMode) => dBusValues?.Count switch
        {
            0 or null => GenericName("TaskCompletionSource")
                .AddTypeArgumentListArguments(
                    PredefinedType(
                        Token(SyntaxKind.BoolKeyword))),
            _ => GenericName("TaskCompletionSource")
                .AddTypeArgumentListArguments(
                    ParseReturnType(dBusValues, accessMode)!)
        };

        private static ParameterListSyntax ParseParameterList(IEnumerable<DBusValue> inArgs, AccessMode accessMode) => ParameterList(
            SeparatedList(
                inArgs.Select((x, i) =>
                    Parameter(Identifier(x.Name is not null ? SanitizeIdentifier(Camelize(x.Name)) : $"arg{i}")).WithType(GetDotnetType(x, accessMode)))));

        private static string SanitizeSignature(string signature) =>
            signature.Replace('{', 'e')
                .Replace("}", null)
                .Replace('(', 'r')
                .Replace(')', 'z');

        private static string SanitizeIdentifier(string identifier)
        {
            bool isAnyKeyword = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None;
            return isAnyKeyword ? $"@{identifier}" : identifier;
        }

        internal static (DBusValue DBusValue, DBusValue[] InnerDBusTypes, DBusType DBusType) ParseDBusValue(string signature) =>
            SignatureReader.Transform<(DBusValue, DBusValue[], DBusType)>(Encoding.ASCII.GetBytes(signature), MapDBusToDotNet);

        private static (DBusValue, DBusValue[], DBusType) MapDBusToDotNet(DBusType dBusType, (DBusValue, DBusValue[], DBusType)[] inner)
        {
            DBusValue[] innerDBusTypes = inner.Select(static x => x.Item1).ToArray();
            return dBusType switch
            {
                DBusType.Byte => (_byteValue, innerDBusTypes, dBusType),
                DBusType.Bool => (_boolValue, innerDBusTypes, dBusType),
                DBusType.Int16 => (_int16Value, innerDBusTypes, dBusType),
                DBusType.UInt16 => (_uInt16Value, innerDBusTypes, dBusType),
                DBusType.Int32 => (_int32Value, innerDBusTypes, dBusType),
                DBusType.UInt32 => (_uInt32Value, innerDBusTypes, dBusType),
                DBusType.Int64 => (_int64Value, innerDBusTypes, dBusType),
                DBusType.UInt64 => (_uInt64Value, innerDBusTypes, dBusType),
                DBusType.Double => (_doubleValue, innerDBusTypes, dBusType),
                DBusType.String => (_stringValue, innerDBusTypes, dBusType),
                DBusType.ObjectPath => (_objectPathValue, innerDBusTypes, dBusType),
                DBusType.Signature => (_signatureValue, innerDBusTypes, dBusType),
                DBusType.Variant => (_variantValue, innerDBusTypes, dBusType),
                DBusType.UnixFd => (_unixFdValue, innerDBusTypes, dBusType),
                DBusType.Array => (new DBusValue { Type = $"a{ParseSignature(innerDBusTypes)}"}, innerDBusTypes, dBusType),
                DBusType.DictEntry => (new DBusValue { Type = $"a{{{ParseSignature(innerDBusTypes)}}}"}, innerDBusTypes, dBusType),
                DBusType.Struct => (new DBusValue { Type = $"({ParseSignature(innerDBusTypes)})"}, innerDBusTypes, dBusType),
                _ => throw new ArgumentOutOfRangeException(nameof(dBusType), dBusType, $"Cannot parse DBusType with value {dBusType}")
            };
        }

        private static Dictionary<string, StructDeclarationSyntax> ComplexTypeDictionary =
            new Dictionary<string, StructDeclarationSyntax>();

        private static TypeSyntax GetDotnetType(DBusValue dBusValue, AccessMode accessMode, bool nullable = false)
        {
            switch (dBusValue.DBusType)
            {
                case DBusType.Byte:
                    return PredefinedType(Token(SyntaxKind.ByteKeyword));
                case DBusType.Bool:
                    return PredefinedType(Token(SyntaxKind.BoolKeyword));
                case DBusType.Int16:
                    return PredefinedType(Token(SyntaxKind.ShortKeyword));
                case DBusType.UInt16:
                    return PredefinedType(Token(SyntaxKind.UShortKeyword));
                case DBusType.Int32:
                    return PredefinedType(Token(SyntaxKind.IntKeyword));
                case DBusType.UInt32:
                    return PredefinedType(Token(SyntaxKind.UIntKeyword));
                case DBusType.Int64:
                    return PredefinedType(Token(SyntaxKind.LongKeyword));
                case DBusType.UInt64:
                    return PredefinedType(Token(SyntaxKind.ULongKeyword));
                case DBusType.Double:
                    return PredefinedType(Token(SyntaxKind.DoubleKeyword));
                case DBusType.String:
                    TypeSyntax str = PredefinedType(Token(SyntaxKind.StringKeyword));
                    if (nullable)
                        str = NullableType(str);
                    return str;
                case DBusType.ObjectPath:
                    return IdentifierName("ObjectPath");
                case DBusType.Signature:
                    return IdentifierName("Signature");
                case DBusType.Variant when accessMode == AccessMode.Read:
                    return IdentifierName("VariantValue");
                case DBusType.Variant when accessMode == AccessMode.Write:
                    return IdentifierName("Variant");
                case DBusType.UnixFd:
                    return IdentifierName("SafeFileHandle");
                case DBusType.Array:
                    TypeSyntax arr = ArrayType(
                            GetDotnetType(dBusValue.InnerDBusTypes![0], accessMode, nullable))
                        .AddRankSpecifiers(ArrayRankSpecifier()
                            .AddSizes(OmittedArraySizeExpression()));
                    if (nullable)
                        arr = NullableType(arr);
                    return arr;
                case DBusType.DictEntry:
                    TypeSyntax dict = GenericName("Dictionary")
                        .AddTypeArgumentListArguments(
                            GetDotnetType(dBusValue.InnerDBusTypes![0], accessMode),
                            GetDotnetType(dBusValue.InnerDBusTypes![1], accessMode, nullable));
                    if (nullable)
                        dict = NullableType(dict);
                    return dict;
                case DBusType.Struct when dBusValue.InnerDBusTypes!.Length == 1:
                    return GenericName("ValueTuple")
                        .AddTypeArgumentListArguments(
                            GetDotnetType(dBusValue.InnerDBusTypes![0], accessMode, nullable));
                case DBusType.Struct when dBusValue.InnerDBusTypes!.Length <= 7:
                    return TupleType()
                        .AddElements(dBusValue.InnerDBusTypes!.Select(dbusType => TupleElement(
                                GetDotnetType(dbusType, accessMode, nullable)))
                            .ToArray());

                case DBusType.Struct when dBusValue.InnerDBusTypes!.Length > 7:

                    var typeId = SanitizeSignature(dBusValue.Type!);

                    if (!ComplexTypeDictionary.ContainsKey(typeId))
                    {
                        var innerTypes = dBusValue.InnerDBusTypes!
                            .Select(dbusType => GetDotnetType(dbusType, accessMode, nullable))
                            .Select((syntax, i) => (syntax, i)).ToArray();
                        var complexType = StructDeclaration(typeId)
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .AddMembers(innerTypes
                                .Select(x =>
                                    FieldDeclaration(
                                            VariableDeclaration(x.syntax)
                                                .WithVariables(
                                                    SingletonSeparatedList(
                                                        VariableDeclarator(
                                                            Identifier($"Item{x.i}")))))
                                        .WithModifiers(
                                            TokenList(
                                                Token(SyntaxKind.PublicKeyword)))).Cast<MemberDeclarationSyntax>()
                                .ToArray()
                            )
                            .AddMembers(
                                ConstructorDeclaration(Identifier(typeId))
                                    .WithParameterList(
                                        ParameterList(SeparatedList(
                                            innerTypes.Select(x =>
                                                Parameter(Identifier($"_arg{x.i}")).WithType(x.syntax))))
                                    ).WithBody(Block()
                                        .AddStatements(
                                            innerTypes.Select(x => ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    IdentifierName($"Item{x.i}"),
                                                    IdentifierName($"_arg{x.i}")))).Cast<StatementSyntax>().ToArray()
                                        ))
                            );

                        ComplexTypeDictionary.Add(typeId, complexType);
                    }

                    return IdentifierName(typeId);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dBusValue.DBusType), dBusValue.DBusType,
                        $"Cannot parse DBusType with value {dBusValue.DBusType}");
            }
        }
    }
}
