using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlBindingPathTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode binding && binding.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                IXamlIlType startType;
                var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                if (parentDataContextNode is null)
                {
                    throw new XamlIlParseException("Cannot parse a compiled binding without an explicit x:DataContextType directive to give a starting data type for bindings.", binding);
                }

                startType = parentDataContextNode.DataContextType;

                XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, binding, startType);
            }

            return node;
        }
    }
}
