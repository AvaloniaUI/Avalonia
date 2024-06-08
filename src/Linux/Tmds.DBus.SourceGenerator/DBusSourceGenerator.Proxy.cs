using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Tmds.DBus.SourceGenerator
{
    public partial class DBusSourceGenerator
    {
        private ClassDeclarationSyntax GenerateProxy(DBusInterface dBusInterface)
        {
            string identifier = Pascalize(dBusInterface.Name!);
            ClassDeclarationSyntax cl = ClassDeclaration(identifier)
                .AddModifiers(Token(SyntaxKind.InternalKeyword));

            FieldDeclarationSyntax interfaceConst = MakePrivateStringConst("Interface", dBusInterface.Name!, PredefinedType(Token(SyntaxKind.StringKeyword)));
            FieldDeclarationSyntax connectionField = MakePrivateReadOnlyField("_connection", ParseTypeName("Connection"));
            FieldDeclarationSyntax destinationField = MakePrivateReadOnlyField("_destination", PredefinedType(Token(SyntaxKind.StringKeyword)));
            FieldDeclarationSyntax pathField = MakePrivateReadOnlyField("_path", PredefinedType(Token(SyntaxKind.StringKeyword)));

            ConstructorDeclarationSyntax ctor = ConstructorDeclaration(identifier)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("connection")).WithType(ParseTypeName("Connection")),
                    Parameter(Identifier("destination")).WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("path")).WithType(PredefinedType(Token(SyntaxKind.StringKeyword))))
                .WithBody(
                    Block(
                        MakeAssignmentExpressionStatement("_connection", "connection"),
                        MakeAssignmentExpressionStatement("_destination", "destination"),
                        MakeAssignmentExpressionStatement("_path", "path")));

            cl = cl.AddMembers(interfaceConst, connectionField, destinationField, pathField, ctor);
            AddProxyMethods(ref cl, dBusInterface);
            AddProxySignals(ref cl, dBusInterface);
            AddProperties(ref cl, dBusInterface);

            return cl;
        }

        private void AddProxyMethods(ref ClassDeclarationSyntax cl, DBusInterface dBusInterface)
        {
            if (dBusInterface.Methods is null)
                return;
            foreach (DBusMethod dBusMethod in dBusInterface.Methods)
            {
                DBusArgument[]? inArgs = dBusMethod.Arguments?.Where(static m => m.Direction is null or "in").ToArray();
                DBusArgument[]? outArgs = dBusMethod.Arguments?.Where(static m => m.Direction == "out").ToArray();

                string returnType = ParseTaskReturnType(outArgs);

                ArgumentListSyntax args = ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            InvocationExpression(
                                IdentifierName("CreateMessage")))));

                if (outArgs?.Length > 0)
                {
                    args = args.AddArguments(
                        Argument(
                            MakeMemberAccessExpression("ReaderExtensions", GetOrAddReadMessageMethod(outArgs))));
                }

                StatementSyntax[] statements = inArgs?.Select((x, i) => ExpressionStatement(
                        InvocationExpression(
                                MakeMemberAccessExpression("writer", GetOrAddWriteMethod(x)))
                            .AddArgumentListArguments(
                                Argument(IdentifierName(x.Name is not null ? SanitizeIdentifier(x.Name) : $"arg{i}")))))
                    .Cast<StatementSyntax>()
                    .ToArray() ?? Array.Empty<StatementSyntax>();

                BlockSyntax createMessageBody = MakeCreateMessageBody(IdentifierName("Interface"), dBusMethod.Name!, ParseSignature(inArgs), statements);

                MethodDeclarationSyntax proxyMethod = MethodDeclaration(ParseTypeName(returnType), $"{Pascalize(dBusMethod.Name!)}Async")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));

                if (inArgs is not null)
                    proxyMethod = proxyMethod.WithParameterList(ParseParameterList(inArgs));

                cl = cl.AddMembers(proxyMethod.WithBody(MakeCallMethodReturnBody(args, createMessageBody)));
            }
        }

        private void AddProxySignals(ref ClassDeclarationSyntax cl, DBusInterface dBusInterface)
        {
            if (dBusInterface.Signals is null) return;
            foreach (DBusSignal dBusSignal in dBusInterface.Signals)
            {
                DBusArgument[]? outArgs = dBusSignal.Arguments?.Where(static x => x.Direction is null or "out").ToArray();
                string? returnType = ParseReturnType(outArgs);

                ParameterListSyntax parameters = ParameterList();

                parameters = returnType is not null
                    ? parameters.AddParameters(
                        Parameter(Identifier("handler"))
                            .WithType(ParseTypeName($"Action<Exception?, {returnType}>")))
                    : parameters.AddParameters(
                        Parameter(Identifier("handler"))
                            .WithType(ParseTypeName("Action<Exception?>")));

                parameters = parameters.AddParameters(
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.TrueLiteralExpression))));

                ArgumentListSyntax arguments = ArgumentList()
                    .AddArguments(Argument(IdentifierName("_connection")),
                        Argument(IdentifierName("rule")));

                if (outArgs is not null)
                {
                    arguments = arguments.AddArguments(
                        Argument(MakeMemberAccessExpression("ReaderExtensions", GetOrAddReadMessageMethod(outArgs))));
                }

                arguments = arguments.AddArguments(
                    Argument(IdentifierName("handler")),
                    Argument(IdentifierName("emitOnCapturedContext")));

                MethodDeclarationSyntax watchSignalMethod = MethodDeclaration(ParseTypeName("ValueTask<IDisposable>"), $"Watch{Pascalize(dBusSignal.Name!)}Async")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(parameters)
                    .WithBody(
                        Block(
                            LocalDeclarationStatement(
                                VariableDeclaration(ParseTypeName("MatchRule"))
                                    .AddVariables(
                                        VariableDeclarator("rule")
                                            .WithInitializer(
                                                EqualsValueClause(MakeMatchRule(dBusSignal))))),
                            ReturnStatement(
                                InvocationExpression(
                                        MakeMemberAccessExpression("SignalHelper", "WatchSignalAsync"))
                                    .WithArgumentList(arguments))));

                cl = cl.AddMembers(watchSignalMethod);
            }
        }

        private static ObjectCreationExpressionSyntax MakeMatchRule(DBusSignal dBusSignal) =>
            ObjectCreationExpression(ParseTypeName("MatchRule"))
                .WithInitializer(
                    InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                        .AddExpressions(
                            MakeAssignmentExpression(IdentifierName("Type"), MakeMemberAccessExpression("MessageType", "Signal")),
                            MakeAssignmentExpression(IdentifierName("Sender"), IdentifierName("_destination")),
                            MakeAssignmentExpression(IdentifierName("Path"), IdentifierName("_path")),
                            MakeAssignmentExpression(IdentifierName("Member"), MakeLiteralExpression(dBusSignal.Name!)),
                            MakeAssignmentExpression(IdentifierName("Interface"), IdentifierName("Interface"))));

        private void AddWatchPropertiesChanged(ref ClassDeclarationSyntax cl) =>
            cl = cl.AddMembers(MethodDeclaration(ParseTypeName("ValueTask<IDisposable>"), "WatchPropertiesChangedAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("handler"))
                        .WithType(ParseTypeName("Action<Exception?, PropertyChanges<Properties>>")),
                    Parameter(Identifier("emitOnCapturedContext"))
                        .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                        .WithDefault(
                            EqualsValueClause(
                                LiteralExpression(SyntaxKind.TrueLiteralExpression))))
                .WithBody(
                    Block(
                        ReturnStatement(
                            InvocationExpression(
                                    MakeMemberAccessExpression("SignalHelper", "WatchPropertiesChangedAsync"))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName("_connection")),
                                    Argument(IdentifierName("_destination")),
                                    Argument(IdentifierName("_path")),
                                    Argument(IdentifierName("Interface")),
                                    Argument(IdentifierName("ReadMessage")),
                                    Argument(IdentifierName("handler")),
                                    Argument(IdentifierName("emitOnCapturedContext")))),
                        LocalFunctionStatement(ParseTypeName("PropertyChanges<Properties>"), "ReadMessage")
                            .AddModifiers(Token(SyntaxKind.StaticKeyword))
                            .AddParameterListParameters(
                                Parameter(Identifier("message"))
                                    .WithType(ParseTypeName("Message")),
                                Parameter(Identifier("_"))
                                    .WithType(
                                        NullableType(
                                            PredefinedType(Token(SyntaxKind.ObjectKeyword)))))
                            .WithBody(
                                Block(
                                    LocalDeclarationStatement(
                                        VariableDeclaration(ParseTypeName("Reader"))
                                            .AddVariables(
                                                VariableDeclarator("reader")
                                                    .WithInitializer(
                                                        EqualsValueClause(
                                                            InvocationExpression(
                                                                MakeMemberAccessExpression("message", "GetBodyReader")))))),
                                    ExpressionStatement(
                                        InvocationExpression(
                                            MakeMemberAccessExpression("reader", "ReadString"))),
                                    LocalDeclarationStatement(
                                        VariableDeclaration(ParseTypeName("List<string>"))
                                            .AddVariables(
                                                VariableDeclarator("changed")
                                                    .WithInitializer(
                                                        EqualsValueClause(
                                                            ImplicitObjectCreationExpression())))),
                                    ReturnStatement(
                                        InvocationExpression(
                                                ObjectCreationExpression(ParseTypeName("PropertyChanges<Properties>")))
                                            .AddArgumentListArguments(
                                                Argument(InvocationExpression(
                                                        IdentifierName("ReadProperties"))
                                                    .AddArgumentListArguments(
                                                        Argument(IdentifierName("reader"))
                                                            .WithRefKindKeyword(Token(SyntaxKind.RefKeyword)),
                                                        Argument(IdentifierName("changed")))),
                                                Argument(
                                                    InvocationExpression(
                                                        MakeMemberAccessExpression("changed", "ToArray"))),
                                                Argument(
                                                    InvocationExpression(
                                                        MakeMemberAccessExpression("reader", GetOrAddReadMethod(new DBusValue { Type = "as" })))))))))));

        private void AddProperties(ref ClassDeclarationSyntax cl, DBusInterface dBusInterface)
        {
            if (dBusInterface.Properties is null || dBusInterface.Properties.Length == 0) return;

            cl = dBusInterface.Properties!.Aggregate(cl, (current, dBusProperty) => dBusProperty.Access switch
            {
                "read" => current.AddMembers(MakeGetMethod(dBusProperty)),
                "write" => current.AddMembers(MakeSetMethod(dBusProperty)),
                "readwrite" => current.AddMembers(MakeGetMethod(dBusProperty), MakeSetMethod(dBusProperty)),
                _ => current
            });

            AddGetAllMethod(ref cl);
            AddReadProperties(ref cl, dBusInterface.Properties);
            AddPropertiesClass(ref cl, dBusInterface);
            AddWatchPropertiesChanged(ref cl);
        }

        private MethodDeclarationSyntax MakeGetMethod(DBusProperty dBusProperty)
        {
            BlockSyntax createMessageBody = MakeCreateMessageBody(MakeLiteralExpression("org.freedesktop.DBus.Properties"), "Get", "ss",
                ExpressionStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("writer", "WriteString"))
                        .AddArgumentListArguments(Argument(IdentifierName("Interface")))),
                ExpressionStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("writer", "WriteString"))
                        .AddArgumentListArguments(Argument(MakeLiteralExpression(dBusProperty.Name!)))));

            ArgumentListSyntax args = ArgumentList()
                .AddArguments(
                    Argument(
                        InvocationExpression(
                            IdentifierName("CreateMessage"))),
                        Argument(
                            MakeMemberAccessExpression("ReaderExtensions", GetOrAddReadMessageMethod(dBusProperty, true))));

                return MethodDeclaration(ParseTypeName(ParseTaskReturnType(dBusProperty)), $"Get{Pascalize(dBusProperty.Name!)}PropertyAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(
                    MakeCallMethodReturnBody(args, createMessageBody));
        }

        private static MethodDeclarationSyntax MakeSetMethod(DBusProperty dBusProperty)
        {
            BlockSyntax createMessageBody = MakeCreateMessageBody(MakeLiteralExpression("org.freedesktop.DBus.Properties"), "Set", "ssv",
                ExpressionStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("writer", "WriteString"))
                        .AddArgumentListArguments(Argument(IdentifierName("Interface")))),
                ExpressionStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("writer", "WriteString"))
                        .AddArgumentListArguments(Argument(MakeLiteralExpression(dBusProperty.Name!)))),
                ExpressionStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("writer", "WriteVariant"))
                        .AddArgumentListArguments(Argument(IdentifierName("value")))));

            ArgumentListSyntax args = ArgumentList(
                SingletonSeparatedList(
                    Argument(
                        InvocationExpression(
                            IdentifierName("CreateMessage")))));

            return MethodDeclaration(ParseTypeName("Task"), $"Set{dBusProperty.Name}PropertyAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("value")).WithType(ParseTypeName(dBusProperty.DotNetType)))
                .WithBody(
                    MakeCallMethodReturnBody(args, createMessageBody));
        }

        private static void AddGetAllMethod(ref ClassDeclarationSyntax cl)
        {
            BlockSyntax createGetAllMessageBody = MakeCreateMessageBody(MakeLiteralExpression("org.freedesktop.DBus.Properties"), "GetAll", "s",
                ExpressionStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("writer", "WriteString"))
                        .AddArgumentListArguments(Argument(IdentifierName("Interface")))));

            ParenthesizedLambdaExpressionSyntax messageValueReaderLambda = ParenthesizedLambdaExpression()
                .AddParameterListParameters(
                    Parameter(Identifier("message")).WithType(ParseTypeName("Message")),
                    Parameter(Identifier("state")).WithType(NullableType(PredefinedType(Token(SyntaxKind.ObjectKeyword)))))
                .WithBody(
                    Block(
                        LocalDeclarationStatement(VariableDeclaration(ParseTypeName("Reader"))
                            .AddVariables(
                                VariableDeclarator("reader")
                                    .WithInitializer(
                                        EqualsValueClause(
                                            InvocationExpression(
                                                MakeMemberAccessExpression("message", "GetBodyReader")))))),
                        ReturnStatement(
                            InvocationExpression(
                                IdentifierName("ReadProperties"))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName("reader"))
                                        .WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))));

            cl = cl.AddMembers(
                MethodDeclaration(ParseTypeName("Task<Properties>"), "GetAllPropertiesAsync")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithBody(
                        Block(
                            ReturnStatement(
                                InvocationExpression(
                                        MakeMemberAccessExpression("_connection", "CallMethodAsync"))
                                    .AddArgumentListArguments(
                                        Argument(InvocationExpression(IdentifierName("CreateGetAllMessage"))),
                                        Argument(messageValueReaderLambda))),
                            LocalFunctionStatement(ParseTypeName("MessageBuffer"), "CreateGetAllMessage")
                                .WithBody(createGetAllMessageBody))));
        }

        private static void AddPropertiesClass(ref ClassDeclarationSyntax cl, DBusInterface dBusInterface)
        {
            ClassDeclarationSyntax propertiesClass = ClassDeclaration("Properties")
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

            propertiesClass = dBusInterface.Properties!.Aggregate(propertiesClass, static (current, property)
                => current.AddMembers(
                    MakeGetSetProperty(ParseTypeName(property.DotNetType), property.Name!, Token(SyntaxKind.PublicKeyword))));

            cl = cl.AddMembers(propertiesClass);
        }

        private void AddReadProperties(ref ClassDeclarationSyntax cl, IEnumerable<DBusProperty> dBusProperties) =>
            cl = cl.AddMembers(
                MethodDeclaration(ParseTypeName("Properties"), "ReadProperties")
                    .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("reader"))
                            .WithType(ParseTypeName("Reader"))
                            .AddModifiers(Token(SyntaxKind.RefKeyword)),
                        Parameter(Identifier("changed"))
                            .WithType(NullableType(ParseTypeName("List<string>")))
                            .WithDefault(
                                EqualsValueClause(
                                    LiteralExpression(SyntaxKind.NullLiteralExpression))))
                    .WithBody(
                        Block(
                            LocalDeclarationStatement(VariableDeclaration(ParseTypeName("Properties"))
                                .AddVariables(
                                    VariableDeclarator("props")
                                        .WithInitializer(
                                            EqualsValueClause(
                                                InvocationExpression(
                                                    ObjectCreationExpression(ParseTypeName("Properties"))))))),
                            LocalDeclarationStatement(VariableDeclaration(ParseTypeName("ArrayEnd"))
                                .AddVariables(
                                    VariableDeclarator("headersEnd")
                                        .WithInitializer(
                                            EqualsValueClause(
                                                InvocationExpression(
                                                        MakeMemberAccessExpression("reader", "ReadArrayStart"))
                                                    .AddArgumentListArguments(
                                                        Argument(MakeMemberAccessExpression("DBusType", "Struct"))))))),
                            WhileStatement(
                                InvocationExpression(
                                        MakeMemberAccessExpression("reader", "HasNext"))
                                    .AddArgumentListArguments(Argument(IdentifierName("headersEnd"))),
                                Block(
                                    SwitchStatement(
                                            InvocationExpression(
                                                MakeMemberAccessExpression("reader", "ReadString")))
                                        .WithSections(
                                            List(
                                                dBusProperties.Select(x => SwitchSection()
                                                    .AddLabels(
                                                        CaseSwitchLabel(
                                                            MakeLiteralExpression(x.Name!)))
                                                    .AddStatements(
                                                        ExpressionStatement(
                                                            InvocationExpression(
                                                                    MakeMemberAccessExpression("reader", "ReadSignature"))
                                                                .AddArgumentListArguments(
                                                                    Argument(MakeLiteralExpression(x.Type!)))),
                                                        ExpressionStatement(
                                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                                MakeMemberAccessExpression("props", x.Name!),
                                                                InvocationExpression(
                                                                    MakeMemberAccessExpression("reader", GetOrAddReadMethod(x))))),
                                                        ExpressionStatement(
                                                            ConditionalAccessExpression(
                                                                IdentifierName("changed"), InvocationExpression(
                                                                    MemberBindingExpression(
                                                                        IdentifierName("Add")))
                                                                    .AddArgumentListArguments(
                                                                        Argument(MakeLiteralExpression(x.Name!))))),
                                                        BreakStatement())))))),
                            ReturnStatement(IdentifierName("props")))));

        private static BlockSyntax MakeCallMethodReturnBody(ArgumentListSyntax args, BlockSyntax createMessageBody) =>
            Block(
                ReturnStatement(
                    InvocationExpression(
                            MakeMemberAccessExpression("_connection", "CallMethodAsync"))
                        .WithArgumentList(args)),
                LocalFunctionStatement(ParseTypeName("MessageBuffer"), "CreateMessage")
                    .WithBody(createMessageBody));

        private static BlockSyntax MakeCreateMessageBody(ExpressionSyntax interfaceExpression, string methodName, string? signature, params StatementSyntax[] statements)
        {
            ArgumentListSyntax args = ArgumentList()
                .AddArguments(
                    Argument(IdentifierName("_destination")),
                    Argument(IdentifierName("_path")),
                    Argument(interfaceExpression),
                    Argument(MakeLiteralExpression(methodName)));

            if (signature is not null)
                args = args.AddArguments(Argument(MakeLiteralExpression(signature)));

            return Block(
                    LocalDeclarationStatement(
                            VariableDeclaration(ParseTypeName("MessageWriter"),
                                SingletonSeparatedList(
                                    VariableDeclarator("writer")
                                        .WithInitializer(EqualsValueClause(
                                            InvocationExpression(
                                                MakeMemberAccessExpression("_connection", "GetMessageWriter"))))))),
                    ExpressionStatement(
                        InvocationExpression(
                                MakeMemberAccessExpression("writer", "WriteMethodCallHeader"))
                            .WithArgumentList(args)))
                .AddStatements(statements)
                .AddStatements(
                    LocalDeclarationStatement(
                        VariableDeclaration(ParseTypeName("MessageBuffer"))
                            .AddVariables(
                                VariableDeclarator("message")
                                    .WithInitializer(
                                        EqualsValueClause(
                                            InvocationExpression(
                                                MakeMemberAccessExpression("writer", "CreateMessage")))))),
                    ExpressionStatement(
                        InvocationExpression(
                            MakeMemberAccessExpression("writer", "Dispose"))),
                    ReturnStatement(
                        IdentifierName("message")));
        }
    }
}
