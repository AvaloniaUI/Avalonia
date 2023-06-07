namespace Avalonia;

internal static class TrimmingMessages
{
    public const string ImplicitTypeConversionSupressWarningMessage = "Implicit conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.";
    public const string ImplicitTypeConversionRequiresUnreferencedCodeMessage = "Implicit conversion methods are required for type conversion.";

    public const string TypeConversionSupressWarningMessage = "Conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.";
    public const string TypeConversionRequiresUnreferencedCodeMessage = "Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.";

    public const string ReflectionBindingRequiresUnreferencedCodeMessage = "BindingExpression and ReflectionBinding heavily use reflection. Consider using CompiledBindings instead.";
    public const string ReflectionBindingSupressWarningMessage = "BindingExpression and ReflectionBinding internal heavily use reflection.";

    public const string CompiledBindingSafeSupressWarningMessage = "CompiledBinding preserves members used in the expression tree.";

    public const string ExpressionNodeRequiresUnreferencedCodeMessage = "ExpressionNode might require unreferenced code.";
    public const string ExpressionSafeSupressWarningMessage = "Typed Expressions preserves members used in the expression tree.";

    public const string SelectorsParseRequiresUnreferencedCodeMessage = "Selectors runtime parser might require unreferenced code. Consider using stronly typed selectors factory with 'new Style(s => s.OfType<Button>())' syntax.";

    public const string PropertyAccessorsRequiresUnreferencedCodeMessage = "PropertyAccessors might require unreferenced code.";
    public const string DataValidationPluginRequiresUnreferencedCodeMessage = "DataValidationPlugin might require unreferenced code.";
    public const string StreamPluginRequiresUnreferencedCodeMessage = "StreamPlugin might require unreferenced code.";

    public const string StyleResourceIncludeRequiresUnreferenceCodeMessage = "StyleInclude and ResourceInclude use AvaloniaXamlLoader.Load which dynamically loads referenced assembly with Avalonia resources. Note, StyleInclude and ResourceInclude defined in XAML are resolved compile time and are safe with trimming and AOT.";
    public const string AvaloniaXamlLoaderRequiresUnreferenceCodeMessage = "AvaloniaXamlLoader.Load(uri, baseUri) dynamically loads referenced assembly with Avalonia resources.";
    public const string XamlTypeResolvedRequiresUnreferenceCodeMessage = "XamlTypeResolver might require unreferenced code.";

    public const string IgnoreNativeAotSupressWarningMessage = "This method is not supported by NativeAOT.";
}
