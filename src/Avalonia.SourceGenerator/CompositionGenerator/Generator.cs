using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Avalonia.SourceGenerator.CompositionGenerator.Extensions;
namespace Avalonia.SourceGenerator.CompositionGenerator
{
    partial class Generator
    {
        private readonly SourceProductionContext _output;
        private readonly GConfig _config;
        private readonly HashSet<string> _objects;
        private readonly HashSet<string> _brushes;
        private readonly Dictionary<string, GManualClass> _manuals;
        public Generator(SourceProductionContext output, GConfig config)
        {
            _output = output;
            _config = config;
            _manuals = _config.ManualClasses.ToDictionary(x => x.Name);
            _objects = new HashSet<string>(_config.ManualClasses.Select(x => x.Name)
                .Concat(_config.Classes.Select(x => x.Name)));
            _brushes = new HashSet<string>(_config.Classes.OfType<GBrush>().Select(x => x.Name)) {"CompositionBrush"};
        }

        
        
        public void Generate()
        {
            foreach (var cl in _config.Classes)
                GenerateClass(cl);
            
            GenerateAnimations();
        }



        string ServerName(string c) => c != null ? ("Server" + c) : "ServerObject";
        string ChangesName(string c) => c != null ? (c + "Changes") : "ChangeSet";
        
