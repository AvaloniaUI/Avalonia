using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class XNameTransformer : IXamlAstTransformer
    {
        /// <summary>
        /// Converts x:Name directives to regular Name assignments
        /// </summary>
        /// <returns></returns>
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode on)
            {
                for (var c =0; c< on.Children.Count;c++)
                {
                    var ch = on.Children[c];
                    if (ch is XamlAstXmlDirective d
                        && d.Namespace == XamlNamespaces.Xaml2006
                        && d.Name == "Name")


                        on.Children[c] = new XamlAstXamlPropertyValueNode(d,
                            new XamlAstNamePropertyReference(d, on.Type, "Name", on.Type),
                            d.Values, true);
                }
            }

            return node;
            
        }
    }
}
