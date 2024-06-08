using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Tmds.DBus.SourceGenerator
{
    public partial class DBusSourceGenerator
    {
        private static CompilationUnitSyntax MakePropertyChangesClass() => MakeCompilationUnit(
            NamespaceDeclaration(IdentifierName("Tmds.DBus.SourceGenerator"))
                .AddMembers(
                    RecordDeclaration(Token(SyntaxKind.RecordKeyword), "PropertyChanges")
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .AddTypeParameterListParameters(TypeParameter(Identifier("TProperties")))
                        .AddParameterListParameters(
                            Parameter(Identifier("Properties"))
                                .WithType(IdentifierName("TProperties")),
                            Parameter(Identifier("Invalidated"))
                                .WithType(ArrayType(PredefinedType(Token(SyntaxKind.StringKeyword))).AddRankSpecifiers(ArrayRankSpecifier())),
                            Parameter(Identifier("Changed"))
                                .WithType(ArrayType(PredefinedType(Token(SyntaxKind.StringKeyword))).AddRankSpecifiers(ArrayRankSpecifier())))
                        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
                        .AddMembers(
                            MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), Identifier("HasChanged"))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                .AddParameterListParameters(
                                    Parameter(Identifier("property")).WithType(PredefinedType(Token(SyntaxKind.StringKeyword))))
                                .WithExpressionBody(
                                    ArrowExpressionClause(
                                        BinaryExpression(SyntaxKind.NotEqualsExpression,
                                            InvocationExpression(
                                                    MakeMemberAccessExpression("Array", "IndexOf"))
                                                .AddArgumentListArguments(
                                                    Argument(IdentifierName("Changed")),
                                                    Argument(IdentifierName("property"))),
                                            MakeLiteralExpression(-1))))
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                            MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), Identifier("IsInvalidated"))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                .AddParameterListParameters(
                                    Parameter(Identifier("property")).WithType(PredefinedType(Token(SyntaxKind.StringKeyword))))
                                .WithExpressionBody(
                                    ArrowExpressionClause(
                                        BinaryExpression(SyntaxKind.NotEqualsExpression,
                                            InvocationExpression(
                                                    MakeMemberAccessExpression("Array", "IndexOf"))
                                                .AddArgumentListArguments(
                                                    Argument(IdentifierName("Invalidated")),
                                                    Argument(IdentifierName("property"))),
                                            MakeLiteralExpression(-1))))
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                        .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken))));

        private static CompilationUnitSyntax MakeSignalHelperClass()
        {
            MethodDeclarationSyntax watchSignalMethod = MethodDeclaration(ParseTypeName("ValueTask<IDisposable>"), "WatchSignalAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("connection"))
                        .WithType(ParseTypeName("Connection")),
                    Parameter(Identifier("rule"))
                        .WithType(ParseTypeName("MatchRule")),
                    Parameter(Identifier("handler"))
                        .WithType(ParseTypeName("Action<Exception?>")),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))))
                .WithBody(
                    Block(
                        ReturnStatement(
                            InvocationExpression(
                                    MakeMemberAccessExpression("connection", "AddMatchAsync"))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName("rule")),
                                    Argument(
                                        ParenthesizedLambdaExpression()
                                            .AddModifiers(Token(SyntaxKind.StaticKeyword))
                                            .AddParameterListParameters(
                                                Parameter(Identifier("_")),
                                                Parameter(Identifier("_")))
                                            .WithExpressionBody(
                                                PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                                                    LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                                    Argument(
                                        ParenthesizedLambdaExpression()
                                            .AddModifiers(Token(SyntaxKind.StaticKeyword))
                                            .AddParameterListParameters(
                                                Parameter(Identifier("e"))
                                                    .WithType(ParseTypeName("Exception")),
                                                Parameter(Identifier("_"))
                                                    .WithType(PredefinedType(Token(SyntaxKind.ObjectKeyword))),
                                                Parameter(Identifier("_"))
                                                    .WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))),
                                                Parameter(Identifier("handlerState"))
                                                    .WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))))
                                            .WithExpressionBody(
                                                InvocationExpression(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            ParenthesizedExpression(
                                                                CastExpression(ParseTypeName("Action<Exception?>"),
                                                                    PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                                                                        IdentifierName("handlerState")))),
                                                            IdentifierName("Invoke")))
                                                    .AddArgumentListArguments(
                                                        Argument(IdentifierName("e"))))),
                                    Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    Argument(IdentifierName("handler")),
                                    Argument(IdentifierName("emitOnCapturedContext"))))));

            MethodDeclarationSyntax watchSignalWithReadMethod = MethodDeclaration(ParseTypeName("ValueTask<IDisposable>"), "WatchSignalAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddTypeParameterListParameters(
                    TypeParameter("T"))
                .AddParameterListParameters(
                    Parameter(Identifier("connection"))
                        .WithType(ParseTypeName("Connection")),
                    Parameter(Identifier("rule"))
                        .WithType(ParseTypeName("MatchRule")),
                    Parameter(Identifier("reader"))
                        .WithType(ParseTypeName("MessageValueReader<T>")),
                    Parameter(Identifier("handler"))
                        .WithType(ParseTypeName("Action<Exception?, T>")),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))))
                .WithBody(
                    Block(
                        ReturnStatement(
                            InvocationExpression(
                                    MakeMemberAccessExpression("connection", "AddMatchAsync"))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName("rule")),
                                    Argument(IdentifierName("reader")),
                                    Argument(
                                        ParenthesizedLambdaExpression()
                                            .AddModifiers(Token(SyntaxKind.StaticKeyword))
                                            .AddParameterListParameters(
                                                Parameter(Identifier("e")).WithType(ParseTypeName("Exception")),
                                                Parameter(Identifier("arg"))
                                                    .WithType(ParseTypeName("T")),
                                                Parameter(Identifier("readerState"))
                                                    .WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))),
                                                Parameter(Identifier("handlerState"))
                                                    .WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))))
                                            .WithExpressionBody(
                                                InvocationExpression(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            ParenthesizedExpression(
                                                                CastExpression(ParseTypeName("Action<Exception?, T>"),
                                                                    PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                                                                        IdentifierName("handlerState")))),
                                                            IdentifierName("Invoke")))
                                                    .AddArgumentListArguments(
                                                        Argument(IdentifierName("e")),
                                                        Argument(IdentifierName("arg"))))),
                                    Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    Argument(IdentifierName("handler")),
                                    Argument(IdentifierName("emitOnCapturedContext"))))));

            MethodDeclarationSyntax watchPropertiesMethod = MethodDeclaration(ParseTypeName("ValueTask<IDisposable>"), "WatchPropertiesChangedAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddTypeParameterListParameters(
                    TypeParameter("T"))
                .AddParameterListParameters(
                    Parameter(Identifier("connection"))
                        .WithType(ParseTypeName("Connection")),
                    Parameter(Identifier("destination"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("path"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("@interface"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("reader"))
                        .WithType(ParseTypeName("MessageValueReader<PropertyChanges<T>>")),
                    Parameter(Identifier("handler"))
                        .WithType(ParseTypeName("Action<Exception?, PropertyChanges<T>>")),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))))
                .WithBody(
                    Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(ParseTypeName("MatchRule"))
                                .AddVariables(
                                    VariableDeclarator("rule")
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(ParseTypeName("MatchRule"))
                                                    .WithInitializer(
                                                        InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                                                            .AddExpressions(
                                                                MakeAssignmentExpression(IdentifierName("Type"),
                                                                    MakeMemberAccessExpression("MessageType", "Signal")),
                                                                MakeAssignmentExpression(IdentifierName("Sender"), IdentifierName("destination")),
                                                                MakeAssignmentExpression(IdentifierName("Path"), IdentifierName("path")),
                                                                MakeAssignmentExpression(IdentifierName("Member"),
                                                                    MakeLiteralExpression("PropertiesChanged")),
                                                                MakeAssignmentExpression(IdentifierName("Interface"),
                                                                    MakeLiteralExpression("org.freedesktop.DBus.Properties")),
                                                                MakeAssignmentExpression(IdentifierName("Arg0"), IdentifierName("@interface")))))))),
                        ReturnStatement(
                            InvocationExpression(
                                    IdentifierName("WatchSignalAsync"))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName("connection")),
                                    Argument(IdentifierName("rule")),
                                    Argument(IdentifierName("reader")),
                                    Argument(IdentifierName("handler")),
                                    Argument(IdentifierName("emitOnCapturedContext"))))));

            return MakeCompilationUnit(
                NamespaceDeclaration(IdentifierName("Tmds.DBus.SourceGenerator"))
                    .AddMembers(
                        ClassDeclaration("SignalHelper")
                            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword))
                            .AddMembers(watchSignalMethod, watchSignalWithReadMethod, watchPropertiesMethod)));
        }

        private const string VariantExtensions = """
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tmds.DBus.Protocol;

// <auto-generated/>
#pragma warning disable
#nullable enable
namespace Tmds.DBus.SourceGenerator
{
    internal static class VariantReader
    {
        public static DBusVariantItem ReadDBusVariant(this ref Reader reader)
        {
            ReadOnlySpan<byte> signature = reader.ReadSignature();
            SignatureReader signatureReader = new(signature);
            if (!signatureReader.TryRead(out DBusType dBusType, out ReadOnlySpan<byte> innerSignature))
                throw new InvalidOperationException("Unable to read empty variant");
            return new DBusVariantItem(Encoding.UTF8.GetString(innerSignature.ToArray()), reader.ReadDBusItem(dBusType, innerSignature));
        }

        private static DBusBasicTypeItem ReadDBusBasicTypeItem(this ref Reader reader, DBusType dBusType) =>
            dBusType switch
            {
                DBusType.Byte => new DBusByteItem(reader.ReadByte()),
                DBusType.Bool => new DBusBoolItem(reader.ReadBool()),
                DBusType.Int16 => new DBusInt16Item(reader.ReadInt16()),
                DBusType.UInt16 => new DBusUInt16Item(reader.ReadUInt16()),
                DBusType.Int32 => new DBusInt32Item(reader.ReadInt32()),
                DBusType.UInt32 => new DBusUInt32Item(reader.ReadUInt32()),
                DBusType.Int64 => new DBusInt64Item(reader.ReadInt64()),
                DBusType.UInt64 => new DBusUInt64Item(reader.ReadUInt64()),
                DBusType.Double => new DBusDoubleItem(reader.ReadDouble()),
                DBusType.String => new DBusStringItem(reader.ReadString()),
                DBusType.ObjectPath => new DBusObjectPathItem(reader.ReadObjectPath()),
                DBusType.Signature => new DBusSignatureItem(new Signature(reader.ReadSignature().ToString())),
                _ => throw new ArgumentOutOfRangeException(nameof(dBusType))
            };

        private static DBusItem ReadDBusItem(this ref Reader reader, DBusType dBusType, ReadOnlySpan<byte> innerSignature)
        {
            switch (dBusType)
            {
                case DBusType.Byte:
                    return new DBusByteItem(reader.ReadByte());
                case DBusType.Bool:
                    return new DBusBoolItem(reader.ReadBool());
                case DBusType.Int16:
                    return new DBusInt16Item(reader.ReadInt16());
                case DBusType.UInt16:
                    return new DBusUInt16Item(reader.ReadUInt16());
                case DBusType.Int32:
                    return new DBusInt32Item(reader.ReadInt32());
                case DBusType.UInt32:
                    return new DBusUInt32Item(reader.ReadUInt32());
                case DBusType.Int64:
                    return new DBusInt64Item(reader.ReadInt64());
                case DBusType.UInt64:
                    return new DBusUInt64Item(reader.ReadUInt64());
                case DBusType.Double:
                    return new DBusDoubleItem(reader.ReadDouble());
                case DBusType.String:
                    return new DBusStringItem(reader.ReadString());
                case DBusType.ObjectPath:
                    return new DBusObjectPathItem(reader.ReadObjectPath());
                case DBusType.Signature:
                    return new DBusSignatureItem(new Signature(reader.ReadSignature().ToString()));
                case DBusType.Array:
                {
                    SignatureReader innerSignatureReader = new(innerSignature);
                    if (!innerSignatureReader.TryRead(out DBusType innerDBusType, out ReadOnlySpan<byte> innerArraySignature))
                       throw new InvalidOperationException("Failed to deserialize array item");
                    List<DBusItem> items = new();
                    ArrayEnd arrayEnd = reader.ReadArrayStart(innerDBusType);
                    while (reader.HasNext(arrayEnd))
                        items.Add(reader.ReadDBusItem(innerDBusType, innerArraySignature));
                    return new DBusArrayItem(innerDBusType, items);
                }
                case DBusType.DictEntry:
                {
                    SignatureReader innerSignatureReader = new(innerSignature);
                    if (!innerSignatureReader.TryRead(out DBusType innerKeyType, out ReadOnlySpan<byte> _) ||
                        !innerSignatureReader.TryRead(out DBusType innerValueType, out ReadOnlySpan<byte> innerValueSignature))
                        throw new InvalidOperationException($"Expected 2 inner types for DictEntry, got {Encoding.UTF8.GetString(innerSignature.ToArray())}");
                    DBusBasicTypeItem key = reader.ReadDBusBasicTypeItem(innerKeyType);
                    DBusItem value = reader.ReadDBusItem(innerValueType, innerValueSignature);
                    return new DBusDictEntryItem(key, value);
                }
                case DBusType.Struct:
                {
                    reader.AlignStruct();
                    List<DBusItem> items = new();
                    SignatureReader innerSignatureReader = new(innerSignature);
                    while (innerSignatureReader.TryRead(out DBusType innerDBusType, out ReadOnlySpan<byte> innerStructSignature))
                        items.Add(reader.ReadDBusItem(innerDBusType, innerStructSignature));
                    return new DBusStructItem(items);
                }
                case DBusType.Variant:
                    return reader.ReadDBusVariant();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal static class VariantWriter
    {
        public static void WriteDBusVariant(this ref MessageWriter writer, DBusVariantItem value)
        {
            writer.WriteSignature(Encoding.UTF8.GetBytes(value.Signature).AsSpan());
            writer.WriteDBusItem(value.Value);
        }

        public static void WriteDBusItem(this ref MessageWriter writer, DBusItem value)
        {
            switch (value)
            {
                case DBusVariantItem variantItem:
                    writer.WriteDBusVariant(variantItem);
                    break;
                case DBusByteItem byteItem:
                    writer.WriteByte(byteItem.Value);
                    break;
                case DBusBoolItem boolItem:
                    writer.WriteBool(boolItem.Value);
                    break;
                case DBusInt16Item int16Item:
                    writer.WriteInt16(int16Item.Value);
                    break;
                case DBusUInt16Item uInt16Item:
                    writer.WriteUInt16(uInt16Item.Value);
                    break;
                case DBusInt32Item int32Item:
                    writer.WriteInt32(int32Item.Value);
                    break;
                case DBusUInt32Item uInt32Item:
                    writer.WriteUInt32(uInt32Item.Value);
                    break;
                case DBusInt64Item int64Item:
                    writer.WriteInt64(int64Item.Value);
                    break;
                case DBusUInt64Item uInt64Item:
                    writer.WriteUInt64(uInt64Item.Value);
                    break;
                case DBusDoubleItem doubleItem:
                    writer.WriteDouble(doubleItem.Value);
                    break;
                case DBusStringItem stringItem:
                    writer.WriteString(stringItem.Value);
                    break;
                case DBusObjectPathItem objectPathItem:
                    writer.WriteObjectPath(objectPathItem.Value);
                    break;
                case DBusSignatureItem signatureItem:
                    writer.WriteSignature(signatureItem.Value.ToString());
                    break;
                case DBusArrayItem arrayItem:
                    ArrayStart arrayStart = writer.WriteArrayStart(arrayItem.ArrayType);
                    foreach (DBusItem item in arrayItem)
                        writer.WriteDBusItem(item);
                    writer.WriteArrayEnd(arrayStart);
                    break;
                case DBusDictEntryItem dictEntryItem:
                    writer.WriteStructureStart();
                    writer.WriteDBusItem(dictEntryItem.Key);
                    writer.WriteDBusItem(dictEntryItem.Value);
                    break;
                case DBusStructItem structItem:
                    writer.WriteStructureStart();
                    foreach (DBusItem item in structItem)
                        writer.WriteDBusItem(item);
                    break;
                case DBusByteArrayItem byteArrayItem:
                    ArrayStart byteArrayStart = writer.WriteArrayStart(DBusType.Byte);
                    foreach (byte item in byteArrayItem)
                        writer.WriteByte(item);
                    writer.WriteArrayEnd(byteArrayStart);
                    break;
            }
        }
    }

    internal abstract class DBusItem { }

    internal abstract class DBusBasicTypeItem : DBusItem { }

    internal class DBusVariantItem : DBusItem
    {
        public DBusVariantItem(string signature, DBusItem value)
        {
            Signature = signature;
            Value = value;
        }

        public string Signature { get; }

        public DBusItem Value { get; }
    }

    internal class DBusByteItem : DBusBasicTypeItem
    {
        public DBusByteItem(byte value)
        {
            Value = value;
        }

        public byte Value { get; }
    }

    internal class DBusBoolItem : DBusBasicTypeItem
    {
        public DBusBoolItem(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
    }

    internal class DBusInt16Item : DBusBasicTypeItem
    {
        public DBusInt16Item(short value)
        {
            Value = value;
        }

        public short Value { get; }
    }

    internal class DBusUInt16Item : DBusBasicTypeItem
    {
        public DBusUInt16Item(ushort value)
        {
            Value = value;
        }

        public ushort Value { get; }
    }

    internal class DBusInt32Item : DBusBasicTypeItem
    {
        public DBusInt32Item(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    internal class DBusUInt32Item : DBusBasicTypeItem
    {
        public DBusUInt32Item(uint value)
        {
            Value = value;
        }

        public uint Value { get; }
    }

    internal class DBusInt64Item : DBusBasicTypeItem
    {
        public DBusInt64Item(long value)
        {
            Value = value;
        }

        public long Value { get; }
    }

    internal class DBusUInt64Item : DBusBasicTypeItem
    {
        public DBusUInt64Item(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
    }

    internal class DBusDoubleItem : DBusBasicTypeItem
    {
        public DBusDoubleItem(double value)
        {
            Value = value;
        }

        public double Value { get; }
    }

    internal class DBusStringItem : DBusBasicTypeItem
    {
        public DBusStringItem(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    internal class DBusObjectPathItem : DBusBasicTypeItem
    {
        public DBusObjectPathItem(ObjectPath value)
        {
            Value = value;
        }

        public ObjectPath Value { get; }
    }

    internal class DBusSignatureItem : DBusBasicTypeItem
    {
        public DBusSignatureItem(Signature value)
        {
            Value = value;
        }

        public Signature Value { get; }
    }

    internal class DBusArrayItem : DBusItem, IReadOnlyList<DBusItem>
    {
        private readonly IReadOnlyList<DBusItem> _value;

        public DBusArrayItem(DBusType arrayType, IReadOnlyList<DBusItem> value)
        {
            ArrayType = arrayType;
            _value = value;
        }

        public DBusType ArrayType { get; }

        public IEnumerator<DBusItem> GetEnumerator() => _value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();

        public int Count => _value.Count;

        public DBusItem this[int index] => _value[index];
    }

    internal class DBusDictEntryItem : DBusItem
    {
        public DBusDictEntryItem(DBusBasicTypeItem key, DBusItem value)
        {
            Key = key;
            Value = value;
        }

        public DBusBasicTypeItem Key { get; }

        public DBusItem Value { get; }
    }

    internal class DBusStructItem : DBusItem, IReadOnlyList<DBusItem>
    {
        private readonly IReadOnlyList<DBusItem> _value;

        public DBusStructItem(IReadOnlyList<DBusItem> value)
        {
            _value = value;
        }

        public IEnumerator<DBusItem> GetEnumerator() => _value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();

        public int Count => _value.Count;

        public DBusItem this[int index] => _value[index];
    }

    internal class DBusByteArrayItem : DBusItem, IReadOnlyList<byte>
    {
        private readonly IReadOnlyList<byte> _value;

        public DBusByteArrayItem(IReadOnlyList<byte> value)
        {
            _value = value;
        }

        public IEnumerator<byte> GetEnumerator() => _value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_value).GetEnumerator();

        public int Count => _value.Count;

        public byte this[int index] => _value[index];
    }
}
""";
    }
}
