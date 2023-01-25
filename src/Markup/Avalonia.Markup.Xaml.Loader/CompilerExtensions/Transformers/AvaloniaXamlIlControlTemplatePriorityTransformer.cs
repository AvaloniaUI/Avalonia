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
            // - The property has a single value
            if (node is XamlPropertyAssignmentNode prop &&
                prop.Property is XamlIlAvaloniaProperty avaloniaProperty &&
                context.ParentNodes().Any(IsControlTemplate) &&
                prop.Values.Count == 1)
            {
                var priorityValueSetters = new List<IXamlPropertySetter>();

                // Iterate through the possible setters, trying to find a setter on the property 
                // which has a BindingPriority parameter followed by the parameter of the existing
                // setter.
                foreach (var setter in prop.PossibleSetters)
                {
                    var s = avaloniaProperty.Setters.FirstOrDefault(x => 
                        x.Parameters[0] == bindingPriorityType &&
                        x.Parameters[1] == setter.Parameters[0]);
                    if (s != null)
                        priorityValueSetters.Add(s);
                }

                // If any BindingPriority setters were found, use those.
                if (priorityValueSetters.Count > 0)
                {
                    prop.PossibleSetters = priorityValueSetters;
                    prop.Values.Insert(0, new XamlConstantNode(node, bindingPriorityType, (int)BindingPriority.Template));
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
