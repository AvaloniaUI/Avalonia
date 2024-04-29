using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

class AvaloniaXamlIlDuplicateSettersChecker : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstObjectNode objectNode)
        {
            return node;
        }

        var nodeType = objectNode.Type.GetClrType();
        if (!context.GetAvaloniaTypes().Style.IsAssignableFrom(nodeType) &&
            !context.GetAvaloniaTypes().ControlTheme.IsAssignableFrom(nodeType))
        {
            return node;
        }

        var properties = objectNode.Children
            .OfType<XamlAstObjectNode>()
            .Where(n => n.Type.GetClrType().Name == "Setter")
            .SelectMany(setter =>
                setter.Children.OfType<XamlAstXamlPropertyValueNode>()
                    .Where(c => c.Property.GetClrProperty().Name == "Property"))
            .Select(p => p.Values[0])
            .OfType<XamlAstTextNode>()
            .Select(x => x.Text);
        var index = new HashSet<string>();
        foreach (var property in properties)
        {
            if (!index.Add(property))
            {
                context.ReportDiagnostic(new XamlDiagnostic(
                    AvaloniaXamlDiagnosticCodes.DuplicateSetterError,
                    XamlDiagnosticSeverity.Warning,
                    $"Duplicate setter encountered for property '{property}'", node));
            }
        }

        return node;
    }
}
