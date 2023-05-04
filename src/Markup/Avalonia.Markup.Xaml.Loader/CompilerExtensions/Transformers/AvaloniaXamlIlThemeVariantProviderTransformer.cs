using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlThemeVariantProviderTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        var type = context.GetAvaloniaTypes().IThemeVariantProvider;
        if (!(node is XamlAstObjectNode on
              && type.IsAssignableFrom(on.Type.GetClrType())))
            return node;

        var keyDirective = on.Children.FirstOrDefault(n => n is XamlAstXmlDirective d
                                                           && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                           d.Name == "Key") as XamlAstXmlDirective;
        if (keyDirective is null)
            return node;

        var keyProp = type.Properties.First(p => p.Name == "Key");
        on.Children.Add(new XamlAstXamlPropertyValueNode(keyDirective,
            new XamlAstClrProperty(keyDirective, keyProp, context.Configuration),
            keyDirective.Values, true));

        return node;
    }
}
