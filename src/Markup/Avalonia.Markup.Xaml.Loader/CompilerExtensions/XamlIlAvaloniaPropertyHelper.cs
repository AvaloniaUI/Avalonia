using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Avalonia.Utilities;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using XamlX.Emit;
using XamlX.IL;

using XamlIlEmitContext = XamlX.Emit.XamlEmitContext<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>;
using IXamlIlAstEmitableNode = XamlX.Emit.IXamlAstEmitableNode<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlAvaloniaPropertyHelper
    {
        public static bool EmitProvideValueTarget(XamlIlEmitContext context, IXamlILEmitter emitter,
            XamlAstClrProperty property)
        {
            if (Emit(context, emitter, property))
                return true;
            var foundClr = property.DeclaringType.Properties.FirstOrDefault(p => p.Name == property.Name);
            if (foundClr == null)
                return false;
            context
                .Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                .Emit(context, emitter, foundClr);
            return true;
        }
        
        public static bool Emit(XamlIlEmitContext context, IXamlILEmitter emitter, XamlAstClrProperty property)
        {
            if (property is IXamlIlAvaloniaProperty ap)
            {
                emitter.Ldsfld(ap.AvaloniaProperty);
                return true;
            }
            var type = property.DeclaringType;
            var name = property.Name + "Property";
            var found = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }
        
        public static bool Emit(XamlIlEmitContext context, IXamlILEmitter emitter, IXamlProperty property)
        {
            var type = (property.Getter ?? property.Setter).DeclaringType;
            var name = property.Name + "Property";
            var found = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }

        public static IXamlIlAvaloniaPropertyNode CreateNode(AstTransformationContext context,
            string propertyName, IXamlAstTypeReference selectorTypeReference, IXamlLineInfo lineInfo)
        {
            XamlAstNamePropertyReference forgedReference;

            var parsedPropertyName = PropertyParser.Parse(new CharacterReader(propertyName.AsSpan()));
            if(parsedPropertyName.owner == null)
                forgedReference = new XamlAstNamePropertyReference(lineInfo, selectorTypeReference,
                    propertyName, selectorTypeReference);
            else
            {
                var xmlOwner = parsedPropertyName.ns;
                if (xmlOwner != null)
                    xmlOwner += ":";
                xmlOwner += parsedPropertyName.owner;
                
                var tref = TypeReferenceResolver.ResolveType(context, xmlOwner, false, lineInfo, true);

                var propertyFieldName = parsedPropertyName.name + "Property";
                var found = tref.Type.GetAllFields()
                    .FirstOrDefault(f => f.IsStatic && f.IsPublic && f.Name == propertyFieldName);
                if (found == null)
                    throw new XamlX.XamlTransformException(
                        $"Unable to find {propertyFieldName} field on type {tref.Type.GetFullName()}", lineInfo);
                return new XamlIlAvaloniaPropertyFieldNode(context.GetAvaloniaTypes(), lineInfo, found);
            }

            var clrProperty =
                ((XamlAstClrProperty)new PropertyReferenceResolver().Transform(context,
                    forgedReference));
            return new XamlIlAvaloniaPropertyNode(lineInfo,
                context.Configuration.TypeSystem.GetType("Avalonia.AvaloniaProperty"),
                clrProperty);
        }

        public static IXamlType GetAvaloniaPropertyType(IXamlField field,
            AvaloniaXamlIlWellKnownTypes types, IXamlLineInfo lineInfo)
        {
            var avaloniaPropertyType = field.FieldType;
            while (avaloniaPropertyType != null)
            {
                if (avaloniaPropertyType.GenericTypeDefinition?.Equals(types.AvaloniaPropertyT) == true)
                {
                    return avaloniaPropertyType.GenericArguments[0];
                }

                avaloniaPropertyType = avaloniaPropertyType.BaseType;
            }

            throw new XamlX.XamlTransformException(
                $"{field.Name}'s type {field.FieldType} doesn't inherit from  AvaloniaProperty<T>, make sure to use typed properties",
                lineInfo);

        }
    }

    interface IXamlIlAvaloniaPropertyNode : IXamlAstValueNode
    {
        IXamlType AvaloniaPropertyType { get; }
    }
    
    class XamlIlAvaloniaPropertyNode : XamlAstNode, IXamlAstValueNode, IXamlIlAstEmitableNode, IXamlIlAvaloniaPropertyNode
    {
        public XamlIlAvaloniaPropertyNode(IXamlLineInfo lineInfo, IXamlType type, XamlAstClrProperty property) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(this, type, false);
            Property = property;
            AvaloniaPropertyType = Property.Getter?.ReturnType
                                   ?? Property.Setters.First().Parameters[0];
        }

        public XamlAstClrProperty Property { get; }

        public IXamlAstTypeReference Type { get; }
        public XamlILNodeEmitResult Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!XamlIlAvaloniaPropertyHelper.Emit(context, codeGen, Property))
                throw new XamlX.XamlLoadException(Property.Name + " is not an AvaloniaProperty", this);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }

        public IXamlType AvaloniaPropertyType { get; }
    }

    class XamlIlAvaloniaPropertyFieldNode : XamlAstNode, IXamlAstValueNode, IXamlIlAstEmitableNode, IXamlIlAvaloniaPropertyNode
    {
        private readonly IXamlField _field;

        public XamlIlAvaloniaPropertyFieldNode(AvaloniaXamlIlWellKnownTypes types,
            IXamlLineInfo lineInfo, IXamlField field) : base(lineInfo)
        {
            _field = field;
            AvaloniaPropertyType = XamlIlAvaloniaPropertyHelper.GetAvaloniaPropertyType(field,
                types, lineInfo);
        }
        
        

        public IXamlAstTypeReference Type => new XamlAstClrTypeReference(this, _field.FieldType, false);
        public XamlILNodeEmitResult Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
        {
            codeGen.Ldsfld(_field);
            return XamlILNodeEmitResult.Type(0, _field.FieldType);
        }

        public IXamlType AvaloniaPropertyType { get; }
    }

    interface IXamlIlAvaloniaProperty
    {
        IXamlField AvaloniaProperty { get; }
    }
    
    class XamlIlAvaloniaProperty : XamlAstClrProperty, IXamlIlAvaloniaProperty
    {
        public IXamlField AvaloniaProperty { get; }
        public XamlIlAvaloniaProperty(XamlAstClrProperty original, IXamlField field,
            AvaloniaXamlIlWellKnownTypes types)
            :base(original, original.Name, original.DeclaringType, original.Getter, original.Setters)
        {
            var assignBinding = original.CustomAttributes.Any(ca => ca.Type.Equals(types.AssignBindingAttribute));

            AvaloniaProperty = field;
            CustomAttributes = original.CustomAttributes;
            if (!assignBinding)
                Setters.Insert(0, new BindingSetter(types, original.DeclaringType, field));

            // Styled and attached properties can be set with a BindingPriority when they're
            // assigned in a ControlTemplate.
            if (field.FieldType.GenericTypeDefinition == types.StyledPropertyT ||
                field.FieldType.GenericTypeDefinition == types.AvaloniaAttachedPropertyT)
            {
                var propertyType = field.FieldType.GenericArguments[0];
                Setters.Insert(0, new SetValueWithPrioritySetter(types, original.DeclaringType, field, propertyType));
                if (!assignBinding)
                    Setters.Insert(1, new BindingWithPrioritySetter(types, original.DeclaringType, field));
            }

            Setters.Insert(0, new UnsetValueSetter(types, original.DeclaringType, field));
        }

        abstract class AvaloniaPropertyCustomSetter : IXamlILOptimizedEmitablePropertySetter, IEquatable<AvaloniaPropertyCustomSetter>
        {
            protected readonly AvaloniaXamlIlWellKnownTypes Types;
            protected readonly IXamlField AvaloniaProperty;

            protected AvaloniaPropertyCustomSetter(
                AvaloniaXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField avaloniaProperty,
                bool allowNull)
            {
                Types = types;
                AvaloniaProperty = avaloniaProperty;
                TargetType = declaringType;
                BinderParameters = new PropertySetterBinderParameters
                {
                    AllowXNull = allowNull,
                    AllowRuntimeNull = allowNull
                };
            }

            public IXamlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; }

            public IReadOnlyList<IXamlType> Parameters { get; set; }

            public abstract void Emit(IXamlILEmitter emitter);

            public abstract void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments);

            public bool Equals(AvaloniaPropertyCustomSetter other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                return GetType() == other.GetType() && AvaloniaProperty.Equals(other.AvaloniaProperty);
            }

            public override bool Equals(object obj)
                => Equals(obj as AvaloniaPropertyCustomSetter);

            public override int GetHashCode() 
                => AvaloniaProperty.GetHashCode();
        }

        class BindingSetter : AvaloniaPropertyCustomSetter
        {
            public BindingSetter(AvaloniaXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField avaloniaProperty) : base(types, declaringType, avaloniaProperty, false)
            {
                Parameters = new[] { types.IBinding };
            }

            public override void Emit(IXamlILEmitter emitter)
            {
                using (var bloc = emitter.LocalsPool.GetLocal(Types.IBinding))
                    emitter
                        .Stloc(bloc.Local)
                        .Ldsfld(AvaloniaProperty)
                        .Ldloc(bloc.Local);
                EmitAnchorAndBind(emitter);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.Ldsfld(AvaloniaProperty);
                context.Emit(arguments[0], emitter, Parameters[0]);
                EmitAnchorAndBind(emitter);
            }

            private void EmitAnchorAndBind(IXamlILEmitter emitter)
            {
                emitter
                    .Ldnull() // TODO: provide anchor?
                    .EmitCall(Types.AvaloniaObjectBindMethod, true);
            }
        }

        class BindingWithPrioritySetter : AvaloniaPropertyCustomSetter
        {
            public BindingWithPrioritySetter(AvaloniaXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField avaloniaProperty) : base(types, declaringType, avaloniaProperty, false)
            {
                Parameters = new[] { types.BindingPriority, types.IBinding };
            }

            public override void Emit(IXamlILEmitter emitter)
            {
                using (var bloc = emitter.LocalsPool.GetLocal(Types.IBinding))
                    emitter
                        .Stloc(bloc.Local)
                        .Pop() // ignore priority
                        .Ldsfld(AvaloniaProperty)
                        .Ldloc(bloc.Local);
                EmitAnchorAndBind(emitter);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.Ldsfld(AvaloniaProperty);
                context.Emit(arguments[1], emitter, Parameters[1]);
                EmitAnchorAndBind(emitter);
            }

            private void EmitAnchorAndBind(IXamlILEmitter emitter)
            {
                emitter
                    .Ldnull() // TODO: provide anchor?
                    .EmitCall(Types.AvaloniaObjectBindMethod, true);
            }
        }

        class SetValueWithPrioritySetter : AvaloniaPropertyCustomSetter
        {
            public SetValueWithPrioritySetter(AvaloniaXamlIlWellKnownTypes types, IXamlType declaringType, IXamlField avaloniaProperty,
                IXamlType propertyType)
                : base(types, declaringType, avaloniaProperty, propertyType.AcceptsNull())
            {
                Parameters = new[] { types.BindingPriority, propertyType };
            }

            public override void Emit(IXamlILEmitter emitter)
            {
                /*
                  Current stack:
                   - object
                   - binding priority
                   - value
                */

                using (var valueLocal = emitter.LocalsPool.GetLocal(Parameters[1]))
                using (var priorityLocal = emitter.LocalsPool.GetLocal(Types.Int))
                    emitter
                        .Stloc(valueLocal.Local)
                        .Stloc(priorityLocal.Local)
                        .Ldsfld(AvaloniaProperty)
                        .Ldloc(valueLocal.Local)
                        .Ldloc(priorityLocal.Local);

                EmitSetStyledPropertyValue(emitter);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.Ldsfld(AvaloniaProperty);
                context.Emit(arguments[1], emitter, Parameters[1]);
                context.Emit(arguments[0], emitter, Parameters[0]);
                EmitSetStyledPropertyValue(emitter);
            }

            private void EmitSetStyledPropertyValue(IXamlILEmitter emitter)
            {
                var method = Types.AvaloniaObjectSetStyledPropertyValue.MakeGenericMethod(new[] { Parameters[1] });
                emitter.EmitCall(method, true);
            }
        }

        class UnsetValueSetter : AvaloniaPropertyCustomSetter
        {
            public UnsetValueSetter(AvaloniaXamlIlWellKnownTypes types, IXamlType declaringType, IXamlField avaloniaProperty) 
                : base(types, declaringType, avaloniaProperty, false)
            {
                Parameters = new[] { types.UnsetValueType };
            }

            public override void Emit(IXamlILEmitter codegen)
            {
                codegen.Pop();
                EmitSetValue(codegen);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                EmitSetValue(emitter);
            }

            private void EmitSetValue(IXamlILEmitter emitter)
            {
                // Ignore the instance and load one from the static field to avoid extra local variable
                var unsetValue = Types.AvaloniaProperty.Fields.First(f => f.Name == "UnsetValue");

                emitter
                    .Ldsfld(AvaloniaProperty)
                    .Ldsfld(unsetValue)
                    .Ldc_I4(0)
                    .EmitCall(Types.AvaloniaObjectSetValueMethod, true);
            }
        }
    }
}
