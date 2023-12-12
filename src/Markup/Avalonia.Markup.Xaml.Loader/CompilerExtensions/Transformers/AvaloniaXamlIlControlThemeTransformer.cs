using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlControlThemeTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode on ||
                !context.GetAvaloniaTypes().ControlTheme.IsAssignableFrom(on.Type.GetClrType()))
                return node;

            // Check if we've already transformed this node.
            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlTargetTypeMetadataNode)
                return node;

            var targetTypeNode = on.Children.OfType<XamlAstXamlPropertyValueNode>()
                .FirstOrDefault(p => p.Property.GetClrProperty().Name == "TargetType") ??
                throw new XamlTransformException("ControlTheme must have a TargetType.", node);

            IXamlType targetType;

            if (targetTypeNode.Values[0] is XamlTypeExtensionNode extension)
                targetType = extension.Value.GetClrType();
            else if (targetTypeNode.Values[0] is XamlAstTextNode text)
                targetType = TypeReferenceResolver.ResolveType(context, text.Text, false, text, true).GetClrType();
            else
                throw new XamlTransformException("Could not determine TargetType for ControlTheme.", targetTypeNode);

            return new AvaloniaXamlIlTargetTypeMetadataNode(on,
                new XamlAstClrTypeReference(targetTypeNode, targetType, false),
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);
        }
    }
}
