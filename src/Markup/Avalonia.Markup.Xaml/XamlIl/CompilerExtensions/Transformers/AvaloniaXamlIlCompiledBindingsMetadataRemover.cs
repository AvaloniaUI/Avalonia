using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlCompiledBindingsMetadataRemover : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            while (true)
            {
                if (node is NestedScopeMetadataNode nestedScope)
                    node = nestedScope.Value;
                else if (node is AvaloniaXamlIlDataContextTypeMetadataNode dataContextType)
                    node = dataContextType.Value;
                else if (node is AvaloniaXamlIlCompileBindingsNode compileBindings)
                    node = compileBindings.Value;
                else
                    return node;
            }
        }
    }
}
