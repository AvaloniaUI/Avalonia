using Avalonia.Analyzers.GeneratedProperties;
using Avalonia.Generators.Common;

namespace Avalonia.Generators.PropertyGenerator;

/// <param name="Namespace">Containing namespace, or null for the global namespace.</param>
/// <param name="ClassDeclarations">
/// Partial type declaration headers, outermost first, e.g. ["partial class Outer", "partial class MyControl"].
/// </param>
/// <param name="OwnerTypeRef">Fully-qualified owner type reference, e.g. "global::MyNs.Outer.MyControl".</param>
/// <param name="HintName">Generated file hint name, e.g. "MyNs.Outer.MyControl_1.AvaloniaProperties.g.cs".</param>
internal sealed record TypeDeclarationModel(
    string? Namespace,
    EquatableList<string> ClassDeclarations,
    string OwnerTypeRef,
    string HintName);

/// <summary>
/// Everything needed to emit a single generated Avalonia property.
/// </summary>
internal sealed record PropertyGenModel(
    GeneratedPropertyKind Kind,
    TypeDeclarationModel ContainingType,
    string Name,
    string ValueTypeRef,
    string MemberAccessibility,
    string InheritanceModifiers,
    string? SetterAccessibility,
    bool SetterIsNonPublic,
    bool OwnerIsStatic,
    string? HostTypeRef,
    string? HostParamName,
    string? AddOwnerFromTypeRef,
    string? DefaultValueExpr,
    string? UnsetValueExpr,
    int? DefaultBindingMode,
    bool Inherits,
    bool EnableDataValidation,
    string? ChangedMethodName,
    string? ValidateMethodName,
    string? CoerceMethodName);

/// <summary>
/// All generated properties of one containing type — the unit that maps to one emitted file.
/// </summary>
internal sealed record PropertyGenTypeModel(
    TypeDeclarationModel Type,
    EquatableList<PropertyGenModel> Properties);
