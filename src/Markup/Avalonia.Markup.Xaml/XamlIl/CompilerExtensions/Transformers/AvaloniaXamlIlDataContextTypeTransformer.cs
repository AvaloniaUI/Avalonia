using System;
using System.Collections.Generic;
using System.Text;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlDataContextTypeTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on)
            {
                foreach (var child in on.Children)
                {
                    if (child is XamlIlAstXmlDirective directive)
                    {
                        if (directive.Namespace == XamlNamespaces.Xaml2006
                            && directive.Name == "DataContextType"
                            && directive.Values.Count == 1
                            && directive.Values[0] is XamlIlTypeExtensionNode dataContextType)
                        {
                            on.Children.Remove(child);
                            return new AvaloniaXamlIlDataContextTypeMetadataNode(on, dataContextType.Value);
                        }
                    }
                }
            }

            return node;
        }
    }

    class AvaloniaXamlIlDataContextTypeMetadataNode : XamlIlValueWithSideEffectNodeBase
    {
        public IXamlIlAstTypeReference DataContextType { get; set; }

        public AvaloniaXamlIlDataContextTypeMetadataNode(IXamlIlAstValueNode value, IXamlIlAstTypeReference targetType)
            : base(value, value)
        {
            DataContextType = targetType;
        }
    }
}
