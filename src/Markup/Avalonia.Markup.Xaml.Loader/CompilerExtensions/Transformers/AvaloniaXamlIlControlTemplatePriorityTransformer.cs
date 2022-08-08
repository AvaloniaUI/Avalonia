using System.Collections.Generic;
using System.Linq;
using Avalonia.Data;
using XamlX.Ast;
using XamlX.Transform;
using ScopeTypes = Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers.AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes;

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
            // - The property has a single value
            // - The priority from the parent nodes is not LocalValue
            if (node is XamlPropertyAssignmentNode prop &&
                prop.Property is XamlIlAvaloniaProperty avaloniaProperty &&
                prop.Values.Count == 1 &&
                GetPriority(context.ParentNodes()) is var priority &&
                priority != BindingPriority.LocalValue)
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
                    prop.Values.Insert(0, new XamlConstantNode(node, bindingPriorityType, (int)priority));
                }
            }

            return node;
        }

        private static BindingPriority GetPriority(IEnumerable<IXamlAstNode> nodes)
        {
            var result = BindingPriority.LocalValue;

            foreach (var node in nodes)
            {
                if (node is AvaloniaXamlIlTargetTypeMetadataNode tt)
                {
                    var priority = tt.ScopeType switch
                    {
                        ScopeTypes.ControlTheme => BindingPriority.ControlTheme,
                        ScopeTypes.Style => BindingPriority.Style,
                        ScopeTypes.ControlTemplate => BindingPriority.Style,
                        _ => BindingPriority.LocalValue,
                    };

                    if (priority > result)
                        result = priority;
                }
            }

            return result;
        }

        private static bool IsControlTemplate(IXamlAstNode node)
        {
            return node is AvaloniaXamlIlTargetTypeMetadataNode tt &&
                tt.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate;
        }
    }
}
