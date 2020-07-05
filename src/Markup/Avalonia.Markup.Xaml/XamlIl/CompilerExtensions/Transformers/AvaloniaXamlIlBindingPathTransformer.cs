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
                IXamlType startType;
                var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                if (parentDataContextNode is null)
                {
                    throw new XamlX.XamlParseException("Cannot parse a compiled binding without an explicit x:DataType directive to give a starting data type for bindings.", binding);
                }

                startType = parentDataContextNode.DataContextType;

                XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, binding, startType);
            }

            return node;
        }
    }
}
