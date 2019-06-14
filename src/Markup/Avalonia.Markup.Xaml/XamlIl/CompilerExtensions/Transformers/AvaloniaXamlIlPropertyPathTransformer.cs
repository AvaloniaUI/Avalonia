using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Parsers;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlPropertyPathTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXamlPropertyValueNode pv
                && pv.Values.Count == 1
                && pv.Values[0] is XamlIlAstTextNode text
                && pv.Property.GetClrProperty().Getter.ReturnType.Equals(context.GetAvaloniaTypes().PropertyPath)
            )
            {
                var parentScope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault();
                if(parentScope == null)
                    throw new XamlIlParseException("No target type scope found for property path", text);
                if (parentScope.ScopeType != AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style)
                    throw new XamlIlParseException("PropertyPath is currently only valid for styles", pv);


                IEnumerable<PropertyPathGrammar.ISyntax> parsed;
                try
                {
                    parsed = PropertyPathGrammar.Parse(text.Text);
                }
                catch (Exception e)
                {
                    throw new XamlIlParseException("Unable to parse PropertyPath: " + e.Message, text);
                }

                var elements = new List<IXamlIlPropertyPathElementNode>();
                IXamlIlType currentType = parentScope.TargetType.GetClrType();
                
                
                var expectProperty = true;
                var expectCast = true;
                var expectTraversal = false;
                var types = context.GetAvaloniaTypes();
                
                IXamlIlType GetType(string ns, string name)
                {
                    return XamlIlTypeReferenceResolver.ResolveType(context, $"{ns}:{name}", false,
                        text, true).GetClrType();
                }

                void HandleProperty(string name, string typeNamespace, string typeName)
                {
                    if(!expectProperty || currentType == null)
                        throw new XamlIlParseException("Unexpected property node", text);

                    var propertySearchType =
                        typeName != null ? GetType(typeNamespace, typeName) : currentType;

                    IXamlIlPropertyPathElementNode prop = null;
                    var avaloniaPropertyFieldName = name + "Property";
                    var avaloniaPropertyField = propertySearchType.GetAllFields().FirstOrDefault(f =>
                        f.IsStatic && f.IsPublic && f.Name == avaloniaPropertyFieldName);
                    if (avaloniaPropertyField != null)
                    {
                        prop = new XamlIlAvaloniaPropertyPropertyPathElementNode(avaloniaPropertyField,
                            XamlIlAvaloniaPropertyHelper.GetAvaloniaPropertyType(avaloniaPropertyField, types, text));
                    }
                    else
                    {
                        var clrProperty = propertySearchType.GetAllProperties().FirstOrDefault(p => p.Name == name);
                        prop = new XamlIClrPropertyPathElementNode(clrProperty);
                    }

                    if (prop == null)
                        throw new XamlIlParseException(
                            $"Unable to resolve property {name} on type {propertySearchType.GetFqn()}",
                            text);
                    
                    currentType = prop.Type;
                    elements.Add(prop);
                    expectProperty = false;
                    expectTraversal = expectCast = true;
                }
                
                foreach (var ge in parsed)
                {
                    if (ge is PropertyPathGrammar.ChildTraversalSyntax)
                    {
                        if (!expectTraversal)
                            throw new XamlIlParseException("Unexpected child traversal .", text);
                        elements.Add(new XamlIlChildTraversalPropertyPathElementNode());
                        expectTraversal = expectCast = false;
                        expectProperty = true;
                    }
                    else if (ge is PropertyPathGrammar.EnsureTypeSyntax ets)
                    {
                        if(!expectCast)
                            throw new XamlIlParseException("Unexpected cast node", text);
                        currentType = GetType(ets.TypeNamespace, ets.TypeName);
                        elements.Add(new XamlIlCastPropertyPathElementNode(currentType, true));
                        expectProperty = false;
                        expectCast = expectTraversal = true;
                    }
                    else if (ge is PropertyPathGrammar.CastTypeSyntax cts)
                    {
                        if(!expectCast)
                            throw new XamlIlParseException("Unexpected cast node", text);
                        //TODO: Check if cast can be done
                        currentType = GetType(cts.TypeNamespace, cts.TypeName);
                        elements.Add(new XamlIlCastPropertyPathElementNode(currentType, false));
                        expectProperty = false;
                        expectCast = expectTraversal = true;
                    }
                    else if (ge is PropertyPathGrammar.PropertySyntax ps)
                    {
                        HandleProperty(ps.Name, null, null);
                    }
                    else if (ge is PropertyPathGrammar.TypeQualifiedPropertySyntax tqps)
                    {
                        HandleProperty(tqps.Name, tqps.TypeNamespace, tqps.TypeName);
                    }
                    else
                        throw new XamlIlParseException("Unexpected node " + ge, text);
                    
                }
                var propertyPathNode = new XamlIlPropertyPathNode(text, elements, types);
                if (propertyPathNode.Type == null)
                    throw new XamlIlParseException("Unexpected end of the property path", text);
                pv.Values[0] = propertyPathNode;
            }

            return node;
        }

        interface IXamlIlPropertyPathElementNode
        {
            void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen);
            IXamlIlType Type { get; }
        }

        class XamlIlChildTraversalPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
                => codeGen.EmitCall(
                    context.GetAvaloniaTypes()
                        .PropertyPathBuilder.FindMethod(m => m.Name == "ChildTraversal"));

            public IXamlIlType Type => null;
        }
        
        class XamlIlAvaloniaPropertyPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            private readonly IXamlIlField _field;

            public XamlIlAvaloniaPropertyPropertyPathElementNode(IXamlIlField field, IXamlIlType propertyType)
            {
                _field = field;
                Type = propertyType;
            }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
                => codeGen
                    .Ldsfld(_field)
                    .EmitCall(context.GetAvaloniaTypes()
                        .PropertyPathBuilder.FindMethod(m => m.Name == "Property"));

            public IXamlIlType Type { get; }
        }
        
        class XamlIClrPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            private readonly IXamlIlProperty _property;

            public XamlIClrPropertyPathElementNode(IXamlIlProperty property)
            {
                _property = property;
            }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property);

                codeGen.EmitCall(context.GetAvaloniaTypes()
                    .PropertyPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlIlType Type => _property.Getter?.ReturnType ?? _property.Setter?.Parameters[0];
        }

        class XamlIlCastPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            private readonly IXamlIlType _type;
            private readonly bool _ensureType;

            public XamlIlCastPropertyPathElementNode(IXamlIlType type, bool ensureType)
            {
                _type = type;
                _ensureType = ensureType;
            }
            
            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen
                    .Ldtype(_type)
                    .EmitCall(context.GetAvaloniaTypes()
                        .PropertyPathBuilder.FindMethod(m => m.Name == (_ensureType ? "EnsureType" : "Cast")));
            }

            public IXamlIlType Type => _type;
        }

        class XamlIlPropertyPathNode : XamlIlAstNode, IXamlIlPropertyPathNode, IXamlIlAstEmitableNode
        {
            private readonly List<IXamlIlPropertyPathElementNode> _elements;
            private readonly AvaloniaXamlIlWellKnownTypes _types;

            public XamlIlPropertyPathNode(IXamlIlLineInfo lineInfo,
                List<IXamlIlPropertyPathElementNode> elements,
                AvaloniaXamlIlWellKnownTypes types) : base(lineInfo)
            {
                _elements = elements;
                _types = types;
                Type = new XamlIlAstClrTypeReference(this, types.PropertyPath, false);
            }

            public IXamlIlAstTypeReference Type { get; }
            public IXamlIlType PropertyType => _elements.LastOrDefault()?.Type;
            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen
                    .Newobj(_types.PropertyPathBuilder.FindConstructor());
                foreach(var e in _elements)
                    e.Emit(context, codeGen);
                codeGen.EmitCall(_types.PropertyPathBuilder.FindMethod(m => m.Name == "Build"));
                return XamlIlNodeEmitResult.Type(0, _types.PropertyPath);
            }
        }
    }

    interface IXamlIlPropertyPathNode : IXamlIlAstValueNode
    {
        IXamlIlType PropertyType { get; }
    }
}
