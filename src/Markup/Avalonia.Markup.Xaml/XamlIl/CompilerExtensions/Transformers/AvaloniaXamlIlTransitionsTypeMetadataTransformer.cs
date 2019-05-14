using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlTransitionsTypeMetadataTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on)
            {
                foreach (var ch in on.Children)
                {
                    if (ch is XamlIlAstXamlPropertyValueNode pn
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
