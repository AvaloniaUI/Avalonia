using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers.GeneratedProperties;

/// <summary>
/// Flags Avalonia property declarations that could be rewritten with the [StyledProperty]/[DirectProperty]/[AttachedProperty].
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConvertToGeneratedPropertyAnalyzer : DiagnosticAnalyzer
{
    private const string PropertySuffix = "Property";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [GeneratedPropertyDescriptors.Convertible];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            var compilation = start.Compilation;

            // The generated needs C# 13 partial properties.
            if (compilation is not CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp13 })
            {
                return;
            }

            var styledProperty = compilation.GetTypeByMetadataName("Avalonia.StyledProperty`1");
            var directProperty = compilation.GetTypeByMetadataName("Avalonia.DirectProperty`2");
            var attachedProperty = compilation.GetTypeByMetadataName("Avalonia.AttachedProperty`1");
            var avaloniaProperty = compilation.GetTypeByMetadataName("Avalonia.AvaloniaProperty");

            if (styledProperty is null || directProperty is null ||
                attachedProperty is null || avaloniaProperty is null ||
                compilation.GetTypeByMetadataName(GeneratedPropertyShape.StyledAttributeMetadataName) is null ||
                compilation.GetTypeByMetadataName(GeneratedPropertyShape.DirectAttributeMetadataName) is null ||
                compilation.GetTypeByMetadataName(GeneratedPropertyShape.AttachedAttributeMetadataName) is null)
            {
                return;
            }

            var symbols = new PropertyTypeSymbols(styledProperty, directProperty, attachedProperty, avaloniaProperty);
            start.RegisterSyntaxNodeAction(
                ctx => Analyze(ctx, symbols),
                SyntaxKind.FieldDeclaration);
        });
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, PropertyTypeSymbols symbols)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
        // public static readonly
        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            !fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return;
        }

        // Single field declaration only
        if (fieldDeclaration.Declaration.Variables.Count != 1)
        {
            return;
        }

        var variable = fieldDeclaration.Declaration.Variables.Single();
        if (context.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol
            {
                IsStatic: true,
                IsReadOnly: true,
                ContainingType: { IsGenericType: false } owner
            } field)
        {
            return;
        }

        if (GetKind(field.Type, symbols) is not { } kind)
        {
            return;
        }

        // Technically possible to define Avalonia Property that doesn't end in `**Property`,
        // but our source generator won't handle these. 
        if (!field.Name.EndsWith(PropertySuffix, StringComparison.Ordinal) ||
            field.Name.Length == PropertySuffix.Length)
        {
            return;
        }

        // Only accept direct assignment, skip helper calls like
        // MyProperty = CreateMyProperty()
        // Technically possible to create an avalonia property for another type, skip these.
        if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation
            || !OwnerMatches(invocation, owner, context.SemanticModel, symbols))
        {
            return;
        }

        var location = variable.Identifier.GetLocation();
        var propertyName = field.Name.Substring(0, field.Name.Length - PropertySuffix.Length);

        context.ReportDiagnostic(Diagnostic.Create(
            GeneratedPropertyDescriptors.Convertible, location, propertyName, GetAttributeName(kind)));
    }

    private static bool OwnerMatches(
        InvocationExpressionSyntax invocation, INamedTypeSymbol owner, SemanticModel semanticModel, PropertyTypeSymbols symbols)
    {
        var target = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (target is null)
        {
            return false;
        }

        if (target.IsStatic && SymbolEqualityComparer.Default.Equals(target.ContainingType, symbols.AvaloniaProperty))
        {
            return target.Name switch
            {
                "Register" or "RegisterDirect" =>
                    // Register<TOwner, TValue> / RegisterDirect<TOwner, TValue>.
                    target.TypeArguments.Length >= 1 && OwnerEquals(target.TypeArguments[0], owner),
                "RegisterAttached" when target.TypeArguments.Length == 3 =>
                    // RegisterAttached<TOwner, THost, TValue>.
                    OwnerEquals(target.TypeArguments[0], owner),
                "RegisterAttached" when target.TypeArguments.Length == 2 =>
                    // RegisterAttached<THost, TValue>(name, ownerType, ...) — owner passed as typeof.
                    GetOwnerTypeArgument(invocation, semanticModel) is { } ownerType && OwnerEquals(ownerType, owner),
                _ => false
            };
        }

        // Base.{Name}Property.AddOwner<TNewOwner>(...).
        if (target is { Name: "AddOwner", IsStatic: false, TypeArguments.Length: 1 } &&
            GetKind(target.ContainingType, symbols) is not null)
        {
            return OwnerEquals(target.TypeArguments[0], owner);
        }

        return false;

        static bool OwnerEquals(ITypeSymbol candidate, INamedTypeSymbol owner)
            => SymbolEqualityComparer.Default.Equals(candidate, owner);

        static ITypeSymbol? GetOwnerTypeArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            // The owner type can be passed positionally (RegisterAttached<THost, TValue>("Row", typeof(Helper)))
            // or by name (ownerType: typeof(Helper)); rely on the bound operation to map arguments to parameters.
            if (semanticModel.GetOperation(invocation) is not IInvocationOperation operation)
            {
                return null;
            }

            foreach (var argument in operation.Arguments)
            {
                if (argument.Parameter?.Name != "ownerType")
                    continue;

                // Unwrap the implicit conversion the operation model wraps around the typeof expression.
                var value = argument.Value is IConversionOperation conversion ? conversion.Operand : argument.Value;
                if (value is ITypeOfOperation typeOf)
                {
                    return typeOf.TypeOperand;
                }

                return null;
            }

            return null;
        }
    }

    private static GeneratedPropertyKind? GetKind(ITypeSymbol type, PropertyTypeSymbols symbols)
    {
        if (type is not INamedTypeSymbol named)
        {
            return null;
        }

        var definition = named.OriginalDefinition;
        if (SymbolEqualityComparer.Default.Equals(definition, symbols.StyledProperty))
        {
            return GeneratedPropertyKind.Styled;
        }

        if (SymbolEqualityComparer.Default.Equals(definition, symbols.DirectProperty))
        {
            return GeneratedPropertyKind.Direct;
        }

        if (SymbolEqualityComparer.Default.Equals(definition, symbols.AttachedProperty))
        {
            return GeneratedPropertyKind.Attached;
        }

        return null;
    }

    private static string GetAttributeName(GeneratedPropertyKind kind) => kind switch
    {
        GeneratedPropertyKind.Styled => "StyledProperty",
        GeneratedPropertyKind.Direct => "DirectProperty",
        _ => "AttachedProperty",
    };

    private sealed class PropertyTypeSymbols(
        INamedTypeSymbol styledProperty,
        INamedTypeSymbol directProperty,
        INamedTypeSymbol attachedProperty,
        INamedTypeSymbol avaloniaProperty)
    {
        public INamedTypeSymbol StyledProperty { get; } = styledProperty;
        public INamedTypeSymbol DirectProperty { get; } = directProperty;
        public INamedTypeSymbol AttachedProperty { get; } = attachedProperty;
        public INamedTypeSymbol AvaloniaProperty { get; } = avaloniaProperty;
    }
}
