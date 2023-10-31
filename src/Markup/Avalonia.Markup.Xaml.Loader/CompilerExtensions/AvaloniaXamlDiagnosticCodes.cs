using System;
using XamlX;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal static class AvaloniaXamlDiagnosticCodes
{
    public const string Unknown = "AVLN0000";

    // XML/XAML parsing errors 1000-1999.
    public const string ParseError = "AVLN1000";
    public const string InvalidXAML = "AVLN1001";

    // XAML transform errors 2000-2999.
    public const string TransformError = "AVLN2000";
    public const string DuplicateXClass = "AVLN2002";
    public const string LegacyResmScheme = "AVLN2003";
    public const string TypeSystemError = "AVLN2004";
    public const string AvaloniaIntrinsicsError = "AVLN2005";
    public const string CompiledBindingsError = "AVLN2100";
    public const string CompiledBindingsPathError = "AVLN2101";
    public const string DataContextResolvingError = "AVLN2102";
    public const string StyleTransformError = "AVLN2200";
    public const string SelectorsTransformError = "AVLN2201";
    public const string PropertyPathError = "AVLN2202";
    public const string DuplicateSetterError = "AVLN2203";

    // XAML emit errors 3000-3999.
    public const string EmitError = "AVLN3000";
    public const string Loader = "AVLN3001";

    // Generator specific errors 4000-4999.
    public const string NameGeneratorError = "AVLN4001";

    // Reserved for warnings 5000-9998
    public const string Obsolete = "AVLN5001";

    internal static string XamlXDiagnosticCodeToAvalonia(XamlXDiagnosticCode xamlXDiagnosticCode)
    {
        return xamlXDiagnosticCode switch
        {
            XamlXDiagnosticCode.Unknown => Unknown,
            XamlXDiagnosticCode.ParseError => ParseError,
            XamlXDiagnosticCode.TransformError => TransformError,
            XamlXDiagnosticCode.EmitError => EmitError,
            XamlXDiagnosticCode.TypeSystemError => TypeSystemError,
            XamlXDiagnosticCode.Obsolete => Obsolete,
            _ => throw new ArgumentOutOfRangeException(nameof(xamlXDiagnosticCode), xamlXDiagnosticCode, null)
        };
    }
}
