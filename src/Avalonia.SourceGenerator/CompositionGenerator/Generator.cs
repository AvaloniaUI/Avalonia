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



        string ServerName(string c) => c != null ? ("Server" + c) : "ServerObject";
        string ChangesName(string c) => c != null ? (c + "Changes") : "ChangeSet";
        string ChangedFieldsTypeName(GClass c) => c.Name + "ChangedFields";
        string ChangedFieldsFieldName(GClass c) =>  "_changedFieldsOf" + c.Name;
        string PropertyBackingFieldName(GProperty prop) => "_" + prop.Name.WithLowerFirst();
        string ServerPropertyOffsetFieldName(GProperty prop) => "s_OffsetOf" + PropertyBackingFieldName(prop);
        string PropertyPendingAnimationFieldName(GProperty prop) => "_pendingAnimationFor" + prop.Name;

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


            var changesVarName = "c";
            var changesVar = IdentifierName(changesVarName);

            server = server.AddMembers(
                MethodDeclaration(ParseTypeName("void"), "DeserializeChangesExtra")
                    .AddParameterListParameters(Parameter(Identifier("c")).WithType(ParseTypeName("BatchStreamReader")))
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

            var uninitializedObjectName = "dummy";
            var serverStaticCtorBody = cl.Abstract
                ? Block()
                : Block(
                    ParseStatement(
                        $"var dummy = ({serverName})System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof({serverName}));"),
                    ParseStatement($"System.GC.SuppressFinalize(dummy);"),
                    ParseStatement("InitializeFieldOffsets(dummy);")
                );

            var initializeFieldOffsetsBody = cl.Inherits == null
                ? Block()
                : Block(ParseStatement($"Server{cl.Inherits}.InitializeFieldOffsets(dummy);"));

            var resetBody = Block();
            var startAnimationBody = Block();
            var serverGetPropertyBody = Block();
            var serverGetFieldOffsetBody = Block();
            var activatedBody = Block(ParseStatement("base.Activated();"));
            var deactivatedBody = Block(ParseStatement("base.Deactivated();"));
            var serializeMethodBody = SerializeChangesPrologue(cl);
            var deserializeMethodBody = DeserializeChangesPrologue(cl);

            var defaultsMethodBody = Block(ParseStatement("InitializeDefaultsExtra();"));
            
            foreach (var prop in cl.Properties)
            {
                var fieldName = PropertyBackingFieldName(prop);
                var animatedFieldName = PropertyPendingAnimationFieldName(prop);
                var fieldOffsetName = ServerPropertyOffsetFieldName(prop);
                var propType = ParseTypeName(prop.Type);
                var filteredPropertyType = prop.Type.TrimEnd('?');
                var isObject = _objects.Contains(filteredPropertyType);
                var isNullable = prop.Type.EndsWith("?");
                bool isPassthrough = false;

                if (prop.Animated)
                    client = client.AddMembers(DeclareField("IAnimationInstance?", animatedFieldName));
                client = GenerateClientProperty(client, cl, prop, propType, isObject, isNullable);

                var animatedServer = prop.Animated;
                
                var serverPropertyType = ((isObject ? "Server" : "") + prop.Type);
                if (_manuals.TryGetValue(filteredPropertyType, out var manual))
                {
                    if (manual.Passthrough)
                    {
                        isPassthrough = true;
                        serverPropertyType = prop.Type;
                    }

                    if (manual.ServerName != null)
                        serverPropertyType = manual.ServerName + (isNullable ? "?" : "");
                }
                
                if (animatedServer)
                    server = server.AddMembers(
                        DeclareField("ServerAnimatedValueStore<" + serverPropertyType + ">", fieldName),
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
                        .AddMembers(DeclareField("ServerValueStore<" + serverPropertyType + ">", fieldName))
                        .AddMembers(PropertyDeclaration(ParseTypeName(serverPropertyType), prop.Name)
                            .AddModifiers(SyntaxKind.PublicKeyword)
                            .AddAccessorListAccessors(
                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                                    Block(ReturnStatement(MemberAccess(IdentifierName(fieldName), "Value")))),
                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                                    Block(
                                        ParseStatement("var changed = false;"),
                                        IfStatement(BinaryExpression(SyntaxKind.NotEqualsExpression,
                                                 MemberAccess(IdentifierName(fieldName), "Value"),
                                                IdentifierName("value")),
                                            Block(
                                                ParseStatement("On" + prop.Name + "Changing();"),
                                                ParseStatement($"changed = true;"))
                                        ),
                                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            MemberAccess(IdentifierName(fieldName), "Value"), IdentifierName("value"))),
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
                            ExpressionStatement(
                                InvocationExpression(MemberAccess(fieldName, "SetValue"),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(IdentifierName("this")),
                                        Argument(MemberAccess(changesVar, prop.Name, "Value")),
                                    }))))),
                        IfStatement(MemberAccess(changesVar, prop.Name, "IsAnimation"),
                            ExpressionStatement(
                                InvocationExpression(MemberAccess(fieldName, "SetAnimation"),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(IdentifierName("this")),
                                        Argument(ParseExpression("c.Batch.CommitedAt")),
                                        Argument(MemberAccess(changesVar, prop.Name, "Animation")),
                                        Argument(IdentifierName(fieldOffsetName))
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
                
                serializeMethodBody = ApplySerializeField(serializeMethodBody,cl, prop, isObject, isPassthrough);
                deserializeMethodBody = ApplyDeserializeField(deserializeMethodBody,cl, prop, serverPropertyType, isObject);
                
                if (animatedServer)
                {
                    startAnimationBody = ApplyStartAnimation(startAnimationBody, cl, prop);
                    activatedBody = activatedBody.AddStatements(ParseStatement($"{fieldName}.Activate(this);"));
                    deactivatedBody = deactivatedBody.AddStatements(ParseStatement($"{fieldName}.Deactivate(this);"));
                }

                
                serverGetPropertyBody = ApplyGetProperty(serverGetPropertyBody, prop);
                serverGetFieldOffsetBody = ApplyGetProperty(serverGetFieldOffsetBody, prop, fieldOffsetName);

                server = server.AddMembers(DeclareField("int", fieldOffsetName, SyntaxKind.StaticKeyword));
                initializeFieldOffsetsBody = initializeFieldOffsetsBody.AddStatements(ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(fieldOffsetName),
                        InvocationExpression(MemberAccess(IdentifierName(uninitializedObjectName), "GetOffset"),
                            ArgumentList(SingletonSeparatedList(Argument(
                                RefExpression(MemberAccess(IdentifierName(uninitializedObjectName), fieldName)))))))));
                
                if (prop.DefaultValue != null)
                {
                    defaultsMethodBody = defaultsMethodBody.AddStatements(
                        ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(prop.Name), ParseExpression(prop.DefaultValue))));
                }
            }

            server = server.AddMembers(ConstructorDeclaration(serverName)
                .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                .WithBody(serverStaticCtorBody));
            
            server = server.AddMembers(
                ((MethodDeclarationSyntax)ParseMemberDeclaration(
                    $"protected static void InitializeFieldOffsets({serverName} dummy){{}}")!)
                .WithBody(initializeFieldOffsetsBody));

            server = server
                .AddMembers(((MethodDeclarationSyntax)ParseMemberDeclaration(
                    $"protected override void Activated(){{}}")!).WithBody(activatedBody))
                .AddMembers(((MethodDeclarationSyntax)ParseMemberDeclaration(
                    $"protected override void Deactivated(){{}}")!).WithBody(deactivatedBody));
            if (cl.Properties.Count > 0)
                server = server.AddMembers(((MethodDeclarationSyntax)ParseMemberDeclaration(
                            $"protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan commitedAt){{}}")
                        !)
                    .WithBody(deserializeMethodBody));

            client = client.AddMembers(
                    MethodDeclaration(ParseTypeName("void"), "InitializeDefaults").WithBody(defaultsMethodBody))
                .AddMembers(
                    MethodDeclaration(ParseTypeName("void"), "InitializeDefaultsExtra")
                        .AddModifiers(SyntaxKind.PartialKeyword).WithSemicolonToken(Semicolon()));
            
            if (cl.Properties.Count > 0)
                client = client.AddMembers(((MethodDeclarationSyntax)ParseMemberDeclaration(
                        $"private protected override void SerializeChangesCore(BatchStreamWriter writer){{}}")!)
                    .WithBody(serializeMethodBody));
            
            if (list != null)
                client = AppendListProxy(list, client);

            if (startAnimationBody.Statements.Count != 0)
                client = WithStartAnimation(client, startAnimationBody);
            
            server = WithGetPropertyForAnimation(server, serverGetPropertyBody);
            server = WithGetFieldOffset(server, serverGetFieldOffsetBody);
            
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


            SaveTo(unit.AddMembers(GenerateChangedFieldsEnum(cl)), "Transport",
                ChangedFieldsTypeName(cl) + ".generated.cs");
            
            SaveTo(unit.AddMembers(clientNs.AddMembers(client)),
                cl.Name + ".generated.cs");
            SaveTo(unit.AddMembers(serverNs.AddMembers(server)),
                "Server", "Server" + cl.Name + ".generated.cs");
        }
        
        private ClassDeclarationSyntax GenerateClientProperty(ClassDeclarationSyntax client, GClass cl, GProperty prop,
            TypeSyntax propType, bool isObject, bool isNullable)
        {
            var fieldName = PropertyBackingFieldName(prop);
            return client
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

        EnumDeclarationSyntax GenerateChangedFieldsEnum(GClass cl)
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

        StatementSyntax GeneratePropertySetterAssignment(GClass cl, GProperty prop, bool isObject, bool isNullable)
        {
            var pendingAnimationField = PropertyPendingAnimationFieldName(prop);

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
    {pendingAnimationField} = null;
    {ChangedFieldsFieldName(cl)} &= ~{ChangedFieldsTypeName(cl)}.{prop.Name}Animated;
    // Check for implicit animations
    if(ImplicitAnimations != null && ImplicitAnimations.TryGetValue(""{prop.Name}"", out var animation) == true)
    {{
        // Animation affects only current property
        if(animation is CompositionAnimation a)
        {{
            {ChangedFieldsFieldName(cl)} |= {ChangedFieldsTypeName(cl)}.{prop.Name}Animated;
            {pendingAnimationField} = a.CreateInstance(this.Server, value);
        }}  
        // Animation is triggered by the current field, but does not necessary affects it
        StartAnimationGroup(animation, ""{prop.Name}"", value);
    }}
";
            }

            return ParseStatement("{\n" + code + "\n}");
        }
        
        BlockSyntax ApplyStartAnimation(BlockSyntax body, GClass cl, GProperty prop)
        {
            var code = $@"
if (propertyName == ""{prop.Name}"")
{{
var current = {PropertyBackingFieldName(prop)};
var server = animation.CreateInstance(this.Server, finalValue);
{PropertyPendingAnimationFieldName(prop)} = server;
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
            "Avalonia.Media.Color"
        };
        
        BlockSyntax ApplyGetProperty(BlockSyntax body, GProperty prop, string? expr = null)
        {
            if (VariantPropertyTypes.Contains(prop.Type))
                return body.AddStatements(
                    ParseStatement($"if(name == \"{prop.Name}\")\n return  {expr ?? prop.Name};\n")
                );

            return body;
        }
        
        private BlockSyntax SerializeChangesPrologue(GClass cl)
        {
            return Block(
                ParseStatement("base.SerializeChangesCore(writer);"),
                ParseStatement($"writer.Write({ChangedFieldsFieldName(cl)});")
                );
        }
        
        BlockSyntax ApplySerializeField(BlockSyntax body, GClass cl, GProperty prop, bool isObject, bool isPassthrough)
        {
            var changedFields = ChangedFieldsFieldName(cl);
            var changedFieldsType = ChangedFieldsTypeName(cl);
            
            var code = "";
            if (prop.Animated)
            {
                code = $@"
    if(({changedFields} & {changedFieldsType}.{prop.Name}Animated) == {changedFieldsType}.{prop.Name}Animated)
        writer.WriteObject({PropertyPendingAnimationFieldName(prop)});
    else ";
            }

            code += $@"
    if(({changedFields} & {changedFieldsType}.{prop.Name}) == {changedFieldsType}.{prop.Name})
        writer.Write{(isObject ? "Object" : "")}({PropertyBackingFieldName(prop)}{(isObject && !isPassthrough ? "?.Server!":"")});
";
            return body.AddStatements(ParseStatement(code));
        }     
        
        private BlockSyntax DeserializeChangesPrologue(GClass cl)
        {
            return Block(ParseStatement($@"
base.DeserializeChangesCore(reader, commitedAt);
DeserializeChangesExtra(reader);
var changed = reader.Read<{ChangedFieldsTypeName(cl)}>();
"));
        }
        
        BlockSyntax ApplyDeserializeField(BlockSyntax body, GClass cl, GProperty prop, string serverType, bool isObject)
        {
            var changedFieldsType = ChangedFieldsTypeName(cl);
            var code = "";
            if (prop.Animated)
            {
                code = $@"
    if((changed & {changedFieldsType}.{prop.Name}Animated) == {changedFieldsType}.{prop.Name}Animated)
            {PropertyBackingFieldName(prop)}.SetAnimation(this, commitedAt, reader.ReadObject<IAnimationInstance>(), {ServerPropertyOffsetFieldName(prop)});
    else ";
            }

            var readValueCode = $"reader.Read{(isObject ? "Object" : "")}<{serverType}>()";
            code += $@"
    if((changed & {changedFieldsType}.{prop.Name}) == {changedFieldsType}.{prop.Name})
";
            if (prop.Animated)
                code += $"{PropertyBackingFieldName(prop)}.SetValue(this, {readValueCode});";
            else code += $"{prop.Name} =  {readValueCode};";
            return body.AddStatements(ParseStatement(code));
        }

        ClassDeclarationSyntax WithGetPropertyForAnimation(ClassDeclarationSyntax cl, BlockSyntax body)
        {
            if (body.Statements.Count == 0)
                return cl;
            body = body.AddStatements(
                ParseStatement("return base.GetPropertyForAnimation(name);"));
            var method = ((MethodDeclarationSyntax) ParseMemberDeclaration(
                    $"public override Avalonia.Rendering.Composition.Expressions.ExpressionVariant GetPropertyForAnimation(string name){{}}"))
                .WithBody(body);

            return cl.AddMembers(method);
        }
        
        ClassDeclarationSyntax WithGetFieldOffset(ClassDeclarationSyntax cl, BlockSyntax body)
        {
            if (body.Statements.Count == 0)
                return cl;
            body = body.AddStatements(
                ParseStatement("return base.GetFieldOffset(name);"));
            var method = ((MethodDeclarationSyntax)ParseMemberDeclaration(
                    $"public override int? GetFieldOffset(string name){{}}"))
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