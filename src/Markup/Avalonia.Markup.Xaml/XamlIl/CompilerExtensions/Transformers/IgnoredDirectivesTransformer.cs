using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class IgnoredDirectivesTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode no)
            {
                foreach (var d in no.Children.OfType<XamlIlAstXmlDirective>().ToList())
                {
                    if (d.Namespace == XamlNamespaces.Xaml2006)
                    {
                        if (d.Name == "Precompile" || d.Name == "Class")
                            no.Children.Remove(d);
                    }
                }
            }

            return node;
        }
    }
}
