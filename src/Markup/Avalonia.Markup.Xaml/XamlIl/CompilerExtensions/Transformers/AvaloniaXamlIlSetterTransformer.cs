using System;
using System.Collections.Generic;
using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    public class AvaloniaXamlIlSetterTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on
                  && on.Type.GetClrType().FullName == "Avalonia.Styling.Setter"))
                return node;
            var parent = context.ParentNodes().OfType<XamlIlAstObjectNode>()
                .FirstOrDefault(x => x.Type.GetClrType().FullName == "Avalonia.Styling.Style");
            if (parent == null)
                throw new XamlIlParseException(
                    "Avalonia.Styling.Setter is only valid inside Avalonia.Styling.Style", node);
            var selectorProperty = parent.Children.OfType<XamlIlAstXamlPropertyValueNode>()
                .FirstOrDefault(p => p.Property.GetClrProperty().Name == "Selector");
            if (selectorProperty == null)
                throw new XamlIlParseException(
                    "Can not find parent Style Selector", node);
            var selector = selectorProperty.Values.FirstOrDefault() as XamlIlSelectorNode;
            if (selector?.TargetType == null)
                throw new XamlIlParseException(
                    "Can not resolve parent Style Selector type", node);


            var property = @on.Children.OfType<XamlIlAstXamlPropertyValueNode>()
                .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Property");
            if (property == null)
                throw new XamlIlParseException("Setter without a property is not valid", node);

            var propertyName = property.Values.OfType<XamlIlAstTextNode>().FirstOrDefault()?.Text;
            if (propertyName == null)
                throw new XamlIlParseException("Setter.Property must be a string", node);


            var avaloniaPropertyNode = XamlIlAvaloniaPropertyHelper.CreateNode(context, propertyName,
                new XamlIlAstClrTypeReference(selector, selector.TargetType, false), property.Values[0]);
            property.Values = new List<IXamlIlAstValueNode>
            {
                avaloniaPropertyNode
            };

            var valueProperty = on.Children
                .OfType<XamlIlAstXamlPropertyValueNode>().FirstOrDefault(p => p.Property.GetClrProperty().Name == "Value");
            if (valueProperty?.Values?.Count == 1 && valueProperty.Values[0] is XamlIlAstTextNode)
            {
                if (!XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, valueProperty.Values[0],
                        avaloniaPropertyNode.Property.PropertyType, out var converted))
                    throw new XamlIlParseException(
                        $"Unable to convert property value to {avaloniaPropertyNode.Property.PropertyType.GetFqn()}",
                        valueProperty.Values[0]);

                valueProperty.Values = new List<IXamlIlAstValueNode>
                {
                    new XamlIlAstRuntimeCastNode(converted, converted,
                        new XamlIlAstClrTypeReference(converted, context.Configuration.WellKnownTypes.Object, false))
                };
            }

            return node;
        }
        
    }
}
