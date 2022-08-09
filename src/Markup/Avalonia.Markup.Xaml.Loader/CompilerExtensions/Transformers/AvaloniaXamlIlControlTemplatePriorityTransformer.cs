using System.Collections.Generic;
using System.Linq;
using Avalonia.Data;
using XamlX.Ast;
using XamlX.Transform;
using ScopeTypes = Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers.AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    /// <summary>
    /// Transforms property assignments within ControlTemplates to use lower than
    /// LocalValue priority.
    /// </summary>
    class AvaloniaXamlIlControlTemplatePriorityTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlPropertyAssignmentNode p1 && p1.Property.Name == "Background")
            {
            }

            // The node is a candidate for transformation if:
            // - It's a property assignment
            // - The property has a single value
            // - There are ancestor nodes which represent control themes, templates, or styles.
            if (node is XamlPropertyAssignmentNode prop &&
                prop.Values.Count == 1 &&
                GetPriorityFromAncestors(context.ParentNodes()) is var priority &&
                priority != BindingPriority.LocalValue)
            {
                var bindingPriorityType = context.GetAvaloniaTypes().BindingPriority;

                if (prop.Property is XamlIlAvaloniaProperty avaloniaProperty)
                {
                    var priorityValueSetters = new List<IXamlPropertySetter>();

                    // The property is an AvaloniaProperty: iterate through the possible setters,
                    // trying to find a setter on the property which has a BindingPriority parameter
                    // followed by the parameter of the existing setter.
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
            }

            return node;
        }

        private static BindingPriority GetPriorityFromAncestors(IEnumerable<IXamlAstNode> nodes)
        {
            var result = BindingPriority.LocalValue;

            foreach (var node in nodes)
            {
                if (node is AvaloniaXamlIlTargetTypeMetadataNode tt)
                {
                    var priority = (tt.ScopeType & ScopeTypes.InControlTheme) != 0 ?
                        BindingPriority.ControlTheme : BindingPriority.Style;

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