        void GenerateClass(GClass cl)
        {
            var list = cl as GList;
            
            var unit = Unit();
            
            var clientNs = NamespaceDeclaration(IdentifierName("Avalonia.Rendering.Composition"));
            var serverNs = NamespaceDeclaration(IdentifierName("Avalonia.Rendering.Composition.Server"));
            var transportNs = NamespaceDeclaration(IdentifierName("Avalonia.Rendering.Composition.Transport"));

            var inherits = cl.Inherits ?? "CompositionObject";
            var abstractModifier = cl.Abstract ? new[] {SyntaxKind.AbstractKeyword} : null;
            
            var client = ClassDeclaration(cl.Name)
                .AddModifiers(abstractModifier)
                .AddModifiers(SyntaxKind.PublicKeyword, SyntaxKind.UnsafeKeyword, SyntaxKind.PartialKeyword)
                .WithBaseType(inherits);
            
            var serverName = ServerName(cl.Name);
            var serverBase = cl.ServerBase ?? ServerName(cl.Inherits);
            if (list != null)
                serverBase = "ServerList<" + ServerName(list.ItemType) + ">";
            
            var server = ClassDeclaration(serverName)
                .AddModifiers(abstractModifier)
                .AddModifiers(SyntaxKind.UnsafeKeyword, SyntaxKind.PartialKeyword)
                .WithBaseType(serverBase);

            string changesName = ChangesName(cl.Name);
            var changesBase = ChangesName(cl.ChangesBase ?? cl.Inherits);

            if (list != null)
                changesBase = "ListChangeSet<" + ServerName(list.ItemType) + ">";

            var changeSetPoolType = "ChangeSetPool<" + changesName + ">";
            var transport = ClassDeclaration(changesName)
                .AddModifiers(SyntaxKind.UnsafeKeyword, SyntaxKind.PartialKeyword)
                .WithBaseType(changesBase)
                .AddMembers(DeclareField(changeSetPoolType, "Pool",
                    EqualsValueClause(
                        ParseExpression($"new {changeSetPoolType}(pool => new {changesName}(pool))")
                    ),
                    SyntaxKind.PublicKeyword,
                    SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword))
                .AddMembers(ParseMemberDeclaration($"public {changesName}(IChangeSetPool pool) : base(pool){{}}"));

            client = client
                .AddMembers(
                    PropertyDeclaration(ParseTypeName("IChangeSetPool"), "ChangeSetPool")
                    .AddModifiers(SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword,
                            SyntaxKind.OverrideKeyword)
                        .WithExpressionBody(
                            ArrowExpressionClause(MemberAccess(changesName, "Pool")))
                        .WithSemicolonToken(Semicolon()))
                .AddMembers(PropertyDeclaration(ParseTypeName(changesName), "Changes")
                    .AddModifiers(SyntaxKind.PrivateKeyword, SyntaxKind.NewKeyword)
                    .WithExpressionBody(ArrowExpressionClause(CastExpression(ParseTypeName(changesName),
                        MemberAccess(BaseExpression(), "Changes"))))
                    .WithSemicolonToken(Semicolon()));
            
            if (!cl.CustomCtor)
            {
                client = client.AddMembers(PropertyDeclaration(ParseTypeName(serverName), "Server")
                    .AddModifiers(SyntaxKind.InternalKeyword, SyntaxKind.NewKeyword)
                    .AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Semicolon())));
                client = client.AddMembers(
                    ConstructorDeclaration(cl.Name)
                        .AddModifiers(SyntaxKind.InternalKeyword)
                        .WithParameterList(ParameterList(SeparatedList(new[]
                        {
                            Parameter(Identifier("compositor")).WithType(ParseTypeName("Compositor")),
                            Parameter(Identifier("server")).WithType(ParseTypeName(serverName)),
                        })))
                        .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(IdentifierName("compositor")),
                                Argument(IdentifierName("server")),
                            })))).WithBody(Block(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName("Server"),
                                    CastExpression(ParseTypeName(serverName), IdentifierName("server")))),
                            ExpressionStatement(InvocationExpression(IdentifierName("InitializeDefaults")))
                        )));
            }

            if (!cl.CustomServerCtor)
            {
                server = server.AddMembers(
                    ConstructorDeclaration(serverName)
                        .AddModifiers(SyntaxKind.InternalKeyword)
                        .WithParameterList(ParameterList(SeparatedList(new[]
                        {
                            Parameter(Identifier("compositor")).WithType(ParseTypeName("ServerCompositor")),
                        })))
                        .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(IdentifierName("compositor")),
                            })))).WithBody(Block()));
            }


            var changesVarName = "c";
            var changesVar = IdentifierName(changesVarName);

            server = server.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "ApplyChangesExtra")
                    .AddParameterListParameters(Parameter(Identifier("c")).WithType(ParseTypeName(changesName)))
                    .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));
            
            transport = transport.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "ResetExtra")
                    .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));

            var applyMethodBody = Block(
                ExpressionStatement(InvocationExpression(MemberAccess(IdentifierName("base"), "ApplyCore"),
                    ArgumentList(SeparatedList(new[] {Argument(IdentifierName("changes"))})))),
                LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(changesVarName)
                            .WithInitializer(EqualsValueClause(CastExpression(ParseTypeName(changesName),
                                IdentifierName("changes"))))))),
                ExpressionStatement(InvocationExpression(IdentifierName("ApplyChangesExtra"))
                    .AddArgumentListArguments(Argument(IdentifierName("c"))))
            );

            var resetBody = Block();
            var startAnimationBody = Block();
            var getPropertyBody = Block();
            var serverGetPropertyBody = Block();

            var defaultsMethodBody = Block();
            
            foreach (var prop in cl.Properties)
            {
                var fieldName = "_" + prop.Name.WithLowerFirst();
                var propType = ParseTypeName(prop.Type);
                var filteredPropertyType = prop.Type.TrimEnd('?');
                var isObject = _objects.Contains(filteredPropertyType);
                var isNullable = prop.Type.EndsWith("?");
                



                client = client
                    .AddMembers(DeclareField(prop.Type, fieldName))
                    .AddMembers(PropertyDeclaration(propType, prop.Name)
                        .AddModifiers(SyntaxKind.PublicKeyword)
                        .AddAccessorListAccessors(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                                Block(ReturnStatement(IdentifierName(fieldName)))),
                            AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                                Block(
                                    ParseStatement("var changed = false;"),
                                    IfStatement(BinaryExpression(SyntaxKind.NotEqualsExpression,
                                            IdentifierName(fieldName),
                                            IdentifierName("value")),
                                        Block(
                                            ParseStatement("On" + prop.Name + "Changing();"),
                                            ParseStatement("changed = true;"),
                                            GeneratePropertySetterAssignment(prop, fieldName, isObject, isNullable))
                                    ),
                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(fieldName), IdentifierName("value"))),
                                    ParseStatement($"if(changed) On" + prop.Name + "Changed();")
                                ))
                        ))
                    .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changed")
                        .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()))
                    .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changing")
                        .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));


                var animatedServer = prop.Animated;
                
                var serverPropertyType = ((isObject ? "Server" : "") + prop.Type);
                if (_manuals.TryGetValue(filteredPropertyType, out var manual) && manual.ServerName != null)
                    serverPropertyType = manual.ServerName + (isNullable ? "?" : "");
                

                transport = transport
                    .AddMembers(DeclareField((animatedServer ? "Animated" : "") + "Change<" + serverPropertyType + ">",
                        prop.Name, SyntaxKind.PublicKeyword));

                if (animatedServer)
                    server = server.AddMembers(
                        DeclareField("AnimatedValueStore<" + serverPropertyType + ">", fieldName),
                        PropertyDeclaration(ParseTypeName(serverPropertyType), prop.Name)
                            .AddModifiers(SyntaxKind.PublicKeyword)
                            .WithExpressionBody(ArrowExpressionClause(
                                InvocationExpression(MemberAccess(fieldName, "GetAnimated"),
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName("Compositor")))))))
                            .WithSemicolonToken(Semicolon())
                    );
                else
                {
                    server = server
                        .AddMembers(DeclareField(serverPropertyType, fieldName))
                        .AddMembers(PropertyDeclaration(ParseTypeName(serverPropertyType), prop.Name)
                            .AddModifiers(SyntaxKind.PublicKeyword)
                            .AddAccessorListAccessors(
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                                    Block(ReturnStatement(IdentifierName(fieldName)))),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                                    Block(
                                        ParseStatement("var changed = false;"),
                                        IfStatement(BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                IdentifierName(fieldName),
                                                IdentifierName("value")),
                                            Block(
                                                ParseStatement("On" + prop.Name + "Changing();"),
                                                ParseStatement($"changed = true;"))
                                        ),
                                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(fieldName), IdentifierName("value"))),
                                        ParseStatement($"if(changed) On" + prop.Name + "Changed();")
                                    ))
                            ))
                        .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changed")
                            .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()))
                        .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changing")
                            .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));
                }

                if (animatedServer)
                    applyMethodBody = applyMethodBody.AddStatements(
                        IfStatement(MemberAccess(changesVar, prop.Name, "IsValue"),
                            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(fieldName), MemberAccess(changesVar, prop.Name, "Value")))),
                        IfStatement(MemberAccess(changesVar, prop.Name, "IsAnimation"),
                            ExpressionStatement(
                                InvocationExpression(MemberAccess(fieldName, "SetAnimation"),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(changesVar),
                                        Argument(MemberAccess(changesVar, prop.Name, "Animation"))
                                    })))))
                    );
                else
                    applyMethodBody = applyMethodBody.AddStatements(
                        IfStatement(MemberAccess(changesVar, prop.Name, "IsSet"),
                            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(prop.Name), MemberAccess(changesVar, prop.Name, "Value"))))

                    );
                

                resetBody = resetBody.AddStatements(
                    ExpressionStatement(InvocationExpression(MemberAccess(prop.Name, "Reset"))));

                if (animatedServer)
                    startAnimationBody = ApplyStartAnimation(startAnimationBody, prop, fieldName);

                getPropertyBody = ApplyGetProperty(getPropertyBody, prop);
                serverGetPropertyBody = ApplyGetProperty(getPropertyBody, prop);
                
                if (prop.DefaultValue != null)
                {
                    defaultsMethodBody = defaultsMethodBody.AddStatements(
                        ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(prop.Name), ParseExpression(prop.DefaultValue))));
                }
            }

            if (cl is GBrush brush && !cl.Abstract)
            {
                var brushName = brush.Name.StripPrefix("Composition");
                /*
                server = server.AddMembers(
                    MethodDeclaration(ParseTypeName("ICbBrush"), "CreateBackendBrush")
                        .AddModifiers(SyntaxKind.ProtectedKeyword, SyntaxKind.OverrideKeyword)
                        .WithExpressionBody(ArrowExpressionClause(
                            InvocationExpression(MemberAccess("Compositor", "Backend", "Create" + brushName))
                        )).WithSemicolonToken(Semicolon())
                );
                if (!brush.CustomUpdate)
                    server = server.AddMembers(
                        MethodDeclaration(ParseTypeName("void"), "UpdateBackendBrush")
                            .AddModifiers(SyntaxKind.ProtectedKeyword, SyntaxKind.OverrideKeyword)
                            .AddParameterListParameters(Parameter(Identifier("brush"))
                                .WithType(ParseTypeName("ICbBrush")))
                            .AddBodyStatements(
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccess(
                                            ParenthesizedExpression(
                                            CastExpression(ParseTypeName("ICb" + brushName), IdentifierName("brush"))), "Update"),
                                        ArgumentList(SeparatedList(cl.Properties.Select(x =>
                                        {
                                            if(x.Type.TrimEnd('?') == "ICompositionSurface")
                                                return Argument(
                                                    ConditionalAccessExpression(IdentifierName(x.Name),
                                                        MemberBindingExpression(IdentifierName("BackendSurface")))
                                                );
                                            if (_brushes.Contains(x.Type))
                                                return Argument(
                                                    ConditionalAccessExpression(IdentifierName(x.Name),
                                                        MemberBindingExpression(IdentifierName("Brush")))
                                                );
                                            return Argument(IdentifierName(x.Name));
                                        }))))
                                )));

*/
            }
            
            server = server.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "ApplyCore")
                    .AddModifiers(SyntaxKind.ProtectedKeyword, SyntaxKind.OverrideKeyword)
                    .AddParameterListParameters(
                        Parameter(Identifier("changes")).WithType(ParseTypeName("ChangeSet")))
                    .WithBody(applyMethodBody));

            client = client.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "InitializeDefaults").WithBody(defaultsMethodBody));

            transport = transport.AddMembers(MethodDeclaration(ParseTypeName("void"), "Reset")
                .AddModifiers(SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword)
                .WithBody(resetBody.AddStatements(
                    ExpressionStatement(InvocationExpression(IdentifierName("ResetExtra"))),
                    ExpressionStatement(InvocationExpression(MemberAccess("base", "Reset"))))));

            if (list != null)
                client = AppendListProxy(list, client);

            if (startAnimationBody.Statements.Count != 0)
                client = WithStartAnimation(client, startAnimationBody);

            client = WithGetProperty(client, getPropertyBody, false);
            server = WithGetProperty(server, serverGetPropertyBody, true);
            
            if(cl.Implements.Count > 0)
                foreach (var impl in cl.Implements)
                {
                    client = client.WithBaseList(client.BaseList.AddTypes(SimpleBaseType(ParseTypeName(impl.Name))));
                    if (impl.ServerName != null)
                        server = server.WithBaseList(
                            server.BaseList.AddTypes(SimpleBaseType(ParseTypeName(impl.ServerName))));

                    client = client.AddMembers(
                        ParseMemberDeclaration($"{impl.ServerName} {impl.Name}.Server => Server;"));
                }


            SaveTo(unit.AddMembers(clientNs.AddMembers(client)),
                cl.Name + ".generated.cs");
            SaveTo(unit.AddMembers(serverNs.AddMembers(server)),
                "Server", "Server" + cl.Name + ".generated.cs");
            SaveTo(unit.AddMembers(transportNs.AddMembers(transport)),
                "Transport",  cl.Name + "Changes.generated.cs");
        }

        StatementSyntax GeneratePropertySetterAssignment(GProperty prop, string fieldName, bool isObject, bool isNullable)
        {
            var normalChangesAssignment = (StatementSyntax)ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                MemberAccess((ExpressionSyntax) IdentifierName("Changes"), prop.Name,
                    "Value"),
                isObject
                    ?  
                    ConditionalMemberAccess(IdentifierName("value"), "Server", isNullable)
                    : IdentifierName("value")));
            if (!prop.Animated)
                return normalChangesAssignment;

            var code = $@"
{{
    if(animation is CompositionAnimation a)
        Changes.{prop.Name}.Animation = a.CreateInstance(this.Server, value);
    else
    {{
        var saved = Changes.{prop.Name};
        if(!StartAnimationGroup(animation, ""{prop.Name}"", value))
            Changes.{prop.Name}.Value = value;
    }}
}}

