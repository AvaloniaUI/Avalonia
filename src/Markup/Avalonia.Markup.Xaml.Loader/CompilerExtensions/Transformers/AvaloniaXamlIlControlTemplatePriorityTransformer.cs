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

            if (node is XamlPropertyAssignmentNode prop &&
                prop.Property is XamlIlAvaloniaProperty avaloniaProperty &&
                context.ParentNodes().Any(IsControlTemplate))
            {
                // If there are any setters which accept a binding priority, then add the Style
                // binding priority as a value.
                var setPriorityValueSetter =
                    avaloniaProperty.Setters.FirstOrDefault(x => x.Parameters[0] == bindingPriorityType);
                
                if(setPriorityValueSetter != null 
                   && prop.Values.Count == 1
                   && setPriorityValueSetter.Parameters[1].IsAssignableFrom(prop.Values[0].Type.GetClrType()))
                {
                    prop.PossibleSetters = new List<IXamlPropertySetter> { setPriorityValueSetter };
                    prop.Values.Insert(0, new XamlConstantNode(node, bindingPriorityType, (int)BindingPriority.Style));
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
