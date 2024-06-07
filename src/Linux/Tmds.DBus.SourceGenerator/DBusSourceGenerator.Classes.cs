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
            MethodDeclarationSyntax watchSignalMethod = MethodDeclaration(
                    GenericName("ValueTask")
                        .AddTypeArgumentListArguments(
                            IdentifierName("IDisposable")),
                    "WatchSignalAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("connection"))
                        .WithType(IdentifierName("Connection")),
                    Parameter(Identifier("rule"))
                        .WithType(IdentifierName("MatchRule")),
                    Parameter(Identifier("handler"))
                        .WithType(GenericName("Action")
                            .AddTypeArgumentListArguments(
                                NullableType(
                                    IdentifierName("Exception")))),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))),
                    Parameter(Identifier("flags"))
                        .WithType(IdentifierName("ObserverFlags"))
                        .WithDefault(EqualsValueClause(MakeMemberAccessExpression("ObserverFlags", "None"))))
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
                                                    .WithType(IdentifierName("Exception")),
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
                                                                CastExpression(GenericName("Action")
                                                                        .AddTypeArgumentListArguments(
                                                                            NullableType(
                                                                                IdentifierName("Exception"))),
                                                                    PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                                                                        IdentifierName("handlerState")))),
                                                            IdentifierName("Invoke")))
                                                    .AddArgumentListArguments(
                                                        Argument(IdentifierName("e"))))),
                                    Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    Argument(IdentifierName("handler")),
                                    Argument(IdentifierName("emitOnCapturedContext")),
                                    Argument(IdentifierName("flags"))))));

            MethodDeclarationSyntax watchSignalWithReadMethod = MethodDeclaration(
                    GenericName("ValueTask")
                        .AddTypeArgumentListArguments(
                            IdentifierName("IDisposable")),
                    "WatchSignalAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddTypeParameterListParameters(
                    TypeParameter("T"))
                .AddParameterListParameters(
                    Parameter(Identifier("connection"))
                        .WithType(IdentifierName("Connection")),
                    Parameter(Identifier("rule"))
                        .WithType(IdentifierName("MatchRule")),
                    Parameter(Identifier("reader"))
                        .WithType(GenericName("MessageValueReader")
                            .AddTypeArgumentListArguments(
                                IdentifierName("T"))),
                    Parameter(Identifier("handler"))
                        .WithType(GenericName("Action")
                            .AddTypeArgumentListArguments(
                                NullableType(
                                    IdentifierName("Exception")),
                                IdentifierName("T"))),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))),
                    Parameter(Identifier("flags"))
                        .WithType(IdentifierName("ObserverFlags"))
                        .WithDefault(EqualsValueClause(MakeMemberAccessExpression("ObserverFlags", "None"))))
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
                                                Parameter(Identifier("e"))
                                                    .WithType(IdentifierName("Exception")),
                                                Parameter(Identifier("arg"))
                                                    .WithType(IdentifierName("T")),
                                                Parameter(Identifier("readerState"))
                                                    .WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))),
                                                Parameter(Identifier("handlerState"))
                                                    .WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))))
                                            .WithExpressionBody(
                                                InvocationExpression(
                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                            ParenthesizedExpression(
                                                                CastExpression(
                                                                    GenericName("Action")
                                                                        .AddTypeArgumentListArguments(
                                                                            NullableType(
                                                                                IdentifierName("Exception")),
                                                                            IdentifierName("T")),
                                                                    PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                                                                        IdentifierName("handlerState")))),
                                                            IdentifierName("Invoke")))
                                                    .AddArgumentListArguments(
                                                        Argument(IdentifierName("e")),
                                                        Argument(IdentifierName("arg"))))),
                                    Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    Argument(IdentifierName("handler")),
                                    Argument(IdentifierName("emitOnCapturedContext")),
                                    Argument(IdentifierName("flags"))))));

            MethodDeclarationSyntax watchPropertiesMethod = MethodDeclaration(
                    GenericName("ValueTask")
                        .AddTypeArgumentListArguments(
                            IdentifierName("IDisposable")),
                    "WatchPropertiesChangedAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddTypeParameterListParameters(
                    TypeParameter("T"))
                .AddParameterListParameters(
                    Parameter(Identifier("connection"))
                        .WithType(IdentifierName("Connection")),
                    Parameter(Identifier("destination"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("path"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("@interface"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("reader"))
                        .WithType(GenericName("MessageValueReader")
                            .AddTypeArgumentListArguments(
                                GenericName("PropertyChanges")
                                    .AddTypeArgumentListArguments(
                                        IdentifierName("T")))),
                    Parameter(Identifier("handler"))
                        .WithType(GenericName("Action")
                            .AddTypeArgumentListArguments(
                                NullableType(
                                    IdentifierName("Exception")),
                                GenericName("PropertyChanges")
                                    .AddTypeArgumentListArguments(
                                        IdentifierName("T")))),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))),
                    Parameter(Identifier("flags"))
                        .WithType(IdentifierName("ObserverFlags"))
                        .WithDefault(EqualsValueClause(MakeMemberAccessExpression("ObserverFlags", "None"))))
                .WithBody(
                    Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(IdentifierName("MatchRule"))
                                .AddVariables(
                                    VariableDeclarator("rule")
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(IdentifierName("MatchRule"))
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
                                    Argument(IdentifierName("emitOnCapturedContext")),
                                    Argument(IdentifierName("flags"))))));

            return MakeCompilationUnit(
                NamespaceDeclaration(IdentifierName("Tmds.DBus.SourceGenerator"))
                    .AddMembers(
                        ClassDeclaration("SignalHelper")
                            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword))
                            .AddMembers(watchSignalMethod, watchSignalWithReadMethod, watchPropertiesMethod)));
        }

        private static MethodDeclarationSyntax MakeWriteNullableStringMethod() =>
            MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    "WriteNullableString")
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(
                            Identifier("writer"))
                        .WithType(
                            IdentifierName("MessageWriter"))
                        .AddModifiers(
                            Token(SyntaxKind.ThisKeyword),
                            Token(SyntaxKind.RefKeyword)),
                    Parameter(
                            Identifier("value"))
                        .WithType(
                            NullableType(
                                PredefinedType(Token(SyntaxKind.StringKeyword)))))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        InvocationExpression(
                        MakeMemberAccessExpression("writer", "WriteString"))
                            .AddArgumentListArguments(
                                Argument(
                                    BinaryExpression(
                                        SyntaxKind.CoalesceExpression,
                                        IdentifierName("value"),
                                        MakeMemberAccessExpression("string", "Empty"))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        private static MethodDeclarationSyntax MakeWriteObjectPathSafeMethod() =>
            MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    "WriteObjectPathSafe")
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(
                            Identifier("writer"))
                        .WithType(
                            IdentifierName("MessageWriter"))
                        .AddModifiers(
                            Token(SyntaxKind.ThisKeyword),
                            Token(SyntaxKind.RefKeyword)),
                    Parameter(
                            Identifier("value"))
                        .WithType(
                            IdentifierName("ObjectPath")))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        InvocationExpression(
                                MakeMemberAccessExpression("writer", "WriteObjectPath"))
                            .AddArgumentListArguments(
                                Argument(
                                    InvocationExpression(
                                        MakeMemberAccessExpression("value", "ToString"))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
