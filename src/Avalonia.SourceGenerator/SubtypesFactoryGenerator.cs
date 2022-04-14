using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avalonia.SourceGenerator
{
    internal class GenerateSubtypesSyntaxReceiver : ISyntaxReceiver
    {
        public List<(MethodDeclarationSyntax, AttributeSyntax)> CandidateMethods { get; } = new();
        public List<SyntaxNode> Types { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax declarationSyntax)
            {
                foreach (var attribute in declarationSyntax.AttributeLists.SelectMany(i => i.Attributes))
                {
                    CandidateMethods.Add((declarationSyntax, attribute));
                }
            }

            if (syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax)
            {
                Types.Add(syntaxNode);
            }
        }
    }

    [Generator]
    internal class SubtypesFactoryGenerator : ISourceGenerator
    {
        private readonly GenerateSubtypesSyntaxReceiver _receiver = new();
        private static readonly string s_attributeName = typeof(SubtypesFactoryAttribute).FullName;

        public void Execute(GeneratorExecutionContext context)
        {
            var methods = new List<(IMethodSymbol, ITypeSymbol, string)>();

            foreach (var (method, attribute) in _receiver.CandidateMethods)
            {
                var semanticModel = context.Compilation.GetSemanticModel(method.SyntaxTree);
                var attributeTypeInfo = semanticModel.GetTypeInfo(attribute);
                if (attributeTypeInfo.Type is null ||
                    attributeTypeInfo.Type.ToString() != s_attributeName ||
                    attribute.ArgumentList is null)
                {
                    continue;
                }

                var arguments = attribute.ArgumentList.Arguments;
                if (arguments.Count != 2)
                {
                    continue;
                }

                if (arguments[0].Expression is not TypeOfExpressionSyntax typeOfExpr ||
                    arguments[1].Expression is not LiteralExpressionSyntax and not IdentifierNameSyntax)
                {
                    continue;
                }

                var type = semanticModel.GetTypeInfo(typeOfExpr.Type);
                var ns = semanticModel.GetConstantValue(arguments[1].Expression);
                var methodDeclInfo = semanticModel.GetDeclaredSymbol(method);

                if (type.Type is not ITypeSymbol baseType ||
                    ns.HasValue is false ||
                    ns.Value is not string nsValue ||
                    methodDeclInfo is not IMethodSymbol methodSymbol ||
                    methodSymbol.Parameters.Length != 2 ||
                    methodSymbol.Parameters[1].RefKind != RefKind.Out)
                {
                    continue;
                }

                methods.Add((methodSymbol, baseType, nsValue));
            }

            var types = new List<ITypeSymbol>();
            foreach (var type in _receiver.Types)
            {
                var semanticModel = context.Compilation.GetSemanticModel(type.SyntaxTree);
                var decl = semanticModel.GetDeclaredSymbol(type);
                if (decl is ITypeSymbol typeSymbol)
                {
                    types.Add(typeSymbol);
                }
            }

            GenerateSubTypes(context, methods, types);
        }

        private bool IsSubtypeOf(ITypeSymbol type, ITypeSymbol baseType)
        {
            if (type.BaseType is null)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(type.BaseType, baseType))
            {
                return true;
            }

            return IsSubtypeOf(type.BaseType, baseType);
        }

        private void GenerateSubTypes(
            GeneratorExecutionContext context,
            List<(IMethodSymbol Method, ITypeSymbol BaseType, string Namespace)> methods,
            List<ITypeSymbol> types)
        {
            foreach (var (method, baseType, @namespace) in methods)
            {
                var candidateTypes = types.Where(i => IsSubtypeOf(i, baseType)).Where(i => $"{i.ContainingNamespace}.".StartsWith($"{@namespace}.")).ToArray();
                var type = method.ContainingType;
                var isGeneric = type.TypeParameters.Length > 0;
                var isClass = type.TypeKind == TypeKind.Class;

                if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodDecl)
                {
                    continue;
                }

                var parameters = new SeparatedSyntaxList<ParameterSyntax>().AddRange(methodDecl.ParameterList.Parameters.Select(i => i.WithAttributeLists(new SyntaxList<AttributeListSyntax>())));

                var methodDeclText = methodDecl
                    .WithAttributeLists(new SyntaxList<AttributeListSyntax>())
                    .WithParameterList(methodDecl.ParameterList.WithParameters(parameters))
                    .WithBody(null)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                    .WithoutTrivia().ToString();

                var typeDecl = $"partial {(isClass ? "class" : "struct")} {type.Name}{(isGeneric ? $"<{string.Join(", ", type.TypeParameters)}>" : "")}";
                var source = $@"using System;
using System.Collections.Generic;

namespace {method.ContainingNamespace}
{{
    {typeDecl}
    {{
        {methodDeclText}
        {{
            var hasMatch = false;
            (hasMatch, {method.Parameters[1].Name}) = {method.Parameters[0].Name} switch
            {{
{string.Join("\n", candidateTypes.Select(i => $"                \"{i.Name}\" => (true, ({method.Parameters[1].Type})new {i}()),"))}
                _ => (false, default({method.Parameters[1].Type}))
            }};

            return hasMatch;
        }}
    }}
}}";

                context.AddSource($"{type}.{method.MetadataName}.gen.cs", source);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => _receiver);
        }
    }
}
