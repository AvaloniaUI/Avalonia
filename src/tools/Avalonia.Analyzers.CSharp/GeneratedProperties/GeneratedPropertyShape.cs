using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avalonia.Analyzers.GeneratedProperties;

internal enum GeneratedPropertyKind
{
    Styled,
    Direct,
    Attached
}

[Flags]
internal enum PropertyDefects
{
    None = 0,
    MemberNotPartial = 1 << 0,
    ContainingTypeNotPartial = 1 << 1,
    OwnerNotAvaloniaObject = 1 << 2,
    ConflictingArguments = 1 << 3,
    DefaultValueIncompatible = 1 << 4,
    UnsetValueIncompatible = 1 << 5,
    AddOwnerSourceMissing = 1 << 6,
    AttachedShapeInvalid = 1 << 7,
    PropertyShapeInvalid = 1 << 8,
    InvalidCallbackName = 1 << 9,
}

/// <summary>
/// Common logic for checking generated property shapes.
/// Reused between property analyzer and generator.
/// </summary>
internal static class GeneratedPropertyShape
{
    public const string StyledAttributeMetadataName = "Avalonia.StyledPropertyAttribute";
    public const string DirectAttributeMetadataName = "Avalonia.DirectPropertyAttribute";
    public const string AttachedAttributeMetadataName = "Avalonia.AttachedPropertyAttribute";

    public const string AvaloniaObjectMetadataName = "Avalonia.AvaloniaObject";

    public const string GeneratedFileSuffix = ".AvaloniaProperties.g.cs";

    private const string GetPrefix = "Get";

    /// <summary>
    /// C# 14 is required for partial properties with the field keyword.
    /// Currently used roslyn version doesn't define LanguageVersion.CSharp14.
    /// </summary>
    public static bool IsCSharp14OrLater(ParseOptions options)
        => options is CSharpParseOptions { LanguageVersion: >= (LanguageVersion)1400 };

    /// <summary>
    /// Derives the attached property name from the Get method: "GetRow" → "Row".
    /// </summary>
    public static bool TryGetAttachedName(IMethodSymbol method, out string name)
    {
        if (method.Name.Length > GetPrefix.Length &&
            method.Name.StartsWith(GetPrefix, StringComparison.Ordinal) &&
            SyntaxFacts.IsValidIdentifier(method.Name.Substring(GetPrefix.Length)))
        {
            name = method.Name.Substring(GetPrefix.Length);
            return true;
        }

        name = string.Empty;
        return false;
    }

    public static PropertyDefects CheckStyledOrDirect(
        IPropertySymbol property,
        GeneratedPropertyAttributeArgs args,
        GeneratedPropertyKind kind,
        Compilation compilation)
    {
        var defects = PropertyDefects.None;

        if (property.IsStatic ||
            property.IsIndexer ||
            property.ReturnsByRef ||
            property.ReturnsByRefReadonly ||
            property.GetMethod is null ||
            property.SetMethod is not { } setter ||
            setter.IsInitOnly ||
            property.Type.IsRefLikeType ||
            property.Type.TypeKind == TypeKind.Pointer)
        {
            defects |= PropertyDefects.PropertyShapeInvalid;
        }

        if (!property.IsPartialDefinition || HasManualImplementation(property.PartialImplementationPart))
        {
            defects |= PropertyDefects.MemberNotPartial;
        }

        if (HasMultipleGeneratorAttributes(property))
        {
            defects |= PropertyDefects.ConflictingArguments;
        }

        defects |= CheckContainingTypes(property);

        if (!DerivesFromAvaloniaObject(property.ContainingType, compilation))
        {
            defects |= PropertyDefects.OwnerNotAvaloniaObject;
        }

        defects |= CheckCommonArguments(args, property.Name, property.Type, kind, compilation);

        if (kind == GeneratedPropertyKind.Direct &&
            args.UnsetValue is { } unsetValue &&
            !IsConstantCompatible(unsetValue, property.Type, compilation))
        {
            defects |= PropertyDefects.UnsetValueIncompatible;
        }

        return defects;
    }

    public static PropertyDefects CheckAttached(
        IMethodSymbol method,
        GeneratedPropertyAttributeArgs args,
        Compilation compilation)
    {
        var defects = PropertyDefects.None;

        if (!method.IsStatic ||
            method.Arity != 0 || // non-generic
            method.ReturnsVoid ||
            method.ReturnsByRef ||
            method.ReturnsByRefReadonly ||
            method.ReturnType.IsRefLikeType ||
            method.ReturnType.TypeKind == TypeKind.Pointer ||
            method.Parameters.Length != 1 ||
            method.Parameters[0].RefKind != RefKind.None ||
            !DerivesFromAvaloniaObject(method.Parameters[0].Type, compilation) ||
            !TryGetAttachedName(method, out _))
        {
            defects |= PropertyDefects.AttachedShapeInvalid;
        }

        if (!method.IsPartialDefinition || HasManualImplementation(method.PartialImplementationPart))
        {
            defects |= PropertyDefects.MemberNotPartial;
        }

        if (HasMultipleGeneratorAttributes(method))
        {
            defects |= PropertyDefects.ConflictingArguments;
        }

        defects |= CheckContainingTypes(method);

        // Typically, attached properties don't require the owner to be AvaloniaObject, and can be declared from a static class.
        // Exception is AttachedProperty<T>.AddOwner, because it requires a generic type and has AvaloniaObject constrain.
        if (args.AddOwnerFrom is not null &&
            !DerivesFromAvaloniaObject(method.ContainingType, compilation))
        {
            defects |= PropertyDefects.OwnerNotAvaloniaObject;
        }

        TryGetAttachedName(method, out var attachedName);
        defects |= CheckCommonArguments(args, attachedName, method.ReturnType, GeneratedPropertyKind.Attached, compilation);

        return defects;
    }

