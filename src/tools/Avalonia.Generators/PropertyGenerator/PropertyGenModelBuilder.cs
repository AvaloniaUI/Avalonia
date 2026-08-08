using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Analyzers.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avalonia.Generators.PropertyGenerator;

/// <summary>
/// Transforms a <see cref="GeneratorAttributeSyntaxContext"/> hit into a cacheable <see cref="PropertyGenModel"/>.
/// </summary>
internal static class PropertyGenModelBuilder
{
    private static readonly SymbolDisplayFormat s_typeFormatWithNullable =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static PropertyGenModel? Build(
        GeneratorAttributeSyntaxContext context,
        GeneratedPropertyKind kind)
    {
        var args = GeneratedPropertyAttributeArgs.Read(context.Attributes[0]);
        var compilation = context.SemanticModel.Compilation;

        return kind switch
        {
            GeneratedPropertyKind.Attached when context.TargetSymbol is IMethodSymbol method =>
                GeneratedPropertyShape.CheckAttached(method, args, compilation) == PropertyDefects.None
                    ? BuildAttached(method, args, context.TargetNode)
                    : null,
            GeneratedPropertyKind.Styled or GeneratedPropertyKind.Direct when context.TargetSymbol is IPropertySymbol property =>
                GeneratedPropertyShape.CheckStyledOrDirect(property, args, kind, compilation) == PropertyDefects.None
                    ? BuildStyledOrDirect(property, args, kind, context.TargetNode)
                    : null,
            _ => null,
        };
    }

    private static PropertyGenModel BuildStyledOrDirect(
        IPropertySymbol property,
        GeneratedPropertyAttributeArgs args,
        GeneratedPropertyKind kind,
        SyntaxNode declaration)
    {
        var setter = property.SetMethod!;
        // C# only allows an accessor modifier that is strictly more restrictive than the
        // property's accessibility, so "differs" means "restricted" (the `private set` pattern).
        var setterIsRestricted = setter.DeclaredAccessibility != property.DeclaredAccessibility;
        var setterAccessibility = setterIsRestricted
            ? FormatAccessibility(setter.DeclaredAccessibility)
            : null;

        return new PropertyGenModel(
            Kind: kind,
            ContainingType: BuildTypeDeclaration(property.ContainingType),
            Name: property.Name,
            ValueTypeRef: property.Type.ToDisplayString(s_typeFormatWithNullable),
            MemberAccessibility: FormatAccessibility(property.DeclaredAccessibility),
            InheritanceModifiers: FormatInheritanceModifiers(declaration),
            SetterAccessibility: setterAccessibility,
            SetterIsNonPublic: setterIsRestricted,
            OwnerIsStatic: false,
            HostTypeRef: null,
            HostParamName: null,
            AddOwnerFromTypeRef: args.AddOwnerFrom?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DefaultValueExpr: args.DefaultValue is { } defaultValue
                ? TypedConstantFormatter.FormatAsExpression(defaultValue, property.Type)
                : null,
            UnsetValueExpr: args.UnsetValue is { } unsetValue
                ? TypedConstantFormatter.FormatAsExpression(unsetValue, property.Type)
                : null,
            DefaultBindingMode: args.DefaultBindingMode,
            Inherits: args.Inherits,
            EnableDataValidation: args.EnableDataValidation,
            ChangedMethodName: args.ChangedMethodName,
            ValidateMethodName: args.ValidateMethodName,
            CoerceMethodName: args.CoerceMethodName);
    }

    private static PropertyGenModel BuildAttached(
        IMethodSymbol method,
        GeneratedPropertyAttributeArgs args,
        SyntaxNode declaration)
    {
        GeneratedPropertyShape.TryGetAttachedName(method, out var name);
        var host = method.Parameters[0];

        return new PropertyGenModel(
            Kind: GeneratedPropertyKind.Attached,
            ContainingType: BuildTypeDeclaration(method.ContainingType),
            Name: name,
            ValueTypeRef: method.ReturnType.ToDisplayString(s_typeFormatWithNullable),
            MemberAccessibility: FormatAccessibility(method.DeclaredAccessibility),
            InheritanceModifiers: FormatInheritanceModifiers(declaration),
            SetterAccessibility: null,
            SetterIsNonPublic: false,
            OwnerIsStatic: method.ContainingType.IsStatic,
            HostTypeRef: host.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            HostParamName: host.Name,
            AddOwnerFromTypeRef: args.AddOwnerFrom?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DefaultValueExpr: args.DefaultValue is { } defaultValue
                ? TypedConstantFormatter.FormatAsExpression(defaultValue, method.ReturnType)
                : null,
            UnsetValueExpr: null,
            DefaultBindingMode: args.DefaultBindingMode,
            Inherits: args.Inherits,
            EnableDataValidation: false,
            ChangedMethodName: args.ChangedMethodName,
            ValidateMethodName: args.ValidateMethodName,
            CoerceMethodName: args.CoerceMethodName);
    }

    private static TypeDeclarationModel BuildTypeDeclaration(INamedTypeSymbol type)
    {
        var declarations = new List<string>();
        var hintName = new StringBuilder();

        for (var current = type; current is not null; current = current.ContainingType)
        {
            declarations.Insert(0, FormatDeclarationHeader(current));

            if (hintName.Length > 0)
            {
                hintName.Insert(0, '.');
            }

            hintName.Insert(0, current.Arity > 0 ? $"{current.Name}_{current.Arity}" : current.Name);
        }

        var ns = type.ContainingNamespace.IsGlobalNamespace
            ? null
            : type.ContainingNamespace.ToDisplayString();

        if (ns is not null)
        {
            hintName.Insert(0, '.').Insert(0, ns);
        }

        hintName.Append(GeneratedPropertyShape.GeneratedFileSuffix);

        return new TypeDeclarationModel(
            Namespace: ns,
            ClassDeclarations: new(declarations),
            OwnerTypeRef: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            HintName: hintName.ToString());
    }

    private static string FormatDeclarationHeader(INamedTypeSymbol type)
    {
        var keyword = (type.IsRecord, type.TypeKind) switch
        {
            (true, TypeKind.Struct) => "record struct",
            (false, TypeKind.Struct) => "struct",
            (true, _) => "record",
            _ => "class",
        };

        var typeParameters = type.Arity > 0
            ? $"<{string.Join(", ", type.TypeParameters.Select(static p => p.Name))}>"
            : string.Empty;

        return $"partial {keyword} {type.Name}{typeParameters}";
    }

    private static string FormatInheritanceModifiers(SyntaxNode declaration)
    {
        if (declaration is not MemberDeclarationSyntax member)
        {
            return string.Empty;
        }

        // The implementing declaration must repeat the authored member's virtual/override/sealed/new modifiers exactly.
        var result = new StringBuilder();
        foreach (var modifier in member.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.NewKeyword) ||
                modifier.IsKind(SyntaxKind.VirtualKeyword) ||
                modifier.IsKind(SyntaxKind.OverrideKeyword) ||
                modifier.IsKind(SyntaxKind.SealedKeyword))
            {
                result.Append(modifier.ValueText).Append(' ');
            }
        }

        return result.ToString();
    }

    private static string FormatAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "private",
    };
}
