using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaBindingExtensionHackTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            // Our code base expects XAML parser to prefer `FooExtension` to `Foo` even with `<Foo>` syntax
            // This is the legacy of Portable.Xaml, so we emulate that behavior here

            if (node is XamlIlAstXmlTypeReference tref
                && tref.Name == "Binding"
                && tref.XmlNamespace == "https://github.com/avaloniaui")
                tref.IsMarkupExtension = true;
            return node;
        }
    }
}
