using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlCompiledBindingsMetadataRemover : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
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
