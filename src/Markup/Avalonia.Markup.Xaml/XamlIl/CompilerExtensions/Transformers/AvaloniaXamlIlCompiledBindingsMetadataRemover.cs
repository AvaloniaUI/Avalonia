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
                return nestedScope.Value;

            if (node is AvaloniaXamlIlDataContextTypeMetadataNode dataContextType)
                return dataContextType.Value;

            return node;
        }
    }
}
