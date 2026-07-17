using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Avalonia.Analyzers.GeneratedProperties;

/// <summary>
/// Reports diagnostics for [StyledProperty]/[DirectProperty]/[AttachedProperty] misuse.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedPropertyAnalyzer : DiagnosticAnalyzer
{
    private const string PropertySuffix = "Property";

    private enum CallbackShape
    {
        Changed,
        AttachedChanged,
        Validate,
        Coerce,
    }

    // By default, Roslyn doesn't seem to include nullable in ToDisplayString.
    private static readonly SymbolDisplayFormat s_typeFormatWithNullable =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        GeneratedPropertyDescriptors.OwnerNotAvaloniaObject,
        GeneratedPropertyDescriptors.ConflictingArguments,
        GeneratedPropertyDescriptors.IncompatibleConstant,
        GeneratedPropertyDescriptors.AddOwnerSourceMissing,
        GeneratedPropertyDescriptors.InvalidAttachedShape,
        GeneratedPropertyDescriptors.UnboundCallback,
        GeneratedPropertyDescriptors.NotPartial,
        GeneratedPropertyDescriptors.InvalidShape,
        GeneratedPropertyDescriptors.NameSuffix,
        GeneratedPropertyDescriptors.StyledNonPublicSetter,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            var compilation = start.Compilation;
            var styled = compilation.GetTypeByMetadataName(GeneratedPropertyShape.StyledAttributeMetadataName);
            var direct = compilation.GetTypeByMetadataName(GeneratedPropertyShape.DirectAttributeMetadataName);
            var attached = compilation.GetTypeByMetadataName(GeneratedPropertyShape.AttachedAttributeMetadataName);
            if (styled is null && direct is null && attached is null)
            {
                return;
            }

            var languageVersionOk = compilation is CSharpCompilation { LanguageVersion: >= (LanguageVersion)1400 };

            start.RegisterSymbolAction(
                context => AnalyzeProperty(context, styled, direct, languageVersionOk),
                SymbolKind.Property);
            start.RegisterSymbolAction(
                context => AnalyzeMethod(context, attached, languageVersionOk),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeProperty(
        SymbolAnalysisContext context,
        INamedTypeSymbol? styledAttribute,
        INamedTypeSymbol? directAttribute,
        bool languageVersionOk)
    {
        var property = (IPropertySymbol)context.Symbol;
        var (attribute, kind) = FindAttribute(property, styledAttribute, directAttribute);
        if (attribute is null)
        {
            return;
        }

        var args = GeneratedPropertyAttributeArgs.Read(attribute);
        var defects = GeneratedPropertyShape.CheckStyledOrDirect(property, args, kind, context.Compilation);

        ReportCommonDefects(context, property, attribute, args, defects, kind, languageVersionOk);

        if (defects.HasFlag(PropertyDefects.PropertyShapeInvalid))
        {
            Report(context, GeneratedPropertyDescriptors.InvalidShape, MemberLocation(property), property.Name);
        }

        if (defects.HasFlag(PropertyDefects.DefaultValueIncompatible))
        {
            ReportIncompatibleConstant(context, property, attribute, args.DefaultValue, "DefaultValue", property.Type);
        }

        if (defects.HasFlag(PropertyDefects.UnsetValueIncompatible))
        {
            ReportIncompatibleConstant(context, property, attribute, args.UnsetValue, "UnsetValue", property.Type);
        }

        if (defects == PropertyDefects.None && languageVersionOk)
        {
            CheckCallbacks(context, property, attribute, args, property.Type, hostType: null);
        }

        if (property.Name.EndsWith(PropertySuffix, StringComparison.Ordinal) &&
            property.Name.Length > PropertySuffix.Length)
        {
            Report(context, GeneratedPropertyDescriptors.NameSuffix, MemberLocation(property),
                property.Name, property.Name.Substring(0, property.Name.Length - PropertySuffix.Length));
        }

        if (kind == GeneratedPropertyKind.Styled &&
            property.SetMethod is { } setter &&
            setter.DeclaredAccessibility != property.DeclaredAccessibility)
        {
            Report(context, GeneratedPropertyDescriptors.StyledNonPublicSetter, MemberLocation(property), property.Name);
        }
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol? attachedAttribute,
        bool languageVersionOk)
    {
        if (attachedAttribute is null || context.Symbol is not IMethodSymbol method)
        {
            return;
        }

        var attribute = method.GetAttributes().FirstOrDefault(
            attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attachedAttribute));
        if (attribute is null)
        {
            return;
        }

        var args = GeneratedPropertyAttributeArgs.Read(attribute);
        var defects = GeneratedPropertyShape.CheckAttached(method, args, context.Compilation);

        ReportCommonDefects(context, method, attribute, args, defects, GeneratedPropertyKind.Attached, languageVersionOk);

        if (defects.HasFlag(PropertyDefects.AttachedShapeInvalid))
        {
            Report(context, GeneratedPropertyDescriptors.InvalidAttachedShape, MemberLocation(method), method.Name);
        }

        if (defects.HasFlag(PropertyDefects.DefaultValueIncompatible))
        {
            ReportIncompatibleConstant(context, method, attribute, args.DefaultValue, "DefaultValue", method.ReturnType);
        }

        if (defects == PropertyDefects.None && languageVersionOk)
        {
            CheckCallbacks(context, method, attribute, args, method.ReturnType, method.Parameters[0].Type);
        }

        if (GeneratedPropertyShape.TryGetAttachedName(method, out var attachedName) &&
            attachedName.EndsWith(PropertySuffix, StringComparison.Ordinal) &&
            attachedName.Length > PropertySuffix.Length)
        {
            Report(context, GeneratedPropertyDescriptors.NameSuffix, MemberLocation(method),
                attachedName, attachedName.Substring(0, attachedName.Length - PropertySuffix.Length));
        }
    }

    private static void ReportCommonDefects(
        SymbolAnalysisContext context,
        ISymbol member,
        AttributeData attribute,
        GeneratedPropertyAttributeArgs args,
        PropertyDefects defects,
        GeneratedPropertyKind kind,
        bool languageVersionOk)
    {
        if (!languageVersionOk)
        {
            var version = context.Compilation is CSharpCompilation csharp
                ? csharp.LanguageVersion.ToDisplayString()
                : "unknown";
            Report(context, GeneratedPropertyDescriptors.NotPartial, MemberLocation(member),
                CodeFixProperties(GeneratedPropertyDescriptors.Properties.DefectLanguageVersion),
                $"C# 14 or later (current language version: {version})");
        }

        if (defects.HasFlag(PropertyDefects.MemberNotPartial))
        {
            Report(context, GeneratedPropertyDescriptors.NotPartial, MemberLocation(member),
                CodeFixProperties(GeneratedPropertyDescriptors.Properties.DefectMemberNotPartial),
                $"'{member.Name}' to be declared partial member");
        }

        if (defects.HasFlag(PropertyDefects.ContainingTypeNotPartial))
        {
            var nonPartialType = FindNonPartialContainingType(member);
            Report(context, GeneratedPropertyDescriptors.NotPartial, MemberLocation(member),
                CodeFixProperties(GeneratedPropertyDescriptors.Properties.DefectContainingTypeNotPartial),
                $"the containing type '{nonPartialType}' to be declared partial");
        }

        if (defects.HasFlag(PropertyDefects.OwnerNotAvaloniaObject))
        {
            Report(context, GeneratedPropertyDescriptors.OwnerNotAvaloniaObject, MemberLocation(member),
                member.ContainingType.Name);
        }

        if (defects.HasFlag(PropertyDefects.ConflictingArguments))
        {
            ReportConflicts(context, member, attribute, args);
        }

        if (defects.HasFlag(PropertyDefects.AddOwnerSourceMissing) &&
            args.AddOwnerFrom is not (null or IErrorTypeSymbol))
        {
            var name = member is IMethodSymbol method && GeneratedPropertyShape.TryGetAttachedName(method, out var attachedName)
                ? attachedName
                : member.Name;
            Report(context, GeneratedPropertyDescriptors.AddOwnerSourceMissing,
                ArgumentLocation(attribute, nameof(GeneratedPropertyAttributeArgs.AddOwnerFrom), member),
                args.AddOwnerFrom.ToDisplayString(), name, kind.ToString().ToLowerInvariant());
        }

        if (defects.HasFlag(PropertyDefects.InvalidCallbackName))
        {
            foreach (var (parameterName, methodName) in EnumerateCallbacks(args))
            {
                if (methodName is not null && !SyntaxFacts.IsValidIdentifier(methodName))
                {
                    Report(context, GeneratedPropertyDescriptors.UnboundCallback,
                        ArgumentLocation(attribute, parameterName, member),
                        methodName, "a valid method name passed via nameof(...)");
                }
            }
        }
    }

    private static void ReportConflicts(
        SymbolAnalysisContext context,
        ISymbol member,
        AttributeData attribute,
        GeneratedPropertyAttributeArgs args)
    {
        if (GeneratedPropertyShape.HasMultipleGeneratorAttributes(member))
        {
            Report(context, GeneratedPropertyDescriptors.ConflictingArguments, MemberLocation(member),
                attribute.AttributeClass?.Name ?? "the property generator attribute",
                "other property generator attributes on the same member");
        }

        if (args.AddOwnerFrom is null)
        {
            return;
        }

        if (args.Inherits)
        {
            Report(context, GeneratedPropertyDescriptors.ConflictingArguments,
                ArgumentLocation(attribute, nameof(GeneratedPropertyAttributeArgs.Inherits), member),
                "Inherits", "AddOwnerFrom");
        }

        if (args.ValidateMethodName is not null)
        {
            Report(context, GeneratedPropertyDescriptors.ConflictingArguments,
                ArgumentLocation(attribute, nameof(GeneratedPropertyAttributeArgs.ValidateMethodName), member),
                "ValidateMethodName", "AddOwnerFrom");
        }
    }

    private static void ReportIncompatibleConstant(
        SymbolAnalysisContext context,
        ISymbol member,
        AttributeData attribute,
        TypedConstant? constant,
        string parameterName,
        ITypeSymbol targetType)
    {
        var constantType = constant?.Type?.ToDisplayString() ?? "null";
        Report(context, GeneratedPropertyDescriptors.IncompatibleConstant,
            ArgumentLocation(attribute, parameterName, member),
            parameterName, constantType, targetType.ToDisplayString());
    }

    private static void CheckCallbacks(
        SymbolAnalysisContext context,
        ISymbol member,
        AttributeData attribute,
        GeneratedPropertyAttributeArgs args,
        ITypeSymbol valueType,
        ITypeSymbol? hostType)
    {
        var containingType = member.ContainingType;

        if (args.ChangedMethodName is { } changed)
        {
            var shape = hostType is null ? CallbackShape.Changed : CallbackShape.AttachedChanged;
            CheckCallback(context, member, attribute, nameof(GeneratedPropertyAttributeArgs.ChangedMethodName),
                changed, shape, containingType, valueType, hostType);
        }

        if (args.ValidateMethodName is { } validate)
        {
            CheckCallback(context, member, attribute, nameof(GeneratedPropertyAttributeArgs.ValidateMethodName),
                validate, CallbackShape.Validate, containingType, valueType, hostType: null);
        }

        if (args.CoerceMethodName is { } coerce)
        {
            CheckCallback(context, member, attribute, nameof(GeneratedPropertyAttributeArgs.CoerceMethodName),
                coerce, CallbackShape.Coerce, containingType, valueType, hostType: null);
        }
    }

    private static void CheckCallback(
        SymbolAnalysisContext context,
        ISymbol member,
        AttributeData attribute,
        string parameterName,
        string methodName,
        CallbackShape shape,
        INamedTypeSymbol containingType,
        ITypeSymbol valueType,
        ITypeSymbol? hostType)
    {
        if (IsCallbackImplemented(context.Compilation, containingType, methodName, shape, valueType, hostType))
        {
            return;
        }

        var signature = RenderCallbackSignature(methodName, shape, valueType, hostType);
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(GeneratedPropertyDescriptors.Properties.MethodName, methodName)
            .Add(GeneratedPropertyDescriptors.Properties.Signature, signature);

        context.ReportDiagnostic(Diagnostic.Create(
            GeneratedPropertyDescriptors.UnboundCallback,
            ArgumentLocation(attribute, parameterName, member),
            properties,
            methodName, signature));
    }

    private static bool IsCallbackImplemented(
        Compilation compilation,
        INamedTypeSymbol containingType,
        string methodName,
        CallbackShape shape,
        ITypeSymbol valueType,
        ITypeSymbol? hostType)
    {
        foreach (var candidate in containingType.GetMembers(methodName).OfType<IMethodSymbol>())
        {
            if (candidate.Arity != 0 ||
                candidate.IsStatic == (shape == CallbackShape.Changed) ||
                !ParametersMatch(compilation, candidate, shape, valueType, hostType) ||
                !ReturnMatches(candidate, shape, valueType))
            {
                continue;
            }

            // Implemented either as the partial pair (definition + implementation, where the
            // definition typically comes from the generator) or as a plain method (an
            // implementing-only declaration; the compiler reports any duplicate-member issues).
            return !candidate.IsPartialDefinition || candidate.PartialImplementationPart is not null;
        }

        return false;
    }

    private static bool ParametersMatch(
        Compilation compilation,
        IMethodSymbol candidate,
        CallbackShape shape,
        ITypeSymbol valueType,
        ITypeSymbol? hostType)
    {
        var comparer = SymbolEqualityComparer.Default;
        var parameters = candidate.Parameters;

        return shape switch
        {
            CallbackShape.Changed =>
                parameters.Length == 2 &&
                comparer.Equals(parameters[0].Type, valueType) &&
                comparer.Equals(parameters[1].Type, valueType),
            CallbackShape.AttachedChanged =>
                parameters.Length == 3 &&
                comparer.Equals(parameters[0].Type, hostType) &&
                comparer.Equals(parameters[1].Type, valueType) &&
                comparer.Equals(parameters[2].Type, valueType),
            CallbackShape.Validate =>
                parameters.Length == 1 &&
                comparer.Equals(parameters[0].Type, valueType),
            CallbackShape.Coerce =>
                parameters.Length == 2 &&
                comparer.Equals(parameters[0].Type, compilation.GetTypeByMetadataName(GeneratedPropertyShape.AvaloniaObjectMetadataName)) &&
                comparer.Equals(parameters[1].Type, valueType),
            _ => false,
        };
    }

    private static bool ReturnMatches(IMethodSymbol candidate, CallbackShape shape, ITypeSymbol valueType) => shape switch
    {
        CallbackShape.Changed or CallbackShape.AttachedChanged => candidate.ReturnsVoid,
        CallbackShape.Validate => candidate.ReturnType.SpecialType == SpecialType.System_Boolean,
        CallbackShape.Coerce => SymbolEqualityComparer.Default.Equals(candidate.ReturnType, valueType),
        _ => false,
    };

    private static string RenderCallbackSignature(
        string methodName,
        CallbackShape shape,
        ITypeSymbol valueType,
        ITypeSymbol? hostType)
    {
        var value = valueType.ToDisplayString(s_typeFormatWithNullable);
        return shape switch
        {
            CallbackShape.Changed =>
                $"private partial void {methodName}({value} oldValue, {value} newValue)",
            CallbackShape.AttachedChanged =>
                $"private static partial void {methodName}({hostType!.ToDisplayString(s_typeFormatWithNullable)} host, {value} oldValue, {value} newValue)",
            CallbackShape.Validate =>
                $"private static partial bool {methodName}({value} value)",
            CallbackShape.Coerce =>
                $"private static partial {value} {methodName}(global::Avalonia.AvaloniaObject sender, {value} value)",
            _ => methodName,
        };
    }

    private static (AttributeData? Attribute, GeneratedPropertyKind Kind) FindAttribute(
        IPropertySymbol property,
        INamedTypeSymbol? styledAttribute,
        INamedTypeSymbol? directAttribute)
    {
        foreach (var attribute in property.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, styledAttribute))
            {
                return (attribute, GeneratedPropertyKind.Styled);
            }

            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, directAttribute))
            {
                return (attribute, GeneratedPropertyKind.Direct);
            }
        }

        return (null, default);
    }

    private static string FindNonPartialContainingType(ISymbol member)
    {
        for (var type = member.ContainingType; type is not null; type = type.ContainingType)
        {
            var isPartial = type.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .Any(static declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
            if (!isPartial)
            {
                return type.Name;
            }
        }

        return member.ContainingType.Name;
    }

    private static IEnumerable<(string ParameterName, string? MethodName)> EnumerateCallbacks(
        GeneratedPropertyAttributeArgs args)
    {
        yield return (nameof(GeneratedPropertyAttributeArgs.ChangedMethodName), args.ChangedMethodName);
        yield return (nameof(GeneratedPropertyAttributeArgs.ValidateMethodName), args.ValidateMethodName);
        yield return (nameof(GeneratedPropertyAttributeArgs.CoerceMethodName), args.CoerceMethodName);
    }

    private static Location MemberLocation(ISymbol symbol)
        => symbol.Locations.FirstOrDefault() ?? Location.None;

    private static Location ArgumentLocation(AttributeData attribute, string parameterName, ISymbol fallback)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntax)
        {
            if (syntax.ArgumentList is { } argumentList)
            {
                foreach (var argument in argumentList.Arguments)
                {
                    if (argument.NameEquals?.Name.Identifier.ValueText == parameterName)
                    {
                        return argument.GetLocation();
                    }
                }
            }

            return syntax.GetLocation();
        }

        return MemberLocation(fallback);
    }

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object?[] messageArgs)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        ImmutableDictionary<string, string?> properties,
        params object?[] messageArgs)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location, properties, messageArgs));

    private static ImmutableDictionary<string, string?> CodeFixProperties(string defect)
        => ImmutableDictionary<string, string?>.Empty.Add(GeneratedPropertyDescriptors.Properties.Defect, defect);
}
