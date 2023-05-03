using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Generators.Compiler;

internal class NameDirectiveTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstObjectNode objectNode)
            return node;

        for (var index = 0; index < objectNode.Children.Count; index++)
        {
            var child = objectNode.Children[index];
            if (child is XamlAstXmlDirective directive &&
                directive.Namespace == XamlNamespaces.Xaml2006 &&
                directive.Name == "Name")
                objectNode.Children[index] = new XamlAstXamlPropertyValueNode(
                    directive,
                    new XamlAstNamePropertyReference(directive, objectNode.Type, "Name", objectNode.Type),
                    directive.Values, true);
        }

        return node;
    }
}
