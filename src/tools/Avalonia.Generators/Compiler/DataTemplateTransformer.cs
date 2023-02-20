using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Generators.Compiler;

internal class DataTemplateTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlAstObjectNode objectNode &&
            objectNode.Type is XamlAstXmlTypeReference typeReference &&
            (typeReference.Name == "DataTemplate" ||
             typeReference.Name == "ControlTemplate"))
            objectNode.Children.Clear();
        return node;
    }
}