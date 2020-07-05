using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlMetadataRemover : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is AvaloniaXamlIlTargetTypeMetadataNode md)
                return md.Value;

            return node;
        }
    }
}
