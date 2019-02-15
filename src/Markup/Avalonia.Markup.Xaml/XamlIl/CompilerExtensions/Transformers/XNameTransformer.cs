using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    public class XNameTransformer : IXamlIlAstTransformer
    {
        
        /// <summary>
        /// Converts x:Name directives to regular Name assignments
        /// </summary>
        /// <returns></returns>
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on)
            {
                foreach (var ch in on.Children)
                {
                    if (ch is XamlIlAstXmlDirective d
                        && d.Namespace == XamlNamespaces.Xaml2006
                        && d.Name == "Name")
                        return new XamlIlAstXamlPropertyValueNode(d,
                            new XamlIlAstNamePropertyReference(d, on.Type, "Name", on.Type),
                            d.Values);
                }
            }

            return node;
            
        }
    }
}
