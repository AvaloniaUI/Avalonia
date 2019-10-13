using System;
using System.Collections.Generic;
using System.Text;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlTransformSyntheticCompiledBindingMembers : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstNamePropertyReference prop
               && prop.TargetType is XamlIlAstClrTypeReference targetRef
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
                else if (prop.Name == "Source")
                {
                    return new AvaloniaSyntheticCompiledBindingProperty(node,
                        SyntheticCompiledBindingPropertyName.Source);
                }
            }

            return node;
        }
    }

    enum SyntheticCompiledBindingPropertyName
    {
        ElementName,
        RelativeSource,
        Source
    }

    class AvaloniaSyntheticCompiledBindingProperty : XamlIlAstNode, IXamlIlAstPropertyReference
    {
        public SyntheticCompiledBindingPropertyName Name { get; }

        public AvaloniaSyntheticCompiledBindingProperty(
            IXamlIlLineInfo lineInfo,
            SyntheticCompiledBindingPropertyName name)
            : base(lineInfo)
        {
            Name = name;
        }
    }
}
