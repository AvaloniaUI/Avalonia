using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Avalonia.SourceGenerator.CompositionGenerator.Extensions;
namespace Avalonia.SourceGenerator.CompositionGenerator
{
    public partial class Generator
    {
        private readonly ICompositionGeneratorSink _output;
        private readonly GConfig _config;
        private readonly HashSet<string> _objects;
        private readonly HashSet<string> _brushes;
        private readonly Dictionary<string, GManualClass> _manuals;
        public Generator(ICompositionGeneratorSink output, GConfig config)
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

        static string ServerName(string? c) => c != null ? ("Server" + c) : "ServerObject";
        static string ChangesName(string? c) => c != null ? (c + "Changes") : "ChangeSet";
        static string ChangedFieldsTypeName(GClass c) => c.Name + "ChangedFields";
        static string ChangedFieldsFieldName(GClass c) =>  "_changedFieldsOf" + c.Name;
        static string PropertyBackingFieldName(GProperty prop) => "_" + prop.Name.WithLowerFirst();
        static string CompositionPropertyField(GProperty prop) => "s_IdOf" + prop.Name + "Property";

        static ExpressionSyntax ClientProperty(GClass c, GProperty p) =>
            MemberAccess(ServerName(c.Name), CompositionPropertyField(p));
        
        void GenerateClass(GClass cl)
        {
            var list = cl as GList;
            
            var unit = Unit();
            
            var clientNs = NamespaceDeclaration(IdentifierName("Avalonia.Rendering.Composition"));
            var serverNs = NamespaceDeclaration(IdentifierName("Avalonia.Rendering.Composition.Server"));
            var transportNs = NamespaceDeclaration(IdentifierName("Avalonia.Rendering.Composition.Transport"));

            var inherits = cl.Inherits ?? "CompositionObject";
            var abstractModifier = cl.Abstract ? new[] {SyntaxKind.AbstractKeyword} : null;
            var visibilityModifier = cl.Internal ? SyntaxKind.InternalKeyword : SyntaxKind.PublicKeyword;
            
            var client = ClassDeclaration(cl.Name)
                .AddModifiers(abstractModifier)
                .AddModifiers(visibilityModifier, SyntaxKind.UnsafeKeyword, SyntaxKind.PartialKeyword)
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
            string changedFieldsTypeName = ChangedFieldsTypeName(cl);
            string changedFieldsName = ChangedFieldsFieldName(cl);

            if (cl.Properties.Count > 0)
                client = client
                    .AddMembers(DeclareField(changedFieldsTypeName, changedFieldsName));
            
            
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
                            })))).WithBody(Block(ParseStatement("Initialize();"))));
            }

            server = server.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "Initialize")
                    .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));

            server = server.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "DeserializeChangesExtra")
                    .AddParameterListParameters(Parameter(Identifier("c")).WithType(ParseTypeName("BatchStreamReader")))
                    .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));

            var resetBody = Block();
            var startAnimationBody = Block();
            var serverGetPropertyBody = Block();
            var serverGetCompositionPropertyBody = Block();
            var serializeMethodBody = SerializeChangesPrologue(cl);
            var deserializeMethodBody = DeserializeChangesPrologue(cl);

            var defaultsMethodBody = Block(ParseStatement("InitializeDefaultsExtra();"));
            
            foreach (var prop in cl.Properties)
            {
                var fieldName = PropertyBackingFieldName(prop);
                var typeInfo = GetTypeInfo(prop.Type);
                var (propType, filteredPropertyType, isObject, isNullable, isPassthrough,
                    serverPropertyType) = (typeInfo.RoslynType,
                    typeInfo.FilteredTypeName,
                    typeInfo.IsObject, typeInfo.IsNullable, typeInfo.IsPassthrough, typeInfo.ServerType);
                
                var animatedServer = prop.Animated;
                
                client = GenerateClientProperty(client, cl, prop, propType, isObject, isNullable);
                
                if (animatedServer)
                    server = server.AddMembers(
                        DeclareField(serverPropertyType, fieldName),
                        PropertyDeclaration(ParseTypeName(serverPropertyType), prop.Name)
                            .AddModifiers(SyntaxKind.PublicKeyword)
                            .AddAccessorListAccessors(
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithExpressionBody(
                                    ArrowExpressionClause(
                                        InvocationExpression(IdentifierName("GetAnimatedValue"),
                                            ArgumentList(SeparatedList(new[]
                                                {
                                                    Argument(IdentifierName(CompositionPropertyField(prop))),
                                                    Argument(null, Token(SyntaxKind.RefKeyword),
                                                        IdentifierName(fieldName))
                                                }
                                            ))))).WithSemicolonToken(Semicolon()),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithExpressionBody(ArrowExpressionClause(
                                            ParseExpression($"SetAnimatedValue({CompositionPropertyField(prop)}, out {PropertyBackingFieldName(prop)}, value)")))
                                    .WithSemicolonToken(Semicolon())));
                else
                {
                    server = server
                        .AddMembers(DeclareField(serverPropertyType, fieldName))
                        .AddMembers(PropertyDeclaration(ParseTypeName(serverPropertyType), prop.Name)
                            .AddModifiers(SyntaxKind.PublicKeyword)
                            .AddAccessorListAccessors(
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                                    Block(ReturnStatement(
                                        InvocationExpression(IdentifierName("GetValue"),
                                            ArgumentList(SeparatedList(new[]{
                                                    Argument(IdentifierName(CompositionPropertyField(prop))),
                                                    Argument(null, Token(SyntaxKind.RefKeyword), IdentifierName(fieldName))
                                                }
                                            )))))),
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
                                        ExpressionStatement(InvocationExpression(IdentifierName("SetValue"),
                                            ArgumentList(SeparatedList(new[]{
                                                    Argument(IdentifierName(CompositionPropertyField(prop))),
                                                    Argument(null, Token(SyntaxKind.RefKeyword), IdentifierName(fieldName)),
                                                    Argument(IdentifierName("value"))
                                                }
                                            )))),
                                        ParseStatement($"if(changed) On" + prop.Name + "Changed();")
                                    ))
                            ))
                        .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changed")
                            .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()))
                        .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changing")
                            .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));
                }
                
                resetBody = resetBody.AddStatements(
                    ExpressionStatement(InvocationExpression(MemberAccess(prop.Name, "Reset"))));
                
                serializeMethodBody = ApplySerializeField(serializeMethodBody, cl, prop, isObject, isPassthrough);
                deserializeMethodBody = ApplyDeserializeField(deserializeMethodBody,cl, prop, serverPropertyType, isObject);
                
                if (animatedServer)
                {
                    startAnimationBody = ApplyStartAnimation(startAnimationBody, cl, prop);
                }

                
                serverGetPropertyBody = ApplyGetProperty(serverGetPropertyBody, prop);
                serverGetCompositionPropertyBody = ApplyGetProperty(serverGetCompositionPropertyBody, prop, CompositionPropertyField(prop));

                server = server.AddMembers(DeclareField("CompositionProperty", CompositionPropertyField(prop),
                    EqualsValueClause(ParseExpression("CompositionProperty.Register()")),
                    SyntaxKind.InternalKeyword, SyntaxKind.StaticKeyword));
                
                if (prop.DefaultValue != null)
                {
                    defaultsMethodBody = defaultsMethodBody.AddStatements(
                        ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(prop.Name), ParseExpression(prop.DefaultValue))));
                }
            }
            
            if (cl.Properties.Count > 0)
            {
                server = server.AddMembers(((MethodDeclarationSyntax)ParseMemberDeclaration(
                            $"protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt){{}}")
                        !)
                    .WithBody(ApplyDeserializeChangesEpilogue(deserializeMethodBody, cl)));
                server = server.AddMembers(MethodDeclaration(ParseTypeName("void"), "OnFieldsDeserialized")
                    .WithParameterList(ParameterList(SingletonSeparatedList(Parameter(Identifier("changed"))
                        .WithType(ParseTypeName(ChangedFieldsTypeName(cl))))))
                    .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));
            }

            client = client.AddMembers(
                    MethodDeclaration(ParseTypeName("void"), "InitializeDefaults").WithBody(defaultsMethodBody))
                .AddMembers(
                    MethodDeclaration(ParseTypeName("void"), "InitializeDefaultsExtra")
                        .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));

            if (cl.Properties.Count > 0)
            {
                serializeMethodBody = serializeMethodBody.AddStatements(SerializeChangesEpilogue(cl));
                client = client.AddMembers(((MethodDeclarationSyntax)ParseMemberDeclaration(
                        $"private protected override void SerializeChangesCore(BatchStreamWriter writer){{}}")!)
                    .WithBody(serializeMethodBody));
            }

            if (list != null)
                client = AppendListProxy(list, client);

            if (startAnimationBody.Statements.Count != 0)
                client = WithStartAnimation(client, startAnimationBody);

            if (!cl.ServerOnly)
            {
                server = WithGetPropertyForAnimation(server, serverGetPropertyBody);
                server = WithGetCompositionProperty(server, serverGetCompositionPropertyBody);
            }

            if (cl.ServerOnly)
                server = server.AddMembers(GenerateSerializeAllMethod(cl));
            
            if(cl.Implements.Count > 0)
                foreach (var impl in cl.Implements)
                {
                    client = client.WithBaseList(client.BaseList?.AddTypes(SimpleBaseType(ParseTypeName(impl.Name))));
                    if (impl.ServerName != null)
                        server = server.WithBaseList(
                            server.BaseList?.AddTypes(SimpleBaseType(ParseTypeName(impl.ServerName))));

                    if(ParseMemberDeclaration($"{impl.ServerName} {impl.Name}.Server => Server;") is { } member)
                        client = client.AddMembers(member);
                }


            SaveTo(unit.AddMembers(GenerateChangedFieldsEnum(cl)), "Transport",
                ChangedFieldsTypeName(cl) + ".generated.cs");

            if (!cl.ServerOnly)
                SaveTo(unit.AddMembers(clientNs.AddMembers(client)),
                    cl.Name + ".generated.cs");
            
            SaveTo(unit.AddMembers(serverNs.AddMembers(server)),
                "Server", "Server" + cl.Name + ".generated.cs");
        }
        
        private static ClassDeclarationSyntax GenerateClientProperty(ClassDeclarationSyntax client, GClass cl, GProperty prop,
            TypeSyntax propType, bool isObject, bool isNullable)
        {
            var fieldName = PropertyBackingFieldName(prop);
            return client
                    .AddMembers(DeclareField(prop.Type, fieldName))
                    .AddMembers(PropertyDeclaration(propType, prop.Name)
                        .AddModifiers(prop.Internal ? SyntaxKind.InternalKeyword : SyntaxKind.PublicKeyword)
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
                                            GeneratePropertySetterAssignment(cl, prop, isObject, isNullable))
                                    ),
                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(fieldName), IdentifierName("value"))),
                                    ParseStatement($"if(changed) On" + prop.Name + "Changed();")
                                )).WithModifiers(TokenList(prop.InternalSet ? new[]{Token(SyntaxKind.InternalKeyword)} : Array.Empty<SyntaxToken>()))
                        ))
                    .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changed")
                        .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()))
                    .AddMembers(MethodDeclaration(ParseTypeName("void"), "On" + prop.Name + "Changing")
                        .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));
        }

        static EnumDeclarationSyntax GenerateChangedFieldsEnum(GClass cl)
        {
            var changedFieldsEnum = EnumDeclaration(Identifier(ChangedFieldsTypeName(cl)));
            int count = 0;

            void AddValue(string name)
            {
                var value = 1ul << count; 
                changedFieldsEnum = changedFieldsEnum.AddMembers(
                    EnumMemberDeclaration(name)
                        .WithEqualsValue(EqualsValueClause(ParseExpression(value.ToString()))));
                count++;
            }

            foreach (var prop in cl.Properties)
            {
                AddValue(prop.Name);

                if (prop.Animated) 
                    AddValue(prop.Name + "Animated");
            }

            var baseType = count <= 8 ? "byte" : count <= 16 ? "ushort" : count <= 32 ? "uint" : "ulong";
            return changedFieldsEnum.AddBaseListTypes(SimpleBaseType(ParseTypeName(baseType)))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("System.Flags")))));
        }

        static StatementSyntax GeneratePropertySetterAssignment(GClass cl, GProperty prop, bool isObject, bool isNullable)
        {
            var code = @$"
    // Update the backing value
    {PropertyBackingFieldName(prop)} = value;
    
    // Register object for serialization in the next batch
    {ChangedFieldsFieldName(cl)} |= {ChangedFieldsTypeName(cl)}.{prop.Name};
    RegisterForSerialization();
";
            if (prop.Animated)
            {
                code += @$"
    // Reset previous animation if any
    PendingAnimations.Remove({ClientProperty(cl, prop)});
    {ChangedFieldsFieldName(cl)} &= ~{ChangedFieldsTypeName(cl)}.{prop.Name}Animated;
    // Check for implicit animations
    if(ImplicitAnimations != null && ImplicitAnimations.TryGetValue(""{prop.Name}"", out var animation) == true)
    {{
        // Animation affects only current property
        if(animation is CompositionAnimation a)
        {{
            {ChangedFieldsFieldName(cl)} |= {ChangedFieldsTypeName(cl)}.{prop.Name}Animated;
            PendingAnimations[{ClientProperty(cl, prop)}] = a.CreateInstance(this.Server, value);
        }}  
        // Animation is triggered by the current field, but does not necessary affects it
        StartAnimationGroup(animation, ""{prop.Name}"", value);
    }}
";
            }

            return ParseStatement("{\n" + code + "\n}");
        }

        static BlockSyntax ApplyStartAnimation(BlockSyntax body, GClass cl, GProperty prop)
        {
            var code = $@"
if (propertyName == ""{prop.Name}"")
{{
var current = {PropertyBackingFieldName(prop)};
var server = animation.CreateInstance(this.Server, finalValue);
PendingAnimations[{ClientProperty(cl, prop)}] = server;
{ChangedFieldsFieldName(cl)} |= {ChangedFieldsTypeName(cl)}.{prop.Name}Animated;
RegisterForSerialization();
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
            "Matrix",
            "Matrix3x2",
            "Matrix4x4",
            "Quaternion",
            "Color",
            "Avalonia.Media.Color",
            "Vector3D"
        };

        static BlockSyntax ApplyGetProperty(BlockSyntax body, GProperty prop, string? expr = null)
        {
            if (VariantPropertyTypes.Contains(prop.Type))
                return body.AddStatements(
                    ParseStatement($"if(name == \"{prop.Name}\")\n return  {expr ?? prop.Name};\n")
                );

            return body;
        }
        
        private static BlockSyntax SerializeChangesPrologue(GClass cl)
        {
            return Block(
                ParseStatement("base.SerializeChangesCore(writer);"),
                ParseStatement($"writer.Write({ChangedFieldsFieldName(cl)});")
                );
        }

        private MethodDeclarationSyntax GenerateSerializeAllMethod(GClass cl)
        {
            var declaration = (MethodDeclarationSyntax)ParseMemberDeclaration(
                $"internal static void SerializeAllChanges(BatchStreamWriter writer){{}}")!;
            declaration = declaration.AddParameterListParameters(cl.Properties.Select(prop =>
            {
                var type = GetTypeInfo(prop.Type);
                return Parameter(Identifier(prop.Name.WithLowerFirst())).WithType(ParseTypeName(type.ServerType));
            }).ToArray());

            var changedName = ChangedFieldsTypeName(cl);
            var bits = cl.Properties.Select(p => changedName + "." + p.Name);
            var body = Block().AddStatements(ParseStatement($"writer.Write({string.Join("|", bits)});"));
            foreach (var prop in cl.Properties)
            {
                var type = GetTypeInfo(prop.Type);
                body = body.AddStatements(
                    ParseStatement($"writer.Write{(type.IsObject ? "Object" : "")}({prop.Name.WithLowerFirst()});"));
            }

            return declaration.WithBody(body);
        }

        private static BlockSyntax SerializeChangesEpilogue(GClass cl) =>
            Block(ParseStatement(ChangedFieldsFieldName(cl) + " = default;"));

        static BlockSyntax ApplySerializeField(BlockSyntax body, GClass cl, GProperty prop, bool isObject, bool isPassthrough)
        {
            var changedFields = ChangedFieldsFieldName(cl);
            var changedFieldsType = ChangedFieldsTypeName(cl);
            
            var code = "";
            if (prop.Animated)
            {
                code = $@"
    if(({changedFields} & {changedFieldsType}.{prop.Name}Animated) == {changedFieldsType}.{prop.Name}Animated)
        writer.WriteObject(PendingAnimations.GetAndRemove({ClientProperty(cl, prop)}));
    else ";
            }

            code += $@"
    if(({changedFields} & {changedFieldsType}.{prop.Name}) == {changedFieldsType}.{prop.Name})
        writer.Write{(isObject ? "Object" : "")}({PropertyBackingFieldName(prop)}{(isObject && !isPassthrough ? "?.Server!":"")});
";
            return body.AddStatements(ParseStatement(code));
        }

        private static BlockSyntax DeserializeChangesPrologue(GClass cl)
        {
            return Block(
                ParseStatement("base.DeserializeChangesCore(reader, committedAt);"),
                ParseStatement("DeserializeChangesExtra(reader);"),
                ParseStatement($"var changed = reader.Read<{ChangedFieldsTypeName(cl)}>();")
            );
        }

        private static BlockSyntax ApplyDeserializeChangesEpilogue(BlockSyntax body, GClass cl)
        {
            return body.AddStatements(ParseStatement("OnFieldsDeserialized(changed);"));
        }

        static BlockSyntax ApplyDeserializeField(BlockSyntax body, GClass cl, GProperty prop, string serverType, bool isObject)
        {
            var changedFieldsType = ChangedFieldsTypeName(cl);
            var code = "";
            if (prop.Animated)
            {
                code = $@"
    if((changed & {changedFieldsType}.{prop.Name}Animated) == {changedFieldsType}.{prop.Name}Animated)
            SetAnimatedValue({CompositionPropertyField(prop)}, ref {PropertyBackingFieldName(prop)}, committedAt, reader.ReadObject<IAnimationInstance>());
    else ";
            }

            var readValueCode = $"reader.Read{(isObject ? "Object" : "")}<{serverType}>()";
            code += $@"
    if((changed & {changedFieldsType}.{prop.Name}) == {changedFieldsType}.{prop.Name})
";
            code += $"{prop.Name} =  {readValueCode};";
            return body.AddStatements(ParseStatement(code));
        }

        static ClassDeclarationSyntax WithGetPropertyForAnimation(ClassDeclarationSyntax cl, BlockSyntax body)
        {
            if (body.Statements.Count == 0)
                return cl;
            body = body.AddStatements(
                ParseStatement("return base.GetPropertyForAnimation(name);"));
            var method = ((MethodDeclarationSyntax) ParseMemberDeclaration(
                    $"public override Avalonia.Rendering.Composition.Expressions.ExpressionVariant GetPropertyForAnimation(string name){{}}")!)
                .WithBody(body);

            return cl.AddMembers(method);
        }

        static ClassDeclarationSyntax WithGetCompositionProperty(ClassDeclarationSyntax cl, BlockSyntax body)
        {
            if (body.Statements.Count == 0)
                return cl;
            body = body.AddStatements(
                ParseStatement("return base.GetCompositionProperty(name);"));
            var method = ((MethodDeclarationSyntax)ParseMemberDeclaration(
                    $"public override CompositionProperty? GetCompositionProperty(string name){{}}")!)
                .WithBody(body);

            return cl.AddMembers(method);
        }

        static ClassDeclarationSyntax WithStartAnimation(ClassDeclarationSyntax cl, BlockSyntax body)
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
                    "internal override void StartAnimation(string propertyName, CompositionAnimation animation, Avalonia.Rendering.Composition.Expressions.ExpressionVariant? finalValue){}")!)
                .WithBody(body));


        }
        
    }
}
