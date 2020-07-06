using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlResolveByNameMarkupExtensionReplacer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode propertyValueNode)
            {
                foreach(var attribute in propertyValueNode.Property.GetClrProperty().CustomAttributes)
                {
                    if (attribute.Type.FullName == "Avalonia.Controls.ResolveByNameAttribute")
                    {
                        if (propertyValueNode.Values.Count == 1 &&
                            propertyValueNode.Values.First() is XamlAstTextNode)
                        {
                            //propertyValueNode.Values[0] = new XamlMarkupExtensionNode();
                        }
                        break;
                    }
                }
            }

            return node;
        }
    }
}
