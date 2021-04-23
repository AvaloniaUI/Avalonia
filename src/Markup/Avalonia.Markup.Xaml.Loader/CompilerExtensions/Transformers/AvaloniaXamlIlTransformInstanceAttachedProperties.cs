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
    class AvaloniaXamlIlTransformInstanceAttachedProperties : IXamlAstTransformer
    {

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstNamePropertyReference prop 
                && prop.TargetType is XamlAstClrTypeReference targetRef 
                && prop.DeclaringType is XamlAstClrTypeReference declaringRef)
            {
                // Target and declared type aren't assignable but both inherit from AvaloniaObject
                var avaloniaObject = context.Configuration.TypeSystem.FindType("Avalonia.AvaloniaObject");
                if (avaloniaObject.IsAssignableFrom(targetRef.Type)
                    && avaloniaObject.IsAssignableFrom(declaringRef.Type)
                    && !declaringRef.Type.IsAssignableFrom(targetRef.Type))
                {
                    // Instance property
                    var clrProp = declaringRef.Type.GetAllProperties().FirstOrDefault(p => p.Name == prop.Name);
                    if (clrProp != null
                        && (clrProp.Getter?.IsStatic == false || clrProp.Setter?.IsStatic == false))
                    {
                        var declaringType = (clrProp.Getter ?? clrProp.Setter)?.DeclaringType;
                        var avaloniaPropertyFieldName = prop.Name + "Property";
                        var avaloniaPropertyField = declaringType.Fields.FirstOrDefault(f => f.IsStatic && f.Name == avaloniaPropertyFieldName);
                        if (avaloniaPropertyField != null)
                        {
                            var avaloniaPropertyType = avaloniaPropertyField.FieldType;
                            while (avaloniaPropertyType != null
                                   && !(avaloniaPropertyType.Namespace == "Avalonia"
                                        && (avaloniaPropertyType.Name == "AvaloniaProperty"
                                            || avaloniaPropertyType.Name == "AvaloniaProperty`1"
                                        )))
                            {
                                // Attached properties are handled by vanilla XamlIl
                                if (avaloniaPropertyType.Name.StartsWith("AttachedProperty"))
                                    return node;
                                
                                avaloniaPropertyType = avaloniaPropertyType.BaseType;
                            }

                            if (avaloniaPropertyType == null)
                                return node;

                            if (avaloniaPropertyType.GenericArguments?.Count > 1)
                                return node;

                            var propertyType = avaloniaPropertyType.GenericArguments?.Count == 1 ?
                                avaloniaPropertyType.GenericArguments[0] :
                                context.Configuration.WellKnownTypes.Object;

                            return new AvaloniaAttachedInstanceProperty(prop, context.Configuration,
                                    declaringType, propertyType, avaloniaPropertyType, avaloniaObject,
                                    avaloniaPropertyField);
                        }

                    }


                }
            }

            return node;
        }

        class AvaloniaAttachedInstanceProperty : XamlAstClrProperty, IXamlIlAvaloniaProperty
        {
            private readonly TransformerConfiguration _config;
            private readonly IXamlType _declaringType;
            private readonly IXamlType _avaloniaPropertyType;
            private readonly IXamlType _avaloniaObject;
            private readonly IXamlField _field;

            public AvaloniaAttachedInstanceProperty(XamlAstNamePropertyReference prop,
                TransformerConfiguration config,
                IXamlType declaringType,
                IXamlType type,
                IXamlType avaloniaPropertyType,
                IXamlType avaloniaObject,
                IXamlField field) : base(prop, prop.Name,
                declaringType, null)
            
            
            {
                _config = config;
                _declaringType = declaringType;
                _avaloniaPropertyType = avaloniaPropertyType;
                
                // XamlIl doesn't support generic methods yet
                if (_avaloniaPropertyType.GenericArguments?.Count > 0)
                    _avaloniaPropertyType = _avaloniaPropertyType.BaseType;
                
                _avaloniaObject = avaloniaObject;
                _field = field;
                PropertyType = type;
                Setters.Add(new SetterMethod(this));
                Getter = new GetterMethod(this);
            }

            public IXamlType PropertyType { get;  }

            public IXamlField AvaloniaProperty => _field;
            
            class SetterMethod : IXamlPropertySetter, IXamlEmitablePropertySetter<IXamlILEmitter>
            {
                private readonly AvaloniaAttachedInstanceProperty _parent;

                public SetterMethod(AvaloniaAttachedInstanceProperty parent)
                {
                    _parent = parent;
                    Parameters = new[] {_parent._avaloniaObject, _parent.PropertyType};
                }

                public IXamlType TargetType => _parent.DeclaringType;
                public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
                public IReadOnlyList<IXamlType> Parameters { get; }
                public void Emit(IXamlILEmitter emitter)
                {
                    var so = _parent._config.WellKnownTypes.Object;
                    var method = _parent._avaloniaObject
                        .FindMethod(m => m.IsPublic && !m.IsStatic && m.Name == "SetValue"
                                         &&
                                         m.Parameters.Count == 3
                                         && m.Parameters[0].Equals(_parent._avaloniaPropertyType)
                                         && m.Parameters[1].Equals(so)
                                         && m.Parameters[2].IsEnum
                        );
                    if (method == null)
                        throw new XamlTypeSystemException(
                            "Unable to find SetValue(AvaloniaProperty, object, BindingPriority) on AvaloniaObject");
                    using (var loc = emitter.LocalsPool.GetLocal(_parent.PropertyType))
                        emitter
                            .Stloc(loc.Local)
                            .Ldsfld(_parent._field)
                            .Ldloc(loc.Local);

                    if(_parent.PropertyType.IsValueType)
                        emitter.Box(_parent.PropertyType);
                    emitter        
                        .Ldc_I4(0)
                        .EmitCall(method);

                }
            }

            class GetterMethod :  IXamlCustomEmitMethod<IXamlILEmitter>
            {
                public GetterMethod(AvaloniaAttachedInstanceProperty parent) 
                {
                    Parent = parent;
                    DeclaringType = parent._declaringType;
                    Name = "AvaloniaObject:GetValue_" + Parent.Name;
                    Parameters = new[] {parent._avaloniaObject};
                }
                public AvaloniaAttachedInstanceProperty Parent { get; }
                public bool IsPublic => true;
                public bool IsStatic => true;
                public string Name { get; protected set; }
                public IXamlType DeclaringType { get; }
                public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments) 
                    => throw new System.NotSupportedException();


                public bool Equals(IXamlMethod other) =>
                    other is GetterMethod m && m.Name == Name && m.DeclaringType.Equals(DeclaringType);
                public IXamlType ReturnType => Parent.PropertyType;
                public IReadOnlyList<IXamlType> Parameters { get; }

                public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => DeclaringType.CustomAttributes;

                public void EmitCall(IXamlILEmitter emitter)
                {
                    var method = Parent._avaloniaObject
                        .FindMethod(m => m.IsPublic && !m.IsStatic && m.Name == "GetValue"
                                         &&
                                         m.Parameters.Count == 1
                                         && m.Parameters[0].Equals(Parent._avaloniaPropertyType));
                    if (method == null)
                        throw new XamlTypeSystemException(
                            "Unable to find T GetValue<T>(AvaloniaProperty<T>) on AvaloniaObject");
                    emitter
                        .Ldsfld(Parent._field)
                        .EmitCall(method);
                    if (Parent.PropertyType.IsValueType)
                        emitter.Unbox_Any(Parent.PropertyType);

                }
            }
        }
    }
}
