using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlResolveByNameMarkupExtensionReplacer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode propertyValueNode)
            {

                IEnumerable<IXamlCustomAttribute> attributes = propertyValueNode.Property.GetClrProperty().CustomAttributes;

                if (propertyValueNode.Property is XamlAstClrProperty referenceNode &&
                    referenceNode.Getter != null)
                {
                    attributes = attributes.Concat(referenceNode.Getter.CustomAttributes);
                }

                foreach (var attribute in attributes)
                {
                    if (attribute.Type.FullName == "Avalonia.Controls.ResolveByNameAttribute")
                    {
                        if (propertyValueNode.Values.Count == 1 &&
                            propertyValueNode.Values.First() is XamlAstTextNode)
                        {
                            if (XamlTransformHelpers.TryConvertMarkupExtension(context, new XamlAstObjectNode(
                                    propertyValueNode.Values[0],
                                    new XamlAstClrTypeReference(propertyValueNode.Values[0],
                                    context.GetAvaloniaTypes().ResolveByNameExtension, true))
                            {
                                Arguments = new System.Collections.Generic.List<IXamlAstValueNode>
                                    {
                                        propertyValueNode.Values[0]
                                    }
                            }, out var extensionNode))
                            {
                                propertyValueNode.Values[0] = extensionNode;
                            }
                        }
                        break;
                    }
                }
            }

            return node;
        }
    }
}
