using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaBindingExtensionTransformer : IXamlIlAstTransformer
    {
        public bool CompileBindingsByDefault { get; set; }

        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlCompileBindingsNode)
            {
                return node;
            }

            if (node is XamlIlAstObjectNode obj)
            {
                foreach (var item in obj.Children)
                {
                    if (item is XamlIlAstXmlDirective directive)
                    {
                        if (directive.Namespace == XamlNamespaces.Xaml2006
                            && directive.Name == "CompileBindings"
                            && directive.Values.Count == 1)
                        {
                            if (!(directive.Values[0] is XamlIlAstTextNode text
                                && bool.TryParse(text.Text, out var compileBindings)))
                            {
                                throw new XamlIlParseException("The value of x:CompileBindings must be a literal boolean value.", directive.Values[0]);
                            }

                            obj.Children.Remove(directive);

                            return new AvaloniaXamlIlCompileBindingsNode(obj, compileBindings);
                        }
                    }
                }
            }

            // Our code base expects XAML parser to prefer `FooExtension` to `Foo` even with `<Foo>` syntax
            // This is the legacy of Portable.Xaml, so we emulate that behavior here

            if (node is XamlIlAstXmlTypeReference tref
                && tref.Name == "Binding"
                && tref.XmlNamespace == "https://github.com/avaloniaui")
            {
                tref.IsMarkupExtension = true;

                var compileBindings = context.ParentNodes()
                    .OfType<AvaloniaXamlIlCompileBindingsNode>()
                    .FirstOrDefault()
                    ?.CompileBindings ?? CompileBindingsByDefault;

                tref.Name = compileBindings ? "CompiledBinding" : "ReflectionBinding";
            }
            return node;
        }
    }

    internal class AvaloniaXamlIlCompileBindingsNode : XamlIlValueWithSideEffectNodeBase
    {
        public AvaloniaXamlIlCompileBindingsNode(IXamlIlAstValueNode value, bool compileBindings)
            : base(value, value)
        {
            CompileBindings = compileBindings;
        }

        public bool CompileBindings { get; }
    }
}
