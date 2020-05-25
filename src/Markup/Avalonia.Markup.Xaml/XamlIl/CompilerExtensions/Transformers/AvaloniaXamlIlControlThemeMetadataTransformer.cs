using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlControlThemeMetadataTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on
                  && on.Type.GetClrType().FullName == "Avalonia.Controls.ControlTheme"))
                return node;

            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlTargetTypeMetadataNode)
                // Deja vu. I've just been in this place before
                return node;

            var themeFor = on.Children.OfType<XamlIlAstXmlDirective>().FirstOrDefault(ch =>
                                              ch.Name == "DefaultControlThemeFor");

            if (themeFor?.Values.FirstOrDefault() is XamlIlTypeExtensionNode tn)
            {
                on.Children.Remove(themeFor);
                return new AvaloniaXamlIlTargetTypeMetadataNode(on, tn.Value,
                    AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);
            }
            else
            {
                throw new XamlIlParseException("ControlTheme does not have a x:DefaultControlThemeFor attribute", node);
            }
        }
    }
}
