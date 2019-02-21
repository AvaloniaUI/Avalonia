using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.Parsers;
using Avalonia.Utilities;
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

            
            var selectorTypeReference = new XamlIlAstClrTypeReference(selector, selector.TargetType);
            XamlIlAstNamePropertyReference forgedReference;
            
            var parser = new PropertyParser();
            var parsedPropertyName = parser.Parse(new CharacterReader(propertyName.AsSpan()));
            if(parsedPropertyName.owner == null)
                forgedReference = new XamlIlAstNamePropertyReference(property.Values[0], selectorTypeReference,
                    propertyName, selectorTypeReference);
            else
            {
                var xmlOwner = parsedPropertyName.ns;
                if (xmlOwner != null)
                    xmlOwner += ":";
                xmlOwner += parsedPropertyName.owner;
                
                var t = XamlIlTypeReferenceResolver.ResolveType(context, xmlOwner, property.Values[0], true);
                var tref = new XamlIlAstClrTypeReference(property.Values[0], t);
                forgedReference = new XamlIlAstNamePropertyReference(property.Values[0],
                    tref, parsedPropertyName.name, tref);
            }

            var clrProperty =
                ((XamlIlAstClrPropertyReference)new XamlIlPropertyReferenceResolver().Transform(context,
                    forgedReference)).Property;

            property.Values = new List<IXamlIlAstValueNode>
            {
                new XamlIlAvaloniaPropertyNode(property.Values[0], property.Property.GetClrProperty().PropertyType,
                    clrProperty)
            };

            var valueProperty = on.Children
                .OfType<XamlIlAstXamlPropertyValueNode>().FirstOrDefault(p => p.Property.GetClrProperty().Name == "Value");
            if (valueProperty?.Values?.Count == 1 && valueProperty.Values[0] is XamlIlAstTextNode)
            {
                if (!XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, valueProperty.Values[0],
                    clrProperty.PropertyType, out var converted))
                    throw new XamlIlParseException(
                        $"Unable to convert property value to {clrProperty.PropertyType.GetFqn()}",
                        valueProperty.Values[0]);

                valueProperty.Values = new List<IXamlIlAstValueNode>
                {
                    new XamlIlAstRuntimeCastNode(converted, converted,
                        new XamlIlAstClrTypeReference(converted, context.Configuration.WellKnownTypes.Object))
                };
            }

            return node;
        }
        
    }

    class XamlIlAvaloniaPropertyNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlAvaloniaPropertyNode(IXamlIlLineInfo lineInfo, IXamlIlType type, IXamlIlProperty property) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(this, type);
            Property = property;
        }

        public IXamlIlProperty Property { get; set; }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!AvaloniaPropertyDescriptorEmitter.Emit(context, codeGen, Property))
                throw new XamlIlLoadException(Property.Name + " is not an AvaloniaProperty", this);
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
