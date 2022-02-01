using System.Linq;
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
                if (avaloniaProperty.Setters.Any(x => x.Parameters[0] == bindingPriorityType))
                {
                    prop.Values.Insert(0, new XamlConstantNode(node, bindingPriorityType, 1));
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
