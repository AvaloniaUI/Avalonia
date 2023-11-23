using System;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal static class AvaloniaXamlDiagnosticCodes
{
    public const string Unknown = "AVLN9999";

    // XML/XAML parsing errors 1000-1999.
    public const string ParseError = "AVLN1000";
    public const string InvalidXAML = "AVLN1001";

    // XAML transform errors 2000-2999.
    public const string TransformError = "AVLN2000";
    public const string DuplicateXClass = "AVLN2002";
    public const string TypeSystemError = "AVLN2003";
    public const string AvaloniaIntrinsicsError = "AVLN2005";
    public const string BindingsError = "AVLN2100";
    public const string DataContextResolvingError = "AVLN2101";
    public const string StyleTransformError = "AVLN2200";
    public const string SelectorsTransformError = "AVLN2201";
    public const string PropertyPathError = "AVLN2202";
    public const string DuplicateSetterError = "AVLN2203";
    public const string StyleInMergedDictionaries = "AVLN2204";

    // XAML emit errors 3000-3999.
    public const string EmitError = "AVLN3000";
    public const string XamlLoaderUnreachable = "AVLN3001";

    // Generator specific errors 4000-4999.
    public const string NameGeneratorError = "AVLN4001";

    // Reserved 5000-9998
    public const string Obsolete = "AVLN5001";

    internal static string XamlXDiagnosticCodeToAvalonia(object xamlException)
    {
        return xamlException switch
        {
            XamlXWellKnownDiagnosticCodes wellKnownDiagnosticCodes => wellKnownDiagnosticCodes switch
            {
                XamlXWellKnownDiagnosticCodes.Obsolete => Obsolete,
                _ => throw new ArgumentOutOfRangeException()
            },

            XamlDataContextException => DataContextResolvingError,
            XamlBindingsTransformException => BindingsError,
            XamlPropertyPathException => PropertyPathError,
            XamlStyleTransformException => StyleTransformError,
            XamlSelectorsTransformException => SelectorsTransformError,

            XamlTransformException => TransformError,
            XamlTypeSystemException => TypeSystemError,
            XamlLoadException => EmitError,
            XamlParseException => ParseError,
            
            _ => Unknown
        };
    }
}
