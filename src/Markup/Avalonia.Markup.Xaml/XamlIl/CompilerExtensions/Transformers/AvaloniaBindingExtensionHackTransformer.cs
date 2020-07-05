using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaBindingExtensionHackTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            // Our code base expects XAML parser to prefer `FooExtension` to `Foo` even with `<Foo>` syntax
            // This is the legacy of Portable.Xaml, so we emulate that behavior here

            if (node is XamlAstXmlTypeReference tref
                && tref.Name == "Binding"
                && tref.XmlNamespace == "https://github.com/avaloniaui")
                tref.IsMarkupExtension = true;
            return node;
        }
    }
}
