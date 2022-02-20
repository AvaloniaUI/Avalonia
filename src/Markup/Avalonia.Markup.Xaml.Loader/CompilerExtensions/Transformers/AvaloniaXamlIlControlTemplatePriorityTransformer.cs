using System.Collections.Generic;
using System.Linq;
using Avalonia.Data;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    /// <summary>
    /// Transforms property assignments within ControlTemplates to use Style priority where possible.
    /// </summary>
    class AvaloniaXamlIlControlTemplatePriorityTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            var bindingPriorityType = context.GetAvaloniaTypes().BindingPriority;

            // The node is a candidate for transformation if:
            // - It's a property assignment to an Avalonia property
            // - There's a ControlTemplate ancestor
            // - There's just a direct call setter available
            if (node is XamlPropertyAssignmentNode prop &&
                prop.Property is XamlIlAvaloniaProperty avaloniaProperty &&
                context.ParentNodes().Any(IsControlTemplate) &&
                prop.PossibleSetters.Count == 1 &&
                prop.PossibleSetters[0] is XamlDirectCallPropertySetter)
            {
                // Check if there are any setters on the property which accept a binding priority -
                // this filters the candidates down to styled and attached properties. If so, then
                // use this setter with BindingPriority.Style.
                var setPriorityValueSetter =
                    avaloniaProperty.Setters.FirstOrDefault(x => x.Parameters[0] == bindingPriorityType);
                
                if(setPriorityValueSetter != null 
                   && prop.Values.Count == 1
                   && setPriorityValueSetter.Parameters[1].IsAssignableFrom(prop.Values[0].Type.GetClrType()))
                {
                    prop.PossibleSetters = new List<IXamlPropertySetter> { setPriorityValueSetter };
                    prop.Values.Insert(0, new XamlConstantNode(node, bindingPriorityType, (int)BindingPriority.Style));
                    return node;
                }
            }

            return node;
        }

        private static bool IsControlTemplate(IXamlAstNode node)
        {
            return node is AvaloniaXamlIlTargetTypeMetadataNode tt &&
                tt.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate;
        }
    }
}
