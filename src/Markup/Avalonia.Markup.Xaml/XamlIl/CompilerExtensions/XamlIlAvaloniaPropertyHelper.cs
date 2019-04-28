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

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlAvaloniaPropertyHelper
    {
        public static bool Emit(XamlIlEmitContext context, IXamlIlEmitter emitter, IXamlIlProperty property)
        {
            if (property is IXamlIlAvaloniaProperty ap)
            {
                emitter.Ldsfld(ap.AvaloniaProperty);
                return true;
            }
            var type = (property.Getter ?? property.Setter).DeclaringType;
            var name = property.Name + "Property";
            var found = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }

        public static XamlIlAvaloniaPropertyNode CreateNode(XamlIlAstTransformationContext context,
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
                forgedReference = new XamlIlAstNamePropertyReference(lineInfo,
                    tref, parsedPropertyName.name, tref);
            }

            var clrProperty =
                ((XamlIlAstClrPropertyReference)new XamlIlPropertyReferenceResolver().Transform(context,
                    forgedReference)).Property;
            return new XamlIlAvaloniaPropertyNode(lineInfo,
                context.Configuration.TypeSystem.GetType("Avalonia.AvaloniaProperty"),
                clrProperty);
        }
    }
    
    class XamlIlAvaloniaPropertyNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlAvaloniaPropertyNode(IXamlIlLineInfo lineInfo, IXamlIlType type, IXamlIlProperty property) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(this, type, false);
            Property = property;
        }

        public IXamlIlProperty Property { get; }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!XamlIlAvaloniaPropertyHelper.Emit(context, codeGen, Property))
                throw new XamlIlLoadException(Property.Name + " is not an AvaloniaProperty", this);
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }
    }

    interface IXamlIlAvaloniaProperty : IXamlIlProperty
    {
        IXamlIlField AvaloniaProperty { get; }
    }
    
    class XamlIlAvaloniaProperty : IXamlIlAvaloniaProperty
    {
        private readonly IXamlIlProperty _original;

        public IXamlIlField AvaloniaProperty { get; }
        public XamlIlAvaloniaProperty(IXamlIlProperty original, IXamlIlField field)
        {
            _original = original;
            AvaloniaProperty = field;
        }

        public bool Equals(IXamlIlProperty other) =>
            other is XamlIlAvaloniaProperty p && p.AvaloniaProperty.Equals(AvaloniaProperty);

        public string Name => _original.Name;
        public IXamlIlType PropertyType => _original.PropertyType;
        public IXamlIlMethod Setter => _original.Setter;
        public IXamlIlMethod Getter => _original.Getter;
        public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes => _original.CustomAttributes;
    }
}
