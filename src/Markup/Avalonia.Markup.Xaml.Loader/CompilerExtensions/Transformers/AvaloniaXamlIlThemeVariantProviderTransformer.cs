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
        var avTypes = context.GetAvaloniaTypes();
        var type = avTypes.IThemeVariantProvider;
        if (!(node is XamlAstObjectNode on
              && type.IsAssignableFrom(on.Type.GetClrType())))
            return node;

        var keyDirective = on.Children.FirstOrDefault(n => n is XamlAstXmlDirective d
                                                           && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                           d.Name == "Key") as XamlAstXmlDirective;
        if (keyDirective is null)
            return node;

        var themeDictionariesColl = avTypes.IDictionaryT.MakeGenericType(avTypes.ThemeVariant, avTypes.IThemeVariantProvider);
        if (context.ParentNodes().FirstOrDefault() is not XamlAstXamlPropertyValueNode propertyValueNode
            || !themeDictionariesColl.IsAssignableFrom(propertyValueNode.Property.GetClrProperty().Getter.ReturnType))
        {
            return node;
        }
        
        var keyProp = type.Properties.First(p => p.Name == "Key");
        on.Children.Add(new XamlAstXamlPropertyValueNode(keyDirective,
            new XamlAstClrProperty(keyDirective, keyProp, context.Configuration),
            keyDirective.Values, true));

        return node;
    }
}
