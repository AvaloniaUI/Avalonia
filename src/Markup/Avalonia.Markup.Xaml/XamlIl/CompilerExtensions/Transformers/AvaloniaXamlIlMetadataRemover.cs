using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlMetadataRemover : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is AvaloniaXamlIlTargetTypeMetadataNode targetTypeMetadata)
                return targetTypeMetadata.Value;

            if (node is AvaloniaXamlIlDataContextTypeMetadataNode dataContextTypeMetadata)
                return dataContextTypeMetadata.Value;

            if (node is NestedScopeMetadataNode nestedScope)
                return nestedScope.Value;

            return node;
        }
    }
}
