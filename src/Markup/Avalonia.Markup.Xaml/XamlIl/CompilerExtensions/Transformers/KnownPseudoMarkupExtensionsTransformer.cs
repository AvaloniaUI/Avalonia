using System.Collections.Generic;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class KnownPseudoMarkupExtensionsTransformer : IXamlIlAstTransformer
    {
        private static readonly List<string> s_knownPseudoExtensions = new List<string>
        {
            "Avalonia.Data.TemplateBinding",
            "Avalonia.Data.MultiBinding",
            "Avalonia.Data.Binding",
        };
        
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXamlPropertyValueNode pn
                && pn.Values.Count == 1
                && s_knownPseudoExtensions.Contains(pn.Values[0].Type.GetClrType().FullName))
                return new XamlIlMarkupExtensionNode(node, pn.Property.GetClrProperty(),
                    null, pn.Values[0], null);
            else
                return node;
        }
    }
}
