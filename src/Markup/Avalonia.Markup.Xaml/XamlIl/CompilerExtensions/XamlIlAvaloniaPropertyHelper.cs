using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Avalonia.Markup.Xaml.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Avalonia.Utilities;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlAvaloniaPropertyHelper
    {
        public static bool Emit(XamlIlEmitContext context, IXamlIlEmitter emitter, XamlIlAstClrProperty property)
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
        
        public static bool Emit(XamlIlEmitContext context, IXamlIlEmitter emitter, IXamlIlProperty property)
        {
            var type = (property.Getter ?? property.Setter).DeclaringType;
            var name = property.Name + "Property";
            var found = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }

        public static IXamlIlAvaloniaPropertyNode CreateNode(XamlIlAstTransformationContext context,
            string propertyName, IXamlIlAstTypeReference selectorTypeReference, IXamlIlLineInfo lineInfo)
        {
            XamlIlAstNamePropertyReference forgedReference;
            
            var parser = new PropertyParser();
            
            var parsedPropertyName = parser.Parse(new CharacterReader(propertyName.AsSpan()));
            if(parsedPropertyName.owner == null)
                forgedReference = new XamlIlAstNamePropertyReference(lineInfo, selectorTypeReference,
                    propertyName, selectorTypeReference);
            else
            {
                var xmlOwner = parsedPropertyName.ns;
                if (xmlOwner != null)
                    xmlOwner += ":";
                xmlOwner += parsedPropertyName.owner;
                
                var tref = XamlIlTypeReferenceResolver.ResolveType(context, xmlOwner, false, lineInfo, true);

                var propertyFieldName = parsedPropertyName.name + "Property";
                var found = tref.Type.GetAllFields()
                    .FirstOrDefault(f => f.IsStatic && f.IsPublic && f.Name == propertyFieldName);
                if (found == null)
                    throw new XamlIlParseException(
                        $"Unable to find {propertyFieldName} field on type {tref.Type.GetFullName()}", lineInfo);
                return new XamlIlAvaloniaPropertyFieldNode(context.GetAvaloniaTypes(), lineInfo, found);
            }

            var clrProperty =
                ((XamlIlAstClrProperty)new XamlIlPropertyReferenceResolver().Transform(context,
                    forgedReference));
            return new XamlIlAvaloniaPropertyNode(lineInfo,
                context.Configuration.TypeSystem.GetType("Avalonia.AvaloniaProperty"),
                clrProperty);
        }
    }

    interface IXamlIlAvaloniaPropertyNode : IXamlIlAstValueNode
    {
        IXamlIlType AvaloniaPropertyType { get; }
    }
    
    class XamlIlAvaloniaPropertyNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode, IXamlIlAvaloniaPropertyNode
    {
        public XamlIlAvaloniaPropertyNode(IXamlIlLineInfo lineInfo, IXamlIlType type, XamlIlAstClrProperty property) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(this, type, false);
            Property = property;
            AvaloniaPropertyType = Property.Getter?.ReturnType
                                   ?? Property.Setters.First().Parameters[0];
        }

        public XamlIlAstClrProperty Property { get; }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!XamlIlAvaloniaPropertyHelper.Emit(context, codeGen, Property))
                throw new XamlIlLoadException(Property.Name + " is not an AvaloniaProperty", this);
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }

        public IXamlIlType AvaloniaPropertyType { get; }
    }

    class XamlIlAvaloniaPropertyFieldNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode, IXamlIlAvaloniaPropertyNode
    {
        private readonly IXamlIlField _field;

        public XamlIlAvaloniaPropertyFieldNode(AvaloniaXamlIlWellKnownTypes types,
            IXamlIlLineInfo lineInfo, IXamlIlField field) : base(lineInfo)
        {
            _field = field;
            var avaloniaPropertyType = field.FieldType;
            while (avaloniaPropertyType != null)
            {
                if (avaloniaPropertyType.GenericTypeDefinition?.Equals(types.AvaloniaPropertyT) == true)
                {
                    AvaloniaPropertyType = avaloniaPropertyType.GenericArguments[0];
                    return;
                }

                avaloniaPropertyType = avaloniaPropertyType.BaseType;
            }

            throw new XamlIlParseException(
                $"{field.Name}'s type {field.FieldType} doesn't inherit from AvaloniaProperty<T>, make sure to use typed properties",
                lineInfo);

        }
        
        

        public IXamlIlAstTypeReference Type => new XamlIlAstClrTypeReference(this, _field.FieldType, false);
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen.Ldsfld(_field);
            return XamlIlNodeEmitResult.Type(0, _field.FieldType);
        }

        public IXamlIlType AvaloniaPropertyType { get; }
    }

    interface IXamlIlAvaloniaProperty
    {
        IXamlIlField AvaloniaProperty { get; }
    }
    
    class XamlIlAvaloniaProperty : XamlIlAstClrProperty, IXamlIlAvaloniaProperty
    {
        public IXamlIlField AvaloniaProperty { get; }
        public XamlIlAvaloniaProperty(XamlIlAstClrProperty original, IXamlIlField field,
            AvaloniaXamlIlWellKnownTypes types)
            :base(original, original.Name, original.DeclaringType, original.Getter, original.Setters)
        {
            AvaloniaProperty = field;
            CustomAttributes = original.CustomAttributes;
            if (!original.CustomAttributes.Any(ca => ca.Type.Equals(types.AssignBindingAttribute)))
                Setters.Insert(0, new BindingSetter(types, original.DeclaringType, field));
            
            Setters.Insert(0, new UnsetValueSetter(types, original.DeclaringType, field));
        }

        abstract class AvaloniaPropertyCustomSetter : IXamlIlPropertySetter
        {
            protected AvaloniaXamlIlWellKnownTypes Types;
            protected IXamlIlField AvaloniaProperty;

            public AvaloniaPropertyCustomSetter(AvaloniaXamlIlWellKnownTypes types,
                IXamlIlType declaringType,
                IXamlIlField avaloniaProperty)
            {
                Types = types;
                AvaloniaProperty = avaloniaProperty;
                TargetType = declaringType;
            }

            public IXamlIlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters
            {
                AllowXNull = false
            };

            public IReadOnlyList<IXamlIlType> Parameters { get; set; }
            public abstract void Emit(IXamlIlEmitter codegen);
        }

        class BindingSetter : AvaloniaPropertyCustomSetter
        {
            public BindingSetter(AvaloniaXamlIlWellKnownTypes types,
                IXamlIlType declaringType,
                IXamlIlField avaloniaProperty) : base(types, declaringType, avaloniaProperty)
            {
                Parameters = new[] {types.IBinding};
            }

            public override void Emit(IXamlIlEmitter emitter)
            {
                using (var bloc = emitter.LocalsPool.GetLocal(Types.IBinding))
                    emitter
                        .Stloc(bloc.Local)
                        .Ldsfld(AvaloniaProperty)
                        .Ldloc(bloc.Local)
                        // TODO: provide anchor?
                        .Ldnull();
                emitter.EmitCall(Types.AvaloniaObjectBindMethod, true);
            }
        }

        class UnsetValueSetter : AvaloniaPropertyCustomSetter
        {
            public UnsetValueSetter(AvaloniaXamlIlWellKnownTypes types, IXamlIlType declaringType, IXamlIlField avaloniaProperty) 
                : base(types, declaringType, avaloniaProperty)
            {
                Parameters = new[] {types.UnsetValueType};
            }

            public override void Emit(IXamlIlEmitter codegen)
            {
                var unsetValue = Types.AvaloniaProperty.Fields.First(f => f.Name == "UnsetValue");
                codegen
                    // Ignore the instance and load one from the static field to avoid extra local variable
                    .Pop()
                    .Ldsfld(AvaloniaProperty)
                    .Ldsfld(unsetValue)
                    .Ldc_I4(0)
                    .EmitCall(Types.AvaloniaObjectSetValueMethod, true);
            }
        }
    }
}
