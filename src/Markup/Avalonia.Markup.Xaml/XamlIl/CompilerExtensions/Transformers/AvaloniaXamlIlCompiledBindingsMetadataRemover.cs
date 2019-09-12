using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlCompiledBindingsMetadataRemover : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is NestedScopeMetadataNode nestedScope)
                node = nestedScope.Value;

            if (node is AvaloniaXamlIlDataContextTypeMetadataNode dataContextType)
                node = dataContextType.Value;

            return node;
        }
    }
}
