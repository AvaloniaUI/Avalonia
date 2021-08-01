using System;
using System.Collections.Generic;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlTransformSyntheticCompiledBindingMembers : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstNamePropertyReference prop
               && prop.TargetType is XamlAstClrTypeReference targetRef
               && targetRef.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                if (prop.Name == "ElementName")
                {
                    return new AvaloniaSyntheticCompiledBindingProperty(node,
                        SyntheticCompiledBindingPropertyName.ElementName);
                }
                else if (prop.Name == "RelativeSource")
                {
                    return new AvaloniaSyntheticCompiledBindingProperty(node,
                        SyntheticCompiledBindingPropertyName.RelativeSource);
                }
            }

            return node;
        }
    }

    enum SyntheticCompiledBindingPropertyName
    {
        ElementName,
        RelativeSource
    }

    class AvaloniaSyntheticCompiledBindingProperty : XamlAstNode, IXamlAstPropertyReference
    {
        public SyntheticCompiledBindingPropertyName Name { get; }

        public AvaloniaSyntheticCompiledBindingProperty(
            IXamlLineInfo lineInfo,
            SyntheticCompiledBindingPropertyName name)
            : base(lineInfo)
        {
            Name = name;
        }
    }
}
