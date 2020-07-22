using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlTransitionsTypeMetadataTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode on)
            {
                foreach (var ch in on.Children)
                {
                    if (ch is XamlAstXamlPropertyValueNode pn
                        && pn.Property.GetClrProperty().Getter?.ReturnType.Equals(context.GetAvaloniaTypes().Transitions) == true)
                    {
                        for (var c = 0; c < pn.Values.Count; c++)
                        {
                            pn.Values[c] = new AvaloniaXamlIlTargetTypeMetadataNode(pn.Values[c], on.Type,
                                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Transitions);
                        }
                    }
                }
            }
            return node;
        }
    }
}
