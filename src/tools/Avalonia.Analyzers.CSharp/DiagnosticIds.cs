namespace Avalonia.Analyzers;

public static class DiagnosticIds
{
    public const string OnPropertyChangedOverride = "AVA2001";
    public const string Bitmap = "AVA2002";

    // Avalonia property diagnostics.
    public const string AvaloniaPropertyAssociatedProperty = "AVP0001";
    public const string AvaloniaPropertyInappropriateAssignment = "AVP1000";
    public const string AvaloniaPropertyInappropriateRegistration = "AVP1001";
    public const string AvaloniaPropertyOwnedByGenericType = "AVP1002";
    public const string AvaloniaPropertyOwnerDoesNotMatchOuterType = "AVP1010";
    public const string AvaloniaPropertyUnexpectedAccess = "AVP1011";
    public const string AvaloniaPropertySettingOwnStyledValue = "AVP1012";
    public const string AvaloniaPropertySuperfluousAddOwnerCall = "AVP1013";
    public const string AvaloniaPropertyDuplicateName = "AVP1020";
    public const string AvaloniaPropertyAmbiguousName = "AVP1021";
    public const string AvaloniaPropertyNameMismatch = "AVP1022";
    public const string AvaloniaPropertyAccessorSideEffects = "AVP1030";
    public const string AvaloniaPropertyMissingAccessor = "AVP1031";
    public const string AvaloniaPropertyInconsistentAccessibility = "AVP1032";
    public const string AvaloniaPropertyTypeMismatch = "AVP1040";

    // Property source generator diagnostics.
    public const string GeneratedPropertyOwnerNotAvaloniaObject = "AVP2001";
    public const string GeneratedPropertyConflictingArguments = "AVP2002";
    public const string GeneratedPropertyIncompatibleConstant = "AVP2003";
    public const string GeneratedPropertyAddOwnerSourceMissing = "AVP2004";
    public const string GeneratedPropertyInvalidAttachedShape = "AVP2005";
    public const string GeneratedPropertyUnboundCallback = "AVP2006";
    public const string GeneratedPropertyNotPartial = "AVP2007";
    public const string GeneratedPropertyInvalidShape = "AVP2008";
    public const string GeneratedPropertyNameSuffix = "AVP2100";
    public const string GeneratedPropertyStyledNonPublicSetter = "AVP2101";
}
