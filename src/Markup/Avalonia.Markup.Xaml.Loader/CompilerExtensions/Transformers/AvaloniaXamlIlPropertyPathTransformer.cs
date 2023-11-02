using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Parsers;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using XamlX.Emit;
using XamlX.IL;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class XamlPropertyPathException : XamlTransformException
    {
        public XamlPropertyPathException(string message, IXamlLineInfo lineInfo, Exception innerException = null)
            : base(message, lineInfo, innerException)
        {
        }
    }

    class AvaloniaXamlIlPropertyPathTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode pv
                && pv.Values.Count == 1
                && pv.Values[0] is XamlAstTextNode text
                && pv.Property.GetClrProperty().Getter?.ReturnType
                    .Equals(context.GetAvaloniaTypes().PropertyPath) == true
            )
            {
                var parentScope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault();
                if(parentScope == null)
                    throw new XamlPropertyPathException("No target type scope found for property path", text);
                if (parentScope.ScopeType != AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style)
                    throw new XamlPropertyPathException("PropertyPath is currently only valid for styles", pv);


                IEnumerable<PropertyPathGrammar.ISyntax> parsed;
                try
                {
                    parsed = PropertyPathGrammar.Parse(text.Text);
                }
                catch (Exception e)
                {
                    throw new XamlPropertyPathException("Unable to parse PropertyPath: " + e.Message, text, innerException: e);
                }

                var elements = new List<IXamlIlPropertyPathElementNode>();
                IXamlType currentType = parentScope.TargetType.GetClrType();
                
                
                var expectProperty = true;
                var expectCast = true;
                var expectTraversal = false;
                var types = context.GetAvaloniaTypes();
                
                IXamlType GetType(string ns, string name)
                {
                    return TypeReferenceResolver.ResolveType(context, $"{ns}:{name}", false,
                        text, true).GetClrType();
                }

                void HandleProperty(string name, string typeNamespace, string typeName)
                {
                    if(!expectProperty || currentType == null)
                        throw new XamlPropertyPathException("Unexpected property node", text);

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
                        throw new XamlPropertyPathException(
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
                            throw new XamlPropertyPathException("Unexpected child traversal .", text);
                        elements.Add(new XamlIlChildTraversalPropertyPathElementNode());
                        expectTraversal = expectCast = false;
                        expectProperty = true;
                    }
                    else if (ge is PropertyPathGrammar.EnsureTypeSyntax ets)
                    {
                        if(!expectCast)
                            throw new XamlPropertyPathException("Unexpected cast node", text);
                        currentType = GetType(ets.TypeNamespace, ets.TypeName);
                        elements.Add(new XamlIlCastPropertyPathElementNode(currentType, true));
                        expectProperty = false;
                        expectCast = expectTraversal = true;
                    }
                    else if (ge is PropertyPathGrammar.CastTypeSyntax cts)
                    {
                        if(!expectCast)
                            throw new XamlPropertyPathException("Unexpected cast node", text);
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
                        throw new XamlPropertyPathException("Unexpected node " + ge, text);
                    
                }
                var propertyPathNode = new XamlIlPropertyPathNode(text, elements, types);
                if (propertyPathNode.Type == null)
                    throw new XamlPropertyPathException("Unexpected end of the property path", text);
                pv.Values[0] = propertyPathNode;
            }

            return node;
        }

        interface IXamlIlPropertyPathElementNode
        {
            void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen);
            IXamlType Type { get; }
        }

        class XamlIlChildTraversalPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            public void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
                => codeGen.EmitCall(
                    context.GetAvaloniaTypes()
                        .PropertyPathBuilder.FindMethod(m => m.Name == "ChildTraversal"));

            public IXamlType Type => null;
        }
        
        class XamlIlAvaloniaPropertyPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            private readonly IXamlField _field;

            public XamlIlAvaloniaPropertyPropertyPathElementNode(IXamlField field, IXamlType propertyType)
            {
                _field = field;
                Type = propertyType;
            }

            public void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
                => codeGen
                    .Ldsfld(_field)
                    .EmitCall(context.GetAvaloniaTypes()
                        .PropertyPathBuilder.FindMethod(m => m.Name == "Property"));

            public IXamlType Type { get; }
        }
        
        class XamlIClrPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            private readonly IXamlProperty _property;

            public XamlIClrPropertyPathElementNode(IXamlProperty property)
            {
                _property = property;
            }

            public void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property);

                codeGen.EmitCall(context.GetAvaloniaTypes()
                    .PropertyPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlType Type => _property.Getter?.ReturnType ?? _property.Setter?.Parameters[0];
        }

        class XamlIlCastPropertyPathElementNode : IXamlIlPropertyPathElementNode
        {
            private readonly IXamlType _type;
            private readonly bool _ensureType;

            public XamlIlCastPropertyPathElementNode(IXamlType type, bool ensureType)
            {
                _type = type;
                _ensureType = ensureType;
            }
            
            public void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                codeGen
                    .Ldtype(_type)
                    .EmitCall(context.GetAvaloniaTypes()
                        .PropertyPathBuilder.FindMethod(m => m.Name == (_ensureType ? "EnsureType" : "Cast")));
            }

            public IXamlType Type => _type;
        }

        class XamlIlPropertyPathNode : XamlAstNode, IXamlIlPropertyPathNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
        {
            private readonly List<IXamlIlPropertyPathElementNode> _elements;
            private readonly AvaloniaXamlIlWellKnownTypes _types;

            public XamlIlPropertyPathNode(IXamlLineInfo lineInfo,
                List<IXamlIlPropertyPathElementNode> elements,
                AvaloniaXamlIlWellKnownTypes types) : base(lineInfo)
            {
                _elements = elements;
                _types = types;
                Type = new XamlAstClrTypeReference(this, types.PropertyPath, false);
            }

            public IXamlAstTypeReference Type { get; }
            public IXamlType PropertyType => _elements.LastOrDefault()?.Type;
            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                codeGen
                    .Newobj(_types.PropertyPathBuilder.FindConstructor());
                foreach(var e in _elements)
                    e.Emit(context, codeGen);
                codeGen.EmitCall(_types.PropertyPathBuilder.FindMethod(m => m.Name == "Build"));
                return XamlILNodeEmitResult.Type(0, _types.PropertyPath);
            }
        }
    }

    interface IXamlIlPropertyPathNode : IXamlAstValueNode
    {
        IXamlType PropertyType { get; }
    }
}
