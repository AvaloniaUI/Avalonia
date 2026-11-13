using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    static class XamlDirectiveHelpers
    {
        public static XamlAstXmlDirective? FindXamlDirective(IEnumerable<IXamlAstNode> children, string name)
        {
            return children.OfType<XamlAstXmlDirective>()
                .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == name);
        }

        /// <summary>
        /// Parses a directive with a single literal boolean value, such as x:Shared="False".
        /// </summary>
        public static bool TryParseBooleanDirective(XamlAstXmlDirective directive, out bool value)
        {
            value = default;
            return directive.Values.Count == 1
                   && directive.Values[0] is XamlAstTextNode text
                   && bool.TryParse(text.Text, out value);
        }
    }
}
