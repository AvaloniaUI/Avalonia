using System;
using XamlX;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal static class AvaloniaXamlDiagnosticCodes
{
    public const string ParseError = "AVLN1000";
    public const string InvalidXAML = "AVLN1001";

    public const string TransformError = "AVLN2000";
    public const string Obsolete = "AVLN2001";
    public const string DuplicateXClass = "AVLN2002";
    public const string LegacyResmScheme = "AVLN2003";
    public const string TypeSystemError = "AVLN2004";

    public const string EmitError = "AVLN3000";
    public const string Loader = "AVLN3001";

    public const string NameGenerator = "AVLN4000";

    public const string Unknown = "AVLN9999";

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
