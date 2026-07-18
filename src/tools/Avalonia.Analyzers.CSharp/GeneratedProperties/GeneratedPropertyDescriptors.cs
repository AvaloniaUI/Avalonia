using Microsoft.CodeAnalysis;

namespace Avalonia.Analyzers.GeneratedProperties;

/// <summary>
/// Diagnostics for [StyledProperty]/[DirectProperty]/[AttachedProperty] misuse.
/// </summary>
internal static class GeneratedPropertyDescriptors
{
    private const string Category = "AvaloniaProperty";

    /// Properties of keys passed from analyzer to code fixer.
    public static class Properties
    {
        public const string Defect = nameof(Defect);
        public const string MethodName = nameof(MethodName);
        public const string Signature = nameof(Signature);

        public const string DefectMemberNotPartial = "MemberNotPartial";
        public const string DefectContainingTypeNotPartial = "ContainingTypeNotPartial";
        public const string DefectLanguageVersion = "LanguageVersion";
    }

    public static readonly DiagnosticDescriptor OwnerNotAvaloniaObject = new(
        DiagnosticIds.GeneratedPropertyOwnerNotAvaloniaObject,
        "Containing type must derive from AvaloniaObject",
        "The containing type '{0}' must derive from AvaloniaObject to host this generated Avalonia property",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingArguments = new(
        DiagnosticIds.GeneratedPropertyConflictingArguments,
        "Conflicting generated property attribute arguments",
        "'{0}' cannot be combined with '{1}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Inherits and ValidateMethodName are fixed on the source property and cannot be overridden per owner via AddOwnerFrom.");

    public static readonly DiagnosticDescriptor IncompatibleConstant = new(
        DiagnosticIds.GeneratedPropertyIncompatibleConstant,
        "Constant is not compatible with the property type",
        "The {0} constant of type '{1}' is not implicitly convertible to the property type '{2}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AddOwnerSourceMissing = new(
        DiagnosticIds.GeneratedPropertyAddOwnerSourceMissing,
        "AddOwnerFrom type does not expose the source property",
        "Type '{0}' does not expose a static '{1}Property' member compatible with this generated {2} property",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAttachedShape = new(
        DiagnosticIds.GeneratedPropertyInvalidAttachedShape,
        "Invalid attached property accessor shape",
        "'{0}' must be a static partial method named 'Get{{Name}}' with a single AvaloniaObject-derived parameter and a non-void return type",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnboundCallback = new(
        DiagnosticIds.GeneratedPropertyUnboundCallback,
        "Callback method cannot be bound",
        "'{0}' does not resolve to an implemented partial callback method; expected signature: '{1}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        DiagnosticIds.GeneratedPropertyNotPartial,
        "Generated Avalonia property requirements are not met",
        "Generated Avalonia properties require {0}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidShape = new(
        DiagnosticIds.GeneratedPropertyInvalidShape,
        "Invalid generated property shape",
        "'{0}' must be a non-static partial instance property with both get and set accessors and no manual implementation",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NameSuffix = new(
        DiagnosticIds.GeneratedPropertyNameSuffix,
        "Property name doubles the 'Property' suffix",
        "'{0}' generates a field named '{0}Property'; consider renaming to '{1}' to avoid the doubled suffix",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Convertible = new(
        DiagnosticIds.GeneratedPropertyConvertible,
        "Manual Avalonia property declaration can be converted to a generated property",
        "'{0}' can be converted to a generated property using [{1}]",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StyledNonPublicSetter = new(
        DiagnosticIds.GeneratedPropertyStyledNonPublicSetter,
        "Non-public setter on a styled property is not read-only",
        "The non-public setter of styled property '{0}' only restricts the CLR accessor; the property can still be set through SetValue, styles and bindings. Use [DirectProperty] with a non-public setter for true read-only behavior.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
