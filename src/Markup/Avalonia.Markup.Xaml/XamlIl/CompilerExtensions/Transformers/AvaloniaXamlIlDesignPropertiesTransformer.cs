using System.Collections.Generic;
using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlDesignPropertiesTransformer : IXamlIlAstTransformer
    {
        public bool IsDesignMode { get; set; }

        private static Dictionary<string, string> DesignDirectives = new Dictionary<string, string>()
        {
            ["DataContext"] = "DataContext",
            ["DesignWidth"] = "Width", ["DesignHeight"] = "Height", ["PreviewWith"] = "PreviewWith"
        };

        private const string AvaloniaNs = "https://github.com/avaloniaui";
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on)
            {
                for (var c=0; c<on.Children.Count;)
                {
                    var ch = on.Children[c];
                    if (ch is XamlIlAstXmlDirective directive
                        && directive.Namespace == XamlNamespaces.Blend2008
                        && DesignDirectives.TryGetValue(directive.Name, out var mapTo))
                    {
                        if (!IsDesignMode)
                            // Just remove it from AST in non-design mode
                            on.Children.RemoveAt(c);
                        else
                        {
                            // Map to an actual property in `Design` class
                            on.Children[c] = new XamlIlAstXamlPropertyValueNode(ch,
                                new XamlIlAstNamePropertyReference(ch,
                                    new XamlIlAstXmlTypeReference(ch, AvaloniaNs, "Design"),
                                    mapTo, on.Type), directive.Values);
                            c++;
                        }
                    }
                    // Remove all "Design" attached properties in non-design mode
                    else if (
                        !IsDesignMode
                        && ch is XamlIlAstXamlPropertyValueNode pv
                        && pv.Property is XamlIlAstNamePropertyReference pref
                        && pref.DeclaringType is XamlIlAstXmlTypeReference dref
                        && dref.XmlNamespace == AvaloniaNs && dref.Name == "Design"
                    )
                    {
                        on.Children.RemoveAt(c);
                    }
                    else
                        c++;
                }
            }

            return node;
        }
    }
}
