using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlControlTemplateTargetTypeMetadataTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on
                  && on.Type.GetClrType().FullName == "Avalonia.Markup.Xaml.Templates.ControlTemplate"))
                return node;
            var tt = on.Children.OfType<XamlIlAstXamlPropertyValueNode>().FirstOrDefault(ch =>
                                              ch.Property.GetClrProperty().Name == "TargetType");

            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlTargetTypeMetadataNode)
                // Deja vu. I've just been in this place before
                return node;

            IXamlIlAstTypeReference targetType;

            var templatableBaseType = context.Configuration.TypeSystem.GetType("Avalonia.Controls.Control");
            
            if ((tt?.Values.FirstOrDefault() is XamlIlTypeExtensionNode tn))
            {
                targetType = tn.Type;
            }
            else
            {
                var parentScope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault();
                if (parentScope?.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style)
                    targetType = parentScope.TargetType;
                else if (context.ParentNodes().Skip(1).FirstOrDefault() is XamlIlAstObjectNode directParentNode
                         && templatableBaseType.IsAssignableFrom(directParentNode.Type.GetClrType()))
                    targetType = directParentNode.Type;
                else
                    targetType = new XamlIlAstClrTypeReference(node,
                        templatableBaseType, false);
            }
                
                

            return new AvaloniaXamlIlTargetTypeMetadataNode(on, targetType,
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate);
        }
    }

    class AvaloniaXamlIlTargetTypeMetadataNode : XamlIlValueWithSideEffectNodeBase
    {
        public IXamlIlAstTypeReference TargetType { get; set; }
        public ScopeTypes ScopeType { get; }

        public enum ScopeTypes
        {
            Style,
            ControlTemplate,
            Transitions
        }
        
        public AvaloniaXamlIlTargetTypeMetadataNode(IXamlIlAstValueNode value, IXamlIlAstTypeReference targetType,
            ScopeTypes type)
            : base(value, value)
        {
            TargetType = targetType;
            ScopeType = type;
        }
    }
}
