using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlBindingPathTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstConstructableObjectNode binding && binding.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                IXamlType startType = null;
                var sourceProperty = binding.Children.OfType<XamlPropertyAssignmentNode>().FirstOrDefault(c => c.Property.Name == "Source");
                if ((sourceProperty?.Values.Count ?? 0) == 1)
                {
                    var sourceValue = sourceProperty.Values[0];
                    switch (sourceValue)
                    {
                        case XamlAstTextNode textNode:
                            startType = textNode.Type?.GetClrType();
                            break;

                        case XamlMarkupExtensionNode extension:
                            startType = extension.Type?.GetClrType();
                            break;

                        case XamlStaticExtensionNode staticExtension:
                            startType = staticExtension.Type?.GetClrType();
                            break;
                    }
                }

                if (startType == null)
                {
                    var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentDataContextNode is null)
                    {
                        throw new XamlX.XamlParseException("Cannot parse a compiled binding without an explicit x:DataType directive to give a starting data type for bindings.", binding);
                    }

                    startType = parentDataContextNode.DataContextType;
                }

                XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, binding, startType);
            }

            return node;
        }
    }
}
