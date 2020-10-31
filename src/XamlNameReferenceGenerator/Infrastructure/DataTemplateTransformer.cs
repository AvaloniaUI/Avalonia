using XamlX.Ast;
using XamlX.Transform;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal class DataTemplateTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode objectNode &&
                objectNode.Type is XamlAstXmlTypeReference typeReference &&
                typeReference.Name == "DataTemplate") 
                objectNode.Children.Clear();
            return node;
        }
    }
}