using System;
using System.Linq;
using Avalonia.Data;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;
using ScopeTypes = Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers.AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes;

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
            var scope = ScopeTypes.ControlTemplate;

            if ((tt?.Values.FirstOrDefault() is XamlTypeExtensionNode tn))
            {
                targetType = tn.Value;
            }
            else
            {
                var parentScope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault();
                if (parentScope != null && (parentScope.ScopeType & ScopeTypes.Style) != 0)
                {
                    targetType = parentScope.TargetType;
                    scope |= parentScope.ScopeType & ScopeTypes.InControlTheme;
                }
                else if (context.ParentNodes().Skip(1).FirstOrDefault() is XamlAstObjectNode directParentNode
                         && templatableBaseType.IsAssignableFrom(directParentNode.Type.GetClrType()))
                    targetType = directParentNode.Type;
                else
                    targetType = new XamlAstClrTypeReference(node,
                        templatableBaseType, false);
            }

            return new AvaloniaXamlIlTargetTypeMetadataNode(on, targetType, scope);
        }
    }

    class AvaloniaXamlIlTargetTypeMetadataNode : XamlValueWithSideEffectNodeBase
    {
        public IXamlAstTypeReference TargetType { get; set; }
        public ScopeTypes ScopeType { get; }

        [Flags]
        public enum ScopeTypes
        {
            Transitions = 0x00,
            Style = 0x01,
            ControlTemplate = 0x02,
            InControlTheme = 0x10,
            ControlTheme = Style | InControlTheme,
            ControlThemeTemplate = ControlTemplate | InControlTheme,
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
