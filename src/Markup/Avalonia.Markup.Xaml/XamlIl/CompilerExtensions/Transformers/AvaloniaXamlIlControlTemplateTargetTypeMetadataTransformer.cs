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

            if ((tt?.Values.FirstOrDefault() is XamlIlTypeExtensionNode tn))
            {
                targetType = tn.Type;
            }
            else
            {
                var parentScope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault();
                if (parentScope?.Type == AvaloniaXamlIlTargetTypeMetadataNode.ScopeType.Style)
                    targetType = parentScope.TargetType;
                else
                    targetType = new XamlIlAstClrTypeReference(node,
                        context.Configuration.TypeSystem.GetType("Avalonia.Controls.Control"));
            }
                
                

            return new AvaloniaXamlIlTargetTypeMetadataNode(on, targetType,
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeType.ControlTemplate);
        }
    }

    class AvaloniaXamlIlTargetTypeMetadataNode : XamlIlValueWithSideEffectNodeBase
    {
        public IXamlIlAstTypeReference TargetType { get; set; }
        public ScopeType Type { get; }

        public enum ScopeType
        {
            Style,
            ControlTemplate
        }
        
        public AvaloniaXamlIlTargetTypeMetadataNode(IXamlIlAstValueNode value, IXamlIlAstTypeReference targetType,
            ScopeType type)
            : base(value, value)
        {
            TargetType = targetType;
            Type = type;
        }
    }
}
