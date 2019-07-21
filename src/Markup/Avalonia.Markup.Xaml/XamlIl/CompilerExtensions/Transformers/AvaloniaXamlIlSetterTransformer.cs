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
    class AvaloniaXamlIlSetterTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on
                  && on.Type.GetClrType().FullName == "Avalonia.Styling.Setter"))
                return node;

            var parent = context.ParentNodes().OfType<XamlIlAstObjectNode>()
                .FirstOrDefault(p => p.Type.GetClrType().FullName == "Avalonia.Styling.Style");
            
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
                var propType = avaloniaPropertyNode.AvaloniaPropertyType;
                if (!XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, valueProperty.Values[0],
                        propType, out var converted))
                    throw new XamlIlParseException(
                        $"Unable to convert property value to {propType.GetFqn()}",
                        valueProperty.Values[0]);

                valueProperty.Property = new SetterValueProperty(valueProperty.Property,
                    on.Type.GetClrType(), propType, context.GetAvaloniaTypes());
            }

            return node;
        }

        class SetterValueProperty : XamlIlAstClrProperty
        {
            public SetterValueProperty(IXamlIlLineInfo line, IXamlIlType setterType, IXamlIlType targetType,
                AvaloniaXamlIlWellKnownTypes types)
                : base(line, "Value", setterType, null)
            {
                Getter = setterType.Methods.First(m => m.Name == "get_Value");
                var method = setterType.Methods.First(m => m.Name == "set_Value");
                Setters.Add(new XamlIlDirectCallPropertySetter(method, types.IBinding));
                Setters.Add(new XamlIlDirectCallPropertySetter(method, types.UnsetValueType));
                Setters.Add(new XamlIlDirectCallPropertySetter(method, targetType));
            }
            
            class XamlIlDirectCallPropertySetter : IXamlIlPropertySetter
            {
                private readonly IXamlIlMethod _method;
                private readonly IXamlIlType _type;
                public IXamlIlType TargetType { get; }
                public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
                public IReadOnlyList<IXamlIlType> Parameters { get; }
                public void Emit(IXamlIlEmitter codegen)
                {
                    if (_type.IsValueType)
                        codegen.Box(_type);
                    codegen.EmitCall(_method, true);
                }

                public XamlIlDirectCallPropertySetter(IXamlIlMethod method, IXamlIlType type)
                {
                    _method = method;
                    _type = type;
                    Parameters = new[] {type};
                    TargetType = method.ThisOrFirstParameter();
                }
            }
        }
    }
}
