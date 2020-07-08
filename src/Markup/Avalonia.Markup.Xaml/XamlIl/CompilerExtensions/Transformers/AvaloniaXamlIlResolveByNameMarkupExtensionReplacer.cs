using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

#nullable  enable
namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlResolveByNameMarkupExtensionReplacer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlAstXamlPropertyValueNode propertyValueNode)) return node;

            if (!(propertyValueNode.Property is XamlAstClrProperty clrProperty)) return node;

            IEnumerable<IXamlCustomAttribute> attributes = propertyValueNode.Property.GetClrProperty().CustomAttributes;

            if (propertyValueNode.Property is XamlAstClrProperty referenceNode &&
                referenceNode.Getter != null)
            {
                attributes = attributes.Concat(referenceNode.Getter.CustomAttributes);
            }

            if (attributes.All(attribute => attribute.Type.FullName != "Avalonia.Controls.ResolveByNameAttribute"))
                return node;

            if (propertyValueNode.Values.Count != 1 || !(propertyValueNode.Values.First() is XamlAstTextNode))
                return node;

            var newNode = new XamlAstObjectNode(
                propertyValueNode.Values[0],
                new XamlAstClrTypeReference(propertyValueNode.Values[0],
                    context.GetAvaloniaTypes().ResolveByNameExtension, true))
            {
                Arguments = new List<IXamlAstValueNode> { propertyValueNode.Values[0] }
            };
            
            if (XamlTransformHelpers.TryConvertMarkupExtension(context, newNode, out var extensionNode))
            {
                propertyValueNode.Values[0] = extensionNode;
            }

            return node;
        }
    }
}
