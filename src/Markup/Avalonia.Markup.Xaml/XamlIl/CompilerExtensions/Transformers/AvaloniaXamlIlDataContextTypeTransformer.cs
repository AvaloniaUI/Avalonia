using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Avalonia.Utilities;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlDataContextTypeTransformer : IXamlIlAstTransformer
    {
        private const string AvaloniaNs = "https://github.com/avaloniaui";
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on)
            {
                AvaloniaXamlIlDataContextTypeMetadataNode calculatedDataContextTypeNode = null;
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
                            return new AvaloniaXamlIlDataContextTypeMetadataNode(on, dataContextType.Value.GetClrType());
                        }
                    }
                    else if (child is XamlIlAstXamlPropertyValueNode pv
                        && pv.Property is XamlIlAstNamePropertyReference pref
                        && pref.Name == "DataContext"
                        && pref.DeclaringType is XamlIlAstXmlTypeReference tref
                        && tref.Name == "StyledElement"
                        && tref.XmlNamespace == AvaloniaNs)
                    {
                        var bindingType = context.GetAvaloniaTypes().IBinding;
                        if (!pv.Values[0].Type.GetClrType().GetAllInterfaces().Contains(bindingType))
                        {
                            calculatedDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on, pv.Values[0].Type.GetClrType());
                        }
                        else if(pv.Values[0].Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension)
                            && pv.Values[0] is XamlIlAstObjectNode binding)
                        {
                            IXamlIlType startType;
                            var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                            if (parentDataContextNode is null)
                            {
                                throw new XamlIlParseException("Cannot parse a compiled binding without an explicit x:DataContextType directive to give a starting data type for bindings.", binding);
                            }

                            startType = parentDataContextNode.DataContextType;

                            var bindingResultType = XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, binding, startType);
                            calculatedDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on, bindingResultType);
                        }
                    }
                }
                if (!(calculatedDataContextTypeNode is null))
                {
                    return calculatedDataContextTypeNode;
                }
            }
            // TODO: Add node for DataTemplate scope.

            return node;
        }
    }

    class AvaloniaXamlIlDataContextTypeMetadataNode : XamlIlValueWithSideEffectNodeBase
    {
        public IXamlIlType DataContextType { get; set; }

        public AvaloniaXamlIlDataContextTypeMetadataNode(IXamlIlAstValueNode value, IXamlIlType targetType)
            : base(value, value)
        {
            DataContextType = targetType;
        }
    }
}
