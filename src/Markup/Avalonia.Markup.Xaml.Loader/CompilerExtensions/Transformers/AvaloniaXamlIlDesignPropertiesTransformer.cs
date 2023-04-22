using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlDesignPropertiesTransformer : IXamlAstTransformer
    {
        public bool IsDesignMode { get; set; }

        private static Dictionary<string, string> DesignDirectives = new Dictionary<string, string>()
        {
            ["DataContext"] = "DataContext",
            ["DesignWidth"] = "Width", ["DesignHeight"] = "Height", ["PreviewWith"] = "PreviewWith"
        };

        private const string AvaloniaNs = "https://github.com/avaloniaui";
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode on)
            {
                for (var c=0; c<on.Children.Count;)
                {
                    var ch = on.Children[c];
                    if (ch is XamlAstXmlDirective directive
                        && directive.Namespace == XamlNamespaces.Blend2008
                        && DesignDirectives.TryGetValue(directive.Name, out var mapTo))
                    {
                        if (!IsDesignMode)
                            // Just remove it from AST in non-design mode
                            on.Children.RemoveAt(c);
                        else
                        {
                            // Map to an actual property in `Design` class
                            on.Children[c] = new XamlAstXamlPropertyValueNode(ch,
                                new XamlAstNamePropertyReference(ch,
                                    new XamlAstXmlTypeReference(ch, AvaloniaNs, "Design"),
                                    mapTo, on.Type), directive.Values, true);
                            c++;
                        }
                    }
                    // Remove all "Design" attached properties in non-design mode
                    else if (
                        !IsDesignMode
                        && ch is XamlAstXamlPropertyValueNode pv
                        && pv.Property is XamlAstNamePropertyReference pref
                        && pref.DeclaringType is XamlAstXmlTypeReference dref
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
