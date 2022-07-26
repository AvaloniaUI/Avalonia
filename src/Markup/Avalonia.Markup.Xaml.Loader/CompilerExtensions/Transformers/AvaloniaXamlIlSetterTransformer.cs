using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    using XamlParseException = XamlX.XamlParseException;
    class AvaloniaXamlIlSetterTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlAstObjectNode on
                  && on.Type.GetClrType().FullName == "Avalonia.Styling.Setter"))
                return node;

            IXamlType targetType = null;
            IXamlLineInfo lineInfo = null;

            var styleParent = context.ParentNodes()
                .OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                .FirstOrDefault(x => x.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);

            if (styleParent != null)
            {
                targetType = styleParent.TargetType.GetClrType()
                             ?? throw new XamlParseException("Can not resolve parent Style Selector type", node);
                lineInfo = on;
            }
            else
            {
                foreach (var p in context.ParentNodes().OfType<XamlAstObjectNode>())
                {
                    for (var index = 0; index < p.Children.Count; index++)
                    {
                        if (p.Children[index] is XamlAstXmlDirective d &&
                            d.Namespace == XamlNamespaces.Xaml2006 &&
                            d.Name == "SetterTargetType")
                        {
                            p.Children.RemoveAt(index);

                            targetType = context.Configuration.TypeSystem.GetType(((XamlAstTextNode)d.Values[0]).Text);
                            lineInfo = d;

                            break;
                        }
                    }

                    if (targetType != null) break;
                }
            }

            if (targetType == null)
            {
                throw new XamlParseException("Could not determine target type of Setter", node);
            }

            IXamlType propType = null;
            var property = @on.Children.OfType<XamlAstXamlPropertyValueNode>()
                .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Property");
            if (property != null)
            {
                var propertyName = property.Values.OfType<XamlAstTextNode>().FirstOrDefault()?.Text;
                if (propertyName == null)
                    throw new XamlParseException("Setter.Property must be a string", node);

                var avaloniaPropertyNode = XamlIlAvaloniaPropertyHelper.CreateNode(context, propertyName,
                    new XamlAstClrTypeReference(lineInfo, targetType, false), property.Values[0]);
                property.Values = new List<IXamlAstValueNode> {avaloniaPropertyNode};
                propType = avaloniaPropertyNode.AvaloniaPropertyType;
            }
            else
            {
                var propertyPath = on.Children.OfType<XamlAstXamlPropertyValueNode>()
                    .FirstOrDefault(x => x.Property.GetClrProperty().Name == "PropertyPath");
                if (propertyPath == null)
                    throw new XamlX.XamlParseException("Setter without a property or property path is not valid", node);
                if (propertyPath.Values[0] is IXamlIlPropertyPathNode ppn
                    && ppn.PropertyType != null)
                    propType = ppn.PropertyType;
                else
                    throw new XamlX.XamlParseException("Unable to get the property path property type", node);
            }

            var valueProperty = on.Children
                .OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault(p => p.Property.GetClrProperty().Name == "Value");
            if (valueProperty?.Values?.Count == 1 && valueProperty.Values[0] is XamlAstTextNode)
            {
                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, valueProperty.Values[0],
                        propType, out var converted))
                    throw new XamlParseException(
                        $"Unable to convert property value to {propType.GetFqn()}",
                        valueProperty.Values[0]);

                valueProperty.Property = new SetterValueProperty(valueProperty.Property,
                    on.Type.GetClrType(), propType, context.GetAvaloniaTypes());
            }

            return node;
        }

        class SetterValueProperty : XamlAstClrProperty
        {
            public SetterValueProperty(IXamlLineInfo line, IXamlType setterType, IXamlType targetType,
                AvaloniaXamlIlWellKnownTypes types)
                : base(line, "Value", setterType, null)
            {
                Getter = setterType.Methods.First(m => m.Name == "get_Value");
                var method = setterType.Methods.First(m => m.Name == "set_Value");
                Setters.Add(new XamlIlDirectCallPropertySetter(method, types.IBinding));
                Setters.Add(new XamlIlDirectCallPropertySetter(method, types.UnsetValueType));
                Setters.Add(new XamlIlDirectCallPropertySetter(method, targetType));
            }
            
            class XamlIlDirectCallPropertySetter : IXamlPropertySetter, IXamlEmitablePropertySetter<IXamlILEmitter>
            {
                private readonly IXamlMethod _method;
                private readonly IXamlType _type;
                public IXamlType TargetType { get; }
                public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
                public IReadOnlyList<IXamlType> Parameters { get; }
                public void Emit(IXamlILEmitter codegen)
                {
                    if (_type.IsValueType)
                        codegen.Box(_type);
                    codegen.EmitCall(_method, true);
                }

                public XamlIlDirectCallPropertySetter(IXamlMethod method, IXamlType type)
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
