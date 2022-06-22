using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avalonia.SourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class SubtypesFactoryGenerator : IIncrementalGenerator
    {
        private record struct MethodTarget(IMethodSymbol Method, string MethodDecl, ITypeSymbol BaseType, string Namespace);
        private static readonly string s_attributeName = typeof(SubtypesFactoryAttribute).FullName;

        private static bool IsSubtypeOf(ITypeSymbol type, ITypeSymbol baseType)
        {
            return type.BaseType is not null && (SymbolEqualityComparer.Default.Equals(type.BaseType, baseType) || IsSubtypeOf(type.BaseType, baseType));
        }

        private static void GenerateSubTypes(SourceProductionContext context, MethodTarget methodTarget, ImmutableArray<ITypeSymbol> types)
        {
            var (method, methodDecl, baseType, @namespace) = methodTarget;
            var candidateTypes = types.Where(i => IsSubtypeOf(i, baseType)).Where(i => $"{i.ContainingNamespace}.".StartsWith($"{@namespace}.")).ToArray();
            var type = method.ContainingType;
            var isGeneric = type.TypeParameters.Length > 0;
            var isClass = type.TypeKind == TypeKind.Class;

            var typeDecl = $"partial {(isClass ? "class" : "struct")} {type.Name}{(isGeneric ? $"<{string.Join(", ", type.TypeParameters)}>" : "")}";
            var source = $@"using System;
using System.Collections.Generic;

namespace {method.ContainingNamespace}
{{
    {typeDecl}
    {{
        {methodDecl}
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

        private static MethodTarget? PopulateMethodTargets(GeneratorSyntaxContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (context.Node is MethodDeclarationSyntax method)
            {
                var attributes = method.AttributeLists.SelectMany(i => i.Attributes);
                var semanticModel = context.SemanticModel;
                foreach (var attribute in attributes)
                {
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

                    var parameters = new SeparatedSyntaxList<ParameterSyntax>().AddRange(method.ParameterList.Parameters.Select(i => i.WithAttributeLists(new SyntaxList<AttributeListSyntax>())));
                    var methodDecl = method
                        .WithAttributeLists(new SyntaxList<AttributeListSyntax>())
                        .WithParameterList(method.ParameterList.WithParameters(parameters))
                        .WithBody(null)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithoutTrivia().ToString();

                    return new MethodTarget(methodSymbol, methodDecl, baseType, nsValue);
                }
            }

            return null;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typesProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (syntaxNode, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax;
                },
                static (syntaxContext, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return syntaxContext.Node is ClassDeclarationSyntax or StructDeclarationSyntax &&
                        syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is ITypeSymbol typeSymbol
                        ? typeSymbol : null;
                })
                .SelectMany((type, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return type is null ? Array.Empty<ITypeSymbol>() : new ITypeSymbol[] { type };
                });

            var methodsProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (syntaxNode, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return syntaxNode is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
                }, PopulateMethodTargets)
                .SelectMany((method, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return method is null ? Array.Empty<MethodTarget>() : new MethodTarget[] { method.Value };
                });

            var generateContext = methodsProvider.Combine(typesProvider.Collect());

            context.RegisterSourceOutput(generateContext, static (sourceContext, source) =>
            {
                sourceContext.CancellationToken.ThrowIfCancellationRequested();
                GenerateSubTypes(sourceContext, source.Left, source.Right);
            });
        }
    }
}