    private static PropertyDefects CheckCommonArguments(
        GeneratedPropertyAttributeArgs args,
        string propertyName,
        ITypeSymbol valueType,
        GeneratedPropertyKind kind,
        Compilation compilation)
    {
        var defects = PropertyDefects.None;

        if (args.AddOwnerFrom is not null &&
            (args.Inherits || args.ValidateMethodName is not null))
        {
            // AddOwner method does not accept "inherits" or "validate" parameters.
            defects |= PropertyDefects.ConflictingArguments;
        }

        if (args.DefaultValue is { } defaultValue && !IsConstantCompatible(defaultValue, valueType, compilation))
        {
            defects |= PropertyDefects.DefaultValueIncompatible;
        }

        if (args.AddOwnerFrom is not null &&
            propertyName.Length > 0 &&
            !HasAddOwnerSource(args.AddOwnerFrom, propertyName, kind, valueType))
        {
            defects |= PropertyDefects.AddOwnerSourceMissing;
        }

        if (IsInvalidCallbackName(args.ChangedMethodName) ||
            IsInvalidCallbackName(args.ValidateMethodName) ||
            IsInvalidCallbackName(args.CoerceMethodName))
        {
            defects |= PropertyDefects.InvalidCallbackName;
        }

        return defects;

        static bool IsInvalidCallbackName(string? name)
            => name is not null && !SyntaxFacts.IsValidIdentifier(name);
    }

    private static bool HasManualImplementation(ISymbol? implementationPart)
    {
        if (implementationPart is null)
        {
            return false;
        }
        
        var path = implementationPart.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath;
        return path is null || !path.EndsWith(GeneratedFileSuffix, StringComparison.Ordinal);
    }

    private static bool HasMultipleGeneratorAttributes(ISymbol symbol)
    {
        var count = 0;
        foreach (var attribute in symbol.GetAttributes())
        {
            switch (attribute.AttributeClass?.ToDisplayString())
            {
                case StyledAttributeMetadataName:
                case DirectAttributeMetadataName:
                case AttachedAttributeMetadataName:
                    count++;
                    break;
            }
        }

        return count > 1;
    }

    private static bool DerivesFromAvaloniaObject(ITypeSymbol? type, Compilation compilation)
    {
        var avaloniaObject = compilation.GetTypeByMetadataName(AvaloniaObjectMetadataName);
        if (avaloniaObject is null)
        {
            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, avaloniaObject))
            {
                return true;
            }
        }

        return false;
    }

    private static PropertyDefects CheckContainingTypes(ISymbol member)
    {
        for (var type = member.ContainingType; type is not null; type = type.ContainingType)
        {
            if (!IsPartialType(type))
            {
                return PropertyDefects.ContainingTypeNotPartial;
            }
        }

        return PropertyDefects.None;
    }

    private static bool IsPartialType(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConstantCompatible(TypedConstant constant, ITypeSymbol targetType, Compilation compilation)
    {
        if (constant.Kind == TypedConstantKind.Error)
        {
            return false;
        }

        if (constant.IsNull)
        {
            return targetType.IsReferenceType ||
                   targetType.NullableAnnotation == NullableAnnotation.Annotated ||
                   targetType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        if (constant.Type is not { } constantType)
        {
            return false;
        }

        var conversion = compilation.ClassifyCommonConversion(constantType, targetType);
        return conversion is { Exists: true, IsImplicit: true };
    }

    private static bool HasAddOwnerSource(
        ITypeSymbol sourceType,
        string propertyName,
        GeneratedPropertyKind kind,
        ITypeSymbol valueType)
    {
        if (sourceType is IErrorTypeSymbol)
        {
            return false;
        }

        var fieldName = propertyName + "Property";

        for (var current = sourceType; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(fieldName))
            {
                var memberType = member switch
                {
                    IFieldSymbol { IsStatic: true } field => field.Type as INamedTypeSymbol,
                    IPropertySymbol { IsStatic: true } property => property.Type as INamedTypeSymbol,
                    _ => null
                };

                if (memberType is not null && IsCompatibleAvaloniaProperty(memberType, kind, valueType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsCompatibleAvaloniaProperty(INamedTypeSymbol memberType, GeneratedPropertyKind kind, ITypeSymbol valueType)
    {
        switch (kind)
        {
            case GeneratedPropertyKind.Styled:
                // It's technically possible to AddOwner from AttachedProperty to StyledProperty, so walk the base chain.
                for (ITypeSymbol? current = memberType; current is not null; current = current.BaseType)
                {
                    if (Matches(current, "StyledProperty", genericsCount: 1, valueTypeArgIndex: 0, valueType))
                    {
                        return true;
                    }
                }

                return false;

            case GeneratedPropertyKind.Direct:
                return Matches(memberType, "DirectProperty", genericsCount: 2, valueTypeArgIndex: 1, valueType);

            case GeneratedPropertyKind.Attached:
                return Matches(memberType, "AttachedProperty", genericsCount: 1, valueTypeArgIndex: 0, valueType);

            default:
                return false;
        }

        static bool Matches(ITypeSymbol type, string name, int genericsCount, int valueTypeArgIndex, ITypeSymbol valueType)
            => type is INamedTypeSymbol { IsGenericType: true } named &&
               named.Name == name &&
               named.Arity == genericsCount &&
               named.ContainingNamespace is { Name: "Avalonia", ContainingNamespace.IsGlobalNamespace: true } &&
               SymbolEqualityComparer.Default.Equals(named.TypeArguments[valueTypeArgIndex], valueType);
    }
}