";
            
            return IfStatement(
                ParseExpression(
                    $"ImplicitAnimations != null && ImplicitAnimations.TryGetValue(\"{prop.Name}\", out var animation) == true"),
                ParseStatement(code),
                ElseClause(normalChangesAssignment)
            );
        }
        
        BlockSyntax ApplyStartAnimation(BlockSyntax body, GProperty prop, string fieldName)
        {
            var code = $@"
if (propertyName == ""{prop.Name}"")
{{
var current = {fieldName};
var server = animation.CreateInstance(this.Server, finalValue);
Changes.{prop.Name}.Animation = server;
return;
}}
";
            return body.AddStatements(ParseStatement(code));
        }

        private static HashSet<string> VariantPropertyTypes = new HashSet<string>
        {
            "bool",
            "float",
            "Vector2",
            "Vector3",
            "Vector4",
            "Matrix3x2",
            "Matrix4x4",
            "Quaternion",
            "CompositionColor"
        };
        
        BlockSyntax ApplyGetProperty(BlockSyntax body, GProperty prop)
        {
            if (VariantPropertyTypes.Contains(prop.Type))
                return body.AddStatements(
                    ParseStatement($"if(name == \"{prop.Name}\")\n return {prop.Name};\n")
                );

            return body;
        }

        ClassDeclarationSyntax WithGetProperty(ClassDeclarationSyntax cl, BlockSyntax body, bool server)
        {
            if (body.Statements.Count == 0)
                return cl;
            body = body.AddStatements(
                ParseStatement("return base.GetPropertyForAnimation(name);"));
            var method = ((MethodDeclarationSyntax) ParseMemberDeclaration(
                    $"{(server ? "public" : "internal")} override Avalonia.Rendering.Composition.Expressions.ExpressionVariant GetPropertyForAnimation(string name){{}}"))
                .WithBody(body);

            return cl.AddMembers(method);
        }

        ClassDeclarationSyntax WithStartAnimation(ClassDeclarationSyntax cl, BlockSyntax body)
        {
            body = body.AddStatements(
                ExpressionStatement(InvocationExpression(MemberAccess("base", "StartAnimation"),
                    ArgumentList(SeparatedList(new[]
                    {
                        Argument(IdentifierName("propertyName")),
                        Argument(IdentifierName("animation")),
                        Argument(IdentifierName("finalValue")),
                    }))))
            );
            return cl.AddMembers(
                ((MethodDeclarationSyntax) ParseMemberDeclaration(
                    "internal override void StartAnimation(string propertyName, CompositionAnimation animation, Avalonia.Rendering.Composition.Expressions.ExpressionVariant? finalValue){}"))
                .WithBody(body));


        }
        
    }
}