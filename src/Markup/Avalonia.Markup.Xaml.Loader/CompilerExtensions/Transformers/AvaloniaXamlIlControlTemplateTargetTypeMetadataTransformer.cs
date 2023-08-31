using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlControlTemplateTargetTypeMetadataTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlAstObjectNode on
                  && on.Type.GetClrType().FullName == "Avalonia.Markup.Xaml.Templates.ControlTemplate"))
                return node;
            var tt = on.Children.OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault(ch =>
                                              ch.Property.GetClrProperty().Name == "TargetType");

            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlTargetTypeMetadataNode)
                // Deja vu. I've just been in this place before
                return node;

            IXamlAstTypeReference targetType;

            var templatableBaseType = context.Configuration.TypeSystem.GetType("Avalonia.Controls.Control");

            targetType = tt?.Values.FirstOrDefault() switch
            {
                XamlTypeExtensionNode tn => tn.Value,
                XamlAstTextNode textNode => TypeReferenceResolver.ResolveType(context, textNode.Text, false, textNode, true),
                _ when context.ParentNodes()
                    .OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault() is { ScopeType: AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style } parentScope => parentScope.TargetType,
                _ when context.ParentNodes().Skip(1).FirstOrDefault() is XamlAstObjectNode directParentNode
                         && templatableBaseType.IsAssignableFrom(directParentNode.Type.GetClrType()) => directParentNode.Type,
                _ => new XamlAstClrTypeReference(node,
                        templatableBaseType, false)
            };

            return new AvaloniaXamlIlTargetTypeMetadataNode(on, targetType,
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate);
        }
    }

    class AvaloniaXamlIlTargetTypeMetadataNode : XamlValueWithSideEffectNodeBase
    {
        public IXamlAstTypeReference TargetType { get; set; }
        public ScopeTypes ScopeType { get; }

        public enum ScopeTypes
        {
            Style,
            ControlTemplate,
            Transitions
        }

        public AvaloniaXamlIlTargetTypeMetadataNode(IXamlAstValueNode value, IXamlAstTypeReference targetType,
            ScopeTypes type)
            : base(value, value)
        {
            TargetType = targetType;
            ScopeType = type;
        }
    }
}
