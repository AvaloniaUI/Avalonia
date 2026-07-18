using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers.GeneratedProperties;

/// <summary>
/// Flags Avalonia property declarations that could be rewritten with the [StyledProperty]/[DirectProperty]/[AttachedProperty]
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
            start.RegisterOperationAction(ctx => Analyze(ctx, symbols), OperationKind.FieldInitializer);
        });
    }

    private static void Analyze(OperationAnalysisContext context, PropertyTypeSymbols symbols)
    {
        // List patterns aren't available in this netstandard2.0 project (no System.Index), so
        // check the single-field case explicitly.
        if (context.Operation is not IFieldInitializerOperation { InitializedFields.Length: 1 } initializer ||
            initializer.InitializedFields[0] is not
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
            field.Name.Length == PropertySuffix.Length ||
            GetSingleDeclaratorLocation(field, context.CancellationToken) is not { } location)
        {
            return;
        }

        // Only accept direct assignment, skip helper calls like
        // MyProperty = CreateMyProperty()
        if (Unwrap(initializer.Value) is not IInvocationOperation registration)
        {
            return;
        }

        // Technically possible to create an avalonia property for another type, skip these.
        if (!OwnerMatches(registration, owner, symbols))
        {
            return;
        }
        
        var propertyName = field.Name.Substring(0, field.Name.Length - PropertySuffix.Length);
        context.ReportDiagnostic(Diagnostic.Create(
            GeneratedPropertyDescriptors.Convertible, location, propertyName, GetAttributeName(kind)));
    }

    private static bool OwnerMatches(IInvocationOperation registration, INamedTypeSymbol owner, PropertyTypeSymbols symbols)
    {
        var target = registration.TargetMethod;

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
                    GetOwnerTypeArgument(registration) is { } ownerType && OwnerEquals(ownerType, owner),
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

        static ITypeSymbol? GetOwnerTypeArgument(IInvocationOperation registration)
        {
            foreach (var argument in registration.Arguments)
            {
                if (argument.Parameter?.Name == "ownerType" && Unwrap(argument.Value) is ITypeOfOperation typeOf)
                {
                    return typeOf.TypeOperand;
                }
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

    private static Location? GetSingleDeclaratorLocation(IFieldSymbol field, System.Threading.CancellationToken cancellationToken)
    {
        // A single declarator per field: the generator emits one field per property, so
        // "static readonly ... AProperty = ..., BProperty = ..." is excluded.
        if (field.DeclaringSyntaxReferences is not { Length: 1 } references ||
            references[0].GetSyntax(cancellationToken) is not VariableDeclaratorSyntax
            {
                Parent: VariableDeclarationSyntax { Variables.Count: 1 }
            } declarator)
        {
            return null;
        }

        return declarator.Identifier.GetLocation();
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation { Conversion.IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

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
