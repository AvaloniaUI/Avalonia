using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlStyleValidatorTransformer : IXamlAstTransformer
{
    // See https://github.com/AvaloniaUI/Avalonia/issues/7461
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (!(node is XamlAstObjectNode on
              && context.GetAvaloniaTypes().IStyle.IsAssignableFrom(on.Type.GetClrType())))
            return node;

        if (context.ParentNodes().FirstOrDefault() is XamlAstXamlPropertyValueNode propertyValueNode
            && propertyValueNode.Property.GetClrProperty() is { } clrProperty
            && clrProperty.Name == "MergedDictionaries"
            && clrProperty.DeclaringType == context.GetAvaloniaTypes().ResourceDictionary)
        {
            var nodeName = on.Type.GetClrType().Name;
            context.ReportDiagnostic(new XamlDiagnostic(
                AvaloniaXamlDiagnosticCodes.StyleInMergedDictionaries,
                XamlDiagnosticSeverity.Warning,
                // Keep it single line, as MSBuild splits multiline warnings into two warnings.
                $"Including {nodeName} as part of MergedDictionaries will ignore any nested styles." +
                $"Instead, you can add {nodeName} to the Styles collection on the same control or application.",
                node));
        }
        
        return node;
    }
}
