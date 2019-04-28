using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    public class AvaloniaXamlIlTransitionsTypeMetadataTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on)
            {
                foreach (var ch in on.Children)
                {
                    if (ch is XamlIlAstXamlPropertyValueNode pn
                        && pn.Property.GetClrProperty().PropertyType.Equals(context.GetAvaloniaTypes().Transitions))
                    {
                        for (var c = 0; c < pn.Values.Count; c++)
                        {
                            pn.Values[c] = new AvaloniaXamlIlTargetTypeMetadataNode(pn.Values[c], on.Type,
                                AvaloniaXamlIlTargetTypeMetadataNode.ScopeType.Transitions);
                        }
                    }
                }
            }
            return node;
        }
    }
}
