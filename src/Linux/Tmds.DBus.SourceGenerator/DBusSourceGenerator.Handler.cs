using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Tmds.DBus.SourceGenerator
{
    public partial class DBusSourceGenerator
    {
        private ClassDeclarationSyntax GenerateHandler(DBusInterface dBusInterface, bool hasIntrospection)
        {
            ClassDeclarationSyntax cl = ClassDeclaration(Pascalize(dBusInterface.Name!))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.AbstractKeyword))
                .AddBaseListTypes(
                    SimpleBaseType(IdentifierName("IMethodHandler")))
                .AddMembers(
                    FieldDeclaration(
                            VariableDeclaration(
                                    NullableType(
                                        IdentifierName("SynchronizationContext")))
                                .AddVariables(
                                    VariableDeclarator("_synchronizationContext")))
                        .AddModifiers(Token(SyntaxKind.PrivateKeyword)),
                    ConstructorDeclaration(Pascalize(dBusInterface.Name!))
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            Parameter(Identifier("emitOnCapturedContext"))
                                .WithType(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                                .WithDefault(
                                    EqualsValueClause(
                                        LiteralExpression(SyntaxKind.TrueLiteralExpression))))
                        .WithBody(
                            Block(
                                IfStatement(
                                    IdentifierName("emitOnCapturedContext"),
                                    ExpressionStatement(
                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("_synchronizationContext"),
                                            MakeMemberAccessExpression("SynchronizationContext", "Current")))))),
                    MakeGetOnlyProperty(IdentifierName("Connection"), "Connection", Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.AbstractKeyword)),
                    MakeGetOnlyProperty(PredefinedType(Token(SyntaxKind.StringKeyword)), "Path", Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AbstractKeyword)));

            MethodDeclarationSyntax handleMethod = MethodDeclaration(IdentifierName("ValueTask"), "HandleMethodAsync")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("context"))
                        .WithType(IdentifierName("MethodContext")));

            SwitchStatementSyntax switchStatement = SwitchStatement(MakeMemberAccessExpression("context", "Request", "InterfaceAsString"));

            AddHandlerProperties(ref cl, ref switchStatement, dBusInterface);
            AddHandlerMethods(ref cl, ref switchStatement, dBusInterface);
            AddHandlerSignals(ref cl, dBusInterface);
            
            if(hasIntrospection)
                AddHandlerIntrospect(ref cl, ref switchStatement, dBusInterface);

            cl = cl.AddMembers(
                MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), "RunMethodHandlerSynchronously")
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("message"))
                            .WithType(IdentifierName("Message")))
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            LiteralExpression(SyntaxKind.TrueLiteralExpression)))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                handleMethod.WithBody(
                    Block(switchStatement)));

            return cl;
        }

        private void AddHandlerMethods(ref ClassDeclarationSyntax cl, ref SwitchStatementSyntax sw, DBusInterface dBusInterface)
        {
            if (dBusInterface.Methods is null)
                return;

            SyntaxList<SwitchSectionSyntax> switchSections = List<SwitchSectionSyntax>();

            foreach (DBusMethod dBusMethod in dBusInterface.Methods)
            {
                DBusArgument[]? inArgs = dBusMethod.Arguments?.Where(static m => m.Direction is null or "in").ToArray();
                DBusArgument[]? outArgs = dBusMethod.Arguments?.Where(static m => m.Direction == "out").ToArray();

                SwitchSectionSyntax switchSection = SwitchSection()
                    .AddLabels(
                        CasePatternSwitchLabel(
                            RecursivePattern()
                                .WithPositionalPatternClause(
                                    PositionalPatternClause()
                                        .AddSubpatterns(
                                            Subpattern(
                                                ConstantPattern(
                                                    MakeLiteralExpression(dBusMethod.Name!))),
                                            Subpattern(
                                                inArgs?.Length > 0
                                                ? ConstantPattern(
                                                    MakeLiteralExpression(
                                                        ParseSignature(inArgs)!))
                                                : BinaryPattern(SyntaxKind.OrPattern,
                                                    ConstantPattern(
                                                        MakeLiteralExpression(string.Empty)),
                                                    ConstantPattern(
                                                        LiteralExpression(SyntaxKind.NullLiteralExpression)))))),
                            Token(SyntaxKind.ColonToken)));

                BlockSyntax switchSectionBlock = Block();

                string abstractMethodName = $"On{Pascalize(dBusMethod.Name!)}Async";

                MethodDeclarationSyntax abstractMethod = MethodDeclaration(
                    ParseValueTaskReturnType(outArgs, AccessMode.Write), abstractMethodName);

                if (inArgs?.Length > 0)
                    abstractMethod = abstractMethod.WithParameterList(
                        ParseParameterList(inArgs, AccessMode.Read));

                abstractMethod = abstractMethod
                    .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.AbstractKeyword))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

                cl = cl.AddMembers(abstractMethod);

                if (inArgs?.Length > 0)
                {
                    BlockSyntax readParametersMethodBlock = Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(IdentifierName("Reader"))
                                .AddVariables(
                                    VariableDeclarator("reader")
                                        .WithInitializer(
                                            EqualsValueClause(
                                                InvocationExpression(MakeMemberAccessExpression("context", "Request", "GetBodyReader")))))));

                    StatementSyntax[] argFields = new StatementSyntax[inArgs.Length];

                    for (int i = 0; i < inArgs.Length; i++)
                    {
                        string identifier = inArgs[i].Name is not null ? SanitizeIdentifier(Camelize(inArgs[i].Name!)) : $"arg{i}";
                        readParametersMethodBlock = readParametersMethodBlock.AddStatements(
                            ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(identifier), InvocationExpression(
                                    MakeMemberAccessExpression("reader", GetOrAddReadMethod(inArgs[i]))))));
                        argFields[i] = LocalDeclarationStatement(
                            VariableDeclaration(GetDotnetType(inArgs[i], AccessMode.Read))
                                .AddVariables(
                                    VariableDeclarator(identifier)));
                    }

                    switchSectionBlock = switchSectionBlock.AddStatements(argFields);
                    switchSectionBlock = switchSectionBlock.AddStatements(
                        ExpressionStatement(
                            InvocationExpression(
                                IdentifierName("ReadParameters"))),
                        LocalFunctionStatement(
                                PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier("ReadParameters"))
                            .WithBody(readParametersMethodBlock));
                }

                if (outArgs?.Length > 0)
                {
                    switchSectionBlock = switchSectionBlock.AddStatements(
                        LocalDeclarationStatement(
                            VariableDeclaration(ParseReturnType(outArgs, AccessMode.Write)!)
                                .AddVariables(
                                    VariableDeclarator("ret"))));
                }

                ExpressionSyntax callAbstractMethod = AwaitExpression(
                    InvocationExpression(
                            IdentifierName(abstractMethodName))
                        .AddArgumentListArguments(
                            inArgs?.Select(static (argument, i) =>
                                    Argument(
                                        IdentifierName(argument.Name is not null
                                            ? SanitizeIdentifier(Camelize(argument.Name))
                                            : $"arg{i}")))
                                .ToArray() ?? []));

                switchSectionBlock = switchSectionBlock.AddStatements(
                        IfStatement(
                        IsPatternExpression(
                            IdentifierName("_synchronizationContext"), UnaryPattern(ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                        Block(
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                        ParseTaskCompletionSourceType(outArgs, AccessMode.Write))
                                    .AddVariables(
                                        VariableDeclarator("tsc")
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    ImplicitObjectCreationExpression())))),
                            ExpressionStatement(
                                InvocationExpression(
                                    MakeMemberAccessExpression("_synchronizationContext", "Post"))
                                    .AddArgumentListArguments(
                                        Argument(
                                            SimpleLambdaExpression(
                                                Parameter(
                                                    Identifier("_")))
                                                .WithAsyncKeyword(Token(SyntaxKind.AsyncKeyword))
                                                .WithBlock(
                                                    Block(
                                                        TryStatement()
                                                            .AddBlockStatements(
                                                                outArgs?.Length > 0
                                                                    ? LocalDeclarationStatement(
                                                                    VariableDeclaration(
                                                                            ParseReturnType(outArgs, AccessMode.Write)!)
                                                                        .AddVariables(
                                                                            VariableDeclarator("ret1")
                                                                                .WithInitializer(
                                                                                    EqualsValueClause(callAbstractMethod))))
                                                                    : ExpressionStatement(callAbstractMethod),
                                                                ExpressionStatement(
                                                                    InvocationExpression(
                                                                            MakeMemberAccessExpression("tsc", "SetResult"))
                                                                        .AddArgumentListArguments(
                                                                            Argument(
                                                                                outArgs?.Length > 0
                                                                                    ? IdentifierName("ret1")
                                                                                    : LiteralExpression(SyntaxKind.TrueLiteralExpression)))))
                                                            .AddCatches(
                                                                CatchClause()
                                                                    .WithDeclaration(
                                                                        CatchDeclaration(IdentifierName("Exception"))
                                                                        .WithIdentifier(Identifier("e")))
                                                                    .WithBlock(
                                                                        Block(
                                                                            ExpressionStatement(
                                                                                InvocationExpression(
                                                                                        MakeMemberAccessExpression("tsc", "SetException"))
                                                                                    .AddArgumentListArguments(
                                                                                        Argument(IdentifierName("e")))))))))),
                                        Argument(
                                            LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                            ExpressionStatement(
                                outArgs?.Length > 0
                                    ? MakeAssignmentExpression(
                                        IdentifierName("ret"), AwaitExpression(
                                            MakeMemberAccessExpression("tsc", "Task")))
                                    : AwaitExpression(
                                        MakeMemberAccessExpression("tsc", "Task")))),
                        ElseClause(
                            Block(
                            ExpressionStatement(
                                outArgs?.Length > 0
                                    ? MakeAssignmentExpression(
                                        IdentifierName("ret"), callAbstractMethod)
                                    : callAbstractMethod)))));

                    BlockSyntax replyMethodBlock = Block(
                        LocalDeclarationStatement(
                                VariableDeclaration(IdentifierName("MessageWriter"))
                                    .AddVariables(
                                        VariableDeclarator("writer")
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    InvocationExpression(
                                                            MakeMemberAccessExpression("context", "CreateReplyWriter"))
                                                        .AddArgumentListArguments(
                                                            Argument(
                                                                outArgs?.Length > 0
                                                                    ? MakeLiteralExpression(
                                                                        ParseSignature(outArgs)!)
                                                                    : LiteralExpression(SyntaxKind.NullLiteralExpression))))))));

                    if (outArgs?.Length == 1)
                    {
                        replyMethodBlock = replyMethodBlock.AddStatements(
                            ExpressionStatement(
                                InvocationExpression(
                                        MakeMemberAccessExpression("writer", GetOrAddWriteMethod(outArgs[0])))
                                    .AddArgumentListArguments(
                                        Argument(
                                            IdentifierName("ret")))));
                    }
                    else if (outArgs?.Length > 1)
                    {
                        for (int i = 0; i < outArgs.Length; i++)
                        {
                            replyMethodBlock = replyMethodBlock.AddStatements(
                                ExpressionStatement(
                                    InvocationExpression(
                                            MakeMemberAccessExpression("writer", GetOrAddWriteMethod(outArgs[i])))
                                        .AddArgumentListArguments(
                                            Argument(
                                                MakeMemberAccessExpression("ret", outArgs[i].Name is not null
                                                    ? SanitizeIdentifier(Pascalize(outArgs[i].Name!))
                                                    : $"Item{i + 1}")))));
                        }
                    }

                    replyMethodBlock = replyMethodBlock.AddStatements(
                        ExpressionStatement(
                            InvocationExpression(
                                    MakeMemberAccessExpression("context", "Reply"))
                                .AddArgumentListArguments(
                                    Argument(
                                        InvocationExpression(
                                            MakeMemberAccessExpression("writer", "CreateMessage"))))),
                        ExpressionStatement(
                            InvocationExpression(
                                MakeMemberAccessExpression("writer", "Dispose"))));

                    switchSectionBlock = switchSectionBlock.AddStatements(
                        IfStatement(
                            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, MakeMemberAccessExpression("context", "NoReplyExpected")),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName("Reply")))),
                        LocalFunctionStatement(
                                PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier("Reply"))
                            .WithBody(replyMethodBlock));

                switchSectionBlock = switchSectionBlock.AddStatements(BreakStatement());

                switchSections = switchSections.Add(
                    switchSection.AddStatements(switchSectionBlock));
            }

            sw = sw.AddSections(
                SwitchSection()
                    .AddLabels(
                        CaseSwitchLabel(
                            MakeLiteralExpression(dBusInterface.Name!)))
                    .AddStatements(
                        SwitchStatement(
                                TupleExpression()
                                    .AddArguments(
                                        Argument(MakeMemberAccessExpression("context", "Request", "MemberAsString")),
                                        Argument(MakeMemberAccessExpression("context", "Request", "SignatureAsString"))))
                            .WithSections(switchSections),
                        BreakStatement()));
        }

        private void AddHandlerProperties(ref ClassDeclarationSyntax cl, ref SwitchStatementSyntax sw, DBusInterface dBusInterface)
        {
            if (dBusInterface.Properties is null)
                return;

            cl = dBusInterface.Properties!.Aggregate(cl, static (current, property) =>
                current.AddMembers(
                    MakeGetSetProperty(
                        GetDotnetType(property, AccessMode.Write, true),
                        Pascalize(property.Name!), Token(SyntaxKind.PublicKeyword))));

            sw = sw.AddSections(
                SwitchSection()
                    .AddLabels(
                        CaseSwitchLabel(
                            MakeLiteralExpression("org.freedesktop.DBus.Properties")))
                    .AddStatements(
                        SwitchStatement(
                                TupleExpression()
                                    .AddArguments(
                                        Argument(MakeMemberAccessExpression("context", "Request", "MemberAsString")),
                                        Argument(MakeMemberAccessExpression("context", "Request", "SignatureAsString"))))
                            .AddSections(
                                SwitchSection()
                                    .AddLabels(
                                        CaseSwitchLabel(
                                            TupleExpression()
                                                .AddArguments(
                                                    Argument(MakeLiteralExpression("Get")),
                                                    Argument(MakeLiteralExpression("ss")))))
                                    .AddStatements(
                                        Block(
                                            ExpressionStatement(
                                                InvocationExpression(IdentifierName("Reply"))),
                                            LocalFunctionStatement(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier("Reply"))
                                                .AddBodyStatements(
                                                    LocalDeclarationStatement(
                                                        VariableDeclaration(IdentifierName("Reader"))
                                                            .AddVariables(
                                                                VariableDeclarator("reader")
                                                                    .WithInitializer(
                                                                        EqualsValueClause(
                                                                            InvocationExpression(
                                                                                MakeMemberAccessExpression("context", "Request", "GetBodyReader")))))),
                                                    ExpressionStatement(
                                                        InvocationExpression(
                                                            MakeMemberAccessExpression("reader", "ReadString"))),
                                                    LocalDeclarationStatement(
                                                        VariableDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)))
                                                            .AddVariables(
                                                                VariableDeclarator("member")
                                                                    .WithInitializer(
                                                                        EqualsValueClause(
                                                                            InvocationExpression(
                                                                                MakeMemberAccessExpression("reader", "ReadString")))))),
                                                    SwitchStatement(IdentifierName("member"))
                                                        .WithSections(
                                                            List(
                                                                dBusInterface.Properties.Select(property =>
                                                                    SwitchSection()
                                                                        .AddLabels(
                                                                            CaseSwitchLabel(
                                                                                MakeLiteralExpression(property.Name!)))
                                                                        .AddStatements(
                                                                            Block(
                                                                                LocalDeclarationStatement(
                                                                                    VariableDeclaration(IdentifierName("MessageWriter"))
                                                                                        .AddVariables(
                                                                                            VariableDeclarator("writer")
                                                                                                .WithInitializer(
                                                                                                    EqualsValueClause(
                                                                                                        InvocationExpression(
                                                                                                                MakeMemberAccessExpression("context", "CreateReplyWriter"))
                                                                                                            .AddArgumentListArguments(
                                                                                                                Argument(
                                                                                                                    MakeLiteralExpression("v"))))))),
                                                                                ExpressionStatement(
                                                                                    InvocationExpression(
                                                                                            MakeMemberAccessExpression("writer", "WriteSignature"))
                                                                                        .AddArgumentListArguments(
                                                                                            Argument(
                                                                                                MakeLiteralExpression(property.Type!)))),
                                                                                ExpressionStatement(
                                                                                    InvocationExpression(
                                                                                            MakeMemberAccessExpression("writer", GetOrAddWriteMethod(property)))
                                                                                        .AddArgumentListArguments(
                                                                                            Argument(
                                                                                                IdentifierName(
                                                                                                    Pascalize(property.Name!))))),
                                                                                ExpressionStatement(
                                                                                    InvocationExpression(
                                                                                            MakeMemberAccessExpression("context", "Reply"))
                                                                                        .AddArgumentListArguments(
                                                                                            Argument(
                                                                                                InvocationExpression(
                                                                                                    MakeMemberAccessExpression("writer", "CreateMessage"))))),
                                                                                ExpressionStatement(
                                                                                    InvocationExpression(
                                                                                        MakeMemberAccessExpression("writer", "Dispose"))),
                                                                                BreakStatement())))))),
                                            BreakStatement())),
                                SwitchSection()
                                    .AddLabels(
                                        CaseSwitchLabel(
                                            TupleExpression()
                                                .AddArguments(
                                                    Argument(MakeLiteralExpression("GetAll")),
                                                    Argument(MakeLiteralExpression("s")))))
                                    .AddStatements(
                                        Block(
                                            ExpressionStatement(
                                                InvocationExpression(IdentifierName("Reply"))),
                                            LocalFunctionStatement(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier("Reply"))
                                                .AddBodyStatements(
                                                    LocalDeclarationStatement(
                                                        VariableDeclaration(IdentifierName("MessageWriter"))
                                                            .AddVariables(
                                                                VariableDeclarator("writer")
                                                                    .WithInitializer(
                                                                        EqualsValueClause(
                                                                            InvocationExpression(
                                                                                    MakeMemberAccessExpression("context", "CreateReplyWriter"))
                                                                                .AddArgumentListArguments(
                                                                                    Argument(MakeLiteralExpression("a{sv}"))))))),
                                                    LocalDeclarationStatement(
                                                        VariableDeclaration(IdentifierName("ArrayStart"))
                                                            .AddVariables(
                                                                VariableDeclarator("dictStart")
                                                                    .WithInitializer(
                                                                        EqualsValueClause(
                                                                            InvocationExpression(
                                                                                MakeMemberAccessExpression("writer", "WriteDictionaryStart")))))))
                                            .AddBodyStatements(
                                                dBusInterface.Properties.SelectMany(property =>
                                                    new StatementSyntax[]
                                                    {
                                                        ExpressionStatement(
                                                            InvocationExpression(
                                                                MakeMemberAccessExpression("writer", "WriteDictionaryEntryStart"))),
                                                        ExpressionStatement(
                                                            InvocationExpression(
                                                                MakeMemberAccessExpression("writer", "WriteString"))
                                                                .AddArgumentListArguments(
                                                                    Argument(
                                                                        MakeLiteralExpression(property.Name!)))),
                                                        ExpressionStatement(
                                                            InvocationExpression(
                                                                MakeMemberAccessExpression("writer", "WriteSignature"))
                                                                .AddArgumentListArguments(
                                                                    Argument(
                                                                        MakeLiteralExpression(property.Type!)))),
                                                        ExpressionStatement(
                                                            InvocationExpression(
                                                                MakeMemberAccessExpression("writer", GetOrAddWriteMethod(property)))
                                                                .AddArgumentListArguments(
                                                                    Argument(
                                                                        IdentifierName(
                                                                            Pascalize(property.Name!)))))
                                                    }).ToArray())
                                            .AddBodyStatements(
                                                ExpressionStatement(
                                                    InvocationExpression(
                                                            MakeMemberAccessExpression("writer", "WriteDictionaryEnd"))
                                                        .AddArgumentListArguments(
                                                            Argument(
                                                                IdentifierName("dictStart")))),
                                                ExpressionStatement(
                                                    InvocationExpression(
                                                            MakeMemberAccessExpression("context", "Reply"))
                                                        .AddArgumentListArguments(
                                                            Argument(
                                                                InvocationExpression(MakeMemberAccessExpression("writer", "CreateMessage"))))))),
                                        BreakStatement())),
                        BreakStatement()));
        }

        private void AddHandlerIntrospect(ref ClassDeclarationSyntax cl, ref SwitchStatementSyntax sw, DBusInterface dBusInterface)
        {
            
            XmlSerializer xmlSerializer = new(typeof(DBusInterface));
            using StringWriter stringWriter = new();
            using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true });
            xmlSerializer.Serialize(xmlWriter, dBusInterface);
            string introspect = stringWriter.ToString();

            cl = cl.AddMembers(
                FieldDeclaration(
                    VariableDeclaration(GenericName("ReadOnlyMemory")
                            .AddTypeArgumentListArguments(
                                PredefinedType(Token(SyntaxKind.ByteKeyword))))
                        .AddVariables(
                            VariableDeclarator("_introspectXml")
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LiteralExpression(SyntaxKind.Utf8StringLiteralExpression, Utf8Literal(introspect)), IdentifierName("ToArray")))))))
                    .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)));

            sw = sw.AddSections(
                SwitchSection()
                    .AddLabels(
                        CaseSwitchLabel(MakeLiteralExpression("org.freedesktop.DBus.Introspectable")))
                    .AddStatements(
                        SwitchStatement(
                            TupleExpression()
                                .AddArguments(
                                    Argument(MakeMemberAccessExpression("context", "Request", "MemberAsString")),
                                    Argument(MakeMemberAccessExpression("context", "Request", "SignatureAsString"))))
                            .AddSections(
                                SwitchSection()
                                    .AddLabels(
                                        CasePatternSwitchLabel(
                                            RecursivePattern()
                                                .WithPositionalPatternClause(
                                                    PositionalPatternClause()
                                                        .AddSubpatterns(
                                                            Subpattern(
                                                                ConstantPattern(
                                                                    MakeLiteralExpression("Introspect"))),
                                                            Subpattern(
                                                                BinaryPattern(SyntaxKind.OrPattern,
                                                                    ConstantPattern(
                                                                        MakeLiteralExpression(string.Empty)),
                                                                    ConstantPattern(
                                                                        LiteralExpression(SyntaxKind.NullLiteralExpression)))))),
                                            Token(SyntaxKind.ColonToken)))
                                    .AddStatements(
                                        Block(
                                            ExpressionStatement(
                                                InvocationExpression(
                                                    MakeMemberAccessExpression("context", "ReplyIntrospectXml"))
                                                    .AddArgumentListArguments(
                                                        Argument(
                                                            ImplicitArrayCreationExpression(
                                                                InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                                                                    .AddExpressions(IdentifierName("_introspectXml")))))),
                                            BreakStatement()))),
                        BreakStatement()));
        }

        private void AddHandlerSignals(ref ClassDeclarationSyntax cl, DBusInterface dBusInterface)
        {
            if (dBusInterface.Signals is null)
                return;

            foreach (DBusSignal signal in dBusInterface.Signals)
            {
                MethodDeclarationSyntax method = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), $"Emit{Pascalize(signal.Name!)}")
                    .AddModifiers(Token(SyntaxKind.ProtectedKeyword));

                if (signal.Arguments?.Length > 0)
                {
                    method = method.WithParameterList(
                        ParameterList(
                            SeparatedList(
                                signal.Arguments.Select(
                                    static (argument, i) => Parameter(
                                        Identifier(argument.Name is not null ? SanitizeIdentifier(Camelize(argument.Name)) : $"arg{i}"))
                                        .WithType(
                                            GetDotnetType(argument, AccessMode.Write, true))))));
                }

                BlockSyntax body = Block();

                body = body.AddStatements(
                    LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("MessageWriter"),
                            SingletonSeparatedList(
                                VariableDeclarator("writer")
                                    .WithInitializer(EqualsValueClause(
                                        InvocationExpression(
                                            MakeMemberAccessExpression("Connection", "GetMessageWriter"))))))));

                ArgumentListSyntax args = ArgumentList()
                    .AddArguments(
                        Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        Argument(IdentifierName("Path")),
                        Argument(MakeLiteralExpression(dBusInterface.Name!)),
                        Argument(MakeLiteralExpression(signal.Name!)));

                if (signal.Arguments?.Length > 0)
                {
                    args = args.AddArguments(
                        Argument(MakeLiteralExpression(ParseSignature(signal.Arguments)!)));
                }

                body = body.AddStatements(
                    ExpressionStatement(
                        InvocationExpression(
                                MakeMemberAccessExpression("writer", "WriteSignalHeader"))
                            .WithArgumentList(args)));

                if (signal.Arguments?.Length > 0)
                {
                    for (int i = 0; i < signal.Arguments.Length; i++)
                    {
                        body = body.AddStatements(
                            ExpressionStatement(
                                InvocationExpression(
                                        MakeMemberAccessExpression("writer", GetOrAddWriteMethod(signal.Arguments[i])))
                                    .AddArgumentListArguments(
                                        Argument(
                                            IdentifierName(signal.Arguments[i].Name is not null
                                                ? Camelize(signal.Arguments[i].Name!)
                                                : $"arg{i}")))));
                    }
                }

                body = body.AddStatements(
                    ExpressionStatement(
                        InvocationExpression(
                                MakeMemberAccessExpression("Connection", "TrySendMessage"))
                            .AddArgumentListArguments(
                                Argument(
                                    InvocationExpression(
                                        MakeMemberAccessExpression("writer", "CreateMessage"))))),
                    ExpressionStatement(
                        InvocationExpression(
                            MakeMemberAccessExpression("writer", "Dispose"))));

                cl = cl.AddMembers(method.WithBody(body));
            }
        }
    }
}
