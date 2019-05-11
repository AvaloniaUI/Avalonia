using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Markup.Parsers;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlSelectorTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on && on.Type.GetClrType().FullName == "Avalonia.Styling.Style"))
                return node;

            var pn = on.Children.OfType<XamlIlAstXamlPropertyValueNode>()
                .FirstOrDefault(p => p.Property.GetClrProperty().Name == "Selector");

            if (pn == null)
                return node;

            if (pn.Values.Count != 1)
                throw new XamlIlParseException("Selector property should should have exactly one value", node);
            
            if (pn.Values[0] is XamlIlSelectorNode)
                //Deja vu. I've just been in this place before
                return node;
            
            if (!(pn.Values[0] is XamlIlAstTextNode tn))
                throw new XamlIlParseException("Selector property should be a text node", node);

            var selectorType = pn.Property.GetClrProperty().Getter.ReturnType;
            var initialNode = new XamlIlSelectorInitialNode(node, selectorType);
            XamlIlSelectorNode Create(IEnumerable<SelectorGrammar.ISyntax> syntax,
                Func<string, string, XamlIlAstClrTypeReference> typeResolver)
            {
                XamlIlSelectorNode result = initialNode;
                XamlIlOrSelectorNode results = null;
                foreach (var i in syntax)
                {
                    switch (i)
                    {

                        case SelectorGrammar.OfTypeSyntax ofType:
                            result = new XamlIlTypeSelector(result, typeResolver(ofType.Xmlns, ofType.TypeName).Type, true);
                            break;
                        case SelectorGrammar.IsSyntax @is:
                            result = new XamlIlTypeSelector(result, typeResolver(@is.Xmlns, @is.TypeName).Type, false);
                            break;
                        case SelectorGrammar.ClassSyntax @class:
                            result = new XamlIlStringSelector(result, XamlIlStringSelector.SelectorType.Class, @class.Class);
                            break;
                        case SelectorGrammar.NameSyntax name:
                            result = new XamlIlStringSelector(result, XamlIlStringSelector.SelectorType.Name, name.Name);
                            break;
                        case SelectorGrammar.PropertySyntax property:
                        {
                            var type = result?.TargetType;

                            if (type == null)
                                throw new XamlIlParseException("Property selectors must be applied to a type.", node);

                            var targetProperty =
                                type.GetAllProperties().FirstOrDefault(p => p.Name == property.Property);

                            if (targetProperty == null)
                                throw new XamlIlParseException($"Cannot find '{property.Property}' on '{type}", node);

                            if (!XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context,
                                new XamlIlAstTextNode(node, property.Value, context.Configuration.WellKnownTypes.String),
                                targetProperty.PropertyType, out var typedValue))
                                throw new XamlIlParseException(
                                    $"Cannot convert '{property.Value}' to '{targetProperty.PropertyType.GetFqn()}",
                                    node);

                            result = new XamlIlPropertyEqualsSelector(result, targetProperty, typedValue);
                            break;
                        }
                        case SelectorGrammar.ChildSyntax child:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.SelectorType.Child);
                            break;
                        case SelectorGrammar.DescendantSyntax descendant:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.SelectorType.Descendant);
                            break;
                        case SelectorGrammar.TemplateSyntax template:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.SelectorType.Template);
                            break;
                        case SelectorGrammar.NotSyntax not:
                            result = new XamlIlNotSelector(result, Create(not.Argument, typeResolver));
                            break;
                        case SelectorGrammar.CommaSyntax comma:
                            if (results == null) 
                                results = new XamlIlOrSelectorNode(node, selectorType);
                            results.Add(result);
                            result = initialNode;
                            break;
                        default:
                            throw new XamlIlParseException($"Unsupported selector grammar '{i.GetType()}'.", node);
                    }
                }

                return results ?? result;
            }

            IEnumerable<SelectorGrammar.ISyntax> parsed;
            try
            {
                parsed = SelectorGrammar.Parse(tn.Text);
            }
            catch (Exception e)
            {
                throw new XamlIlParseException("Unable to parse selector: " + e.Message, node);
            }

            var selector = Create(parsed, (p, n) 
                => XamlIlTypeReferenceResolver.ResolveType(context, $"{p}:{n}", true, node, true));
            pn.Values[0] = selector;

            return new AvaloniaXamlIlTargetTypeMetadataNode(on,
                new XamlIlAstClrTypeReference(selector, selector.TargetType, false),
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);
        }

    }


    
    abstract class XamlIlSelectorNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        protected XamlIlSelectorNode Previous { get; }
        public abstract IXamlIlType TargetType { get; }

        public XamlIlSelectorNode(XamlIlSelectorNode previous,
            IXamlIlLineInfo info = null,
            IXamlIlType selectorType = null) : base(info ?? previous)
        {
            Previous = previous;
            Type = selectorType == null ? previous.Type : new XamlIlAstClrTypeReference(this, selectorType, false);
        }

        public IXamlIlAstTypeReference Type { get; }

        public virtual XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (Previous != null)
                context.Emit(Previous, codeGen, Type.GetClrType());
            DoEmit(context, codeGen);
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }
        
        protected abstract void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen);

        protected void EmitCall(XamlIlEmitContext context, IXamlIlEmitter codeGen, Func<IXamlIlMethod, bool> method)
        {
            var selectors = context.Configuration.TypeSystem.GetType("Avalonia.Styling.Selectors");
            var found = selectors.FindMethod(m => m.IsStatic && m.Parameters.Count > 0 &&
                                      m.Parameters[0].FullName == "Avalonia.Styling.Selector"
                                      && method(m));
            codeGen.EmitCall(found);
        }
    }
    
    class XamlIlSelectorInitialNode : XamlIlSelectorNode
    {
        public XamlIlSelectorInitialNode(IXamlIlLineInfo info,
            IXamlIlType selectorType) : base(null, info, selectorType)
        {
        }

        public override IXamlIlType TargetType => null;
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen) => codeGen.Ldnull();
    }

    class XamlIlTypeSelector : XamlIlSelectorNode
    {
        public bool Concrete { get; }

        public XamlIlTypeSelector(XamlIlSelectorNode previous, IXamlIlType type, bool concrete) : base(previous)
        {
            TargetType = type;
            Concrete = concrete;
        }

        public override IXamlIlType TargetType { get; }
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var name = Concrete ? "OfType" : "Is";
            codeGen.Ldtype(TargetType);
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 2 && m.Parameters[1].FullName == "System.Type");
        }
    }
    
    class XamlIlStringSelector : XamlIlSelectorNode
    {
        public string String { get; set; }
        public enum SelectorType
        {
            Class,
            Name
        }

        private SelectorType _type;

        public XamlIlStringSelector(XamlIlSelectorNode previous, SelectorType type, string s) : base(previous)
        {
            _type = type;
            String = s;
        }


        public override IXamlIlType TargetType => Previous?.TargetType;
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen.Ldstr(String);
            var name = _type.ToString();
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 2 && m.Parameters[1].FullName == "System.String");
        }
    }

    class XamlIlCombinatorSelector : XamlIlSelectorNode
    {
        private readonly SelectorType _type;

        public enum SelectorType
        {
            Child,
            Descendant,
            Template
        }
        public XamlIlCombinatorSelector(XamlIlSelectorNode previous, SelectorType type) : base(previous)
        {
            _type = type;
        }

        public override IXamlIlType TargetType => null;
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var name = _type.ToString();
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 1);
        }
    }

    class XamlIlNotSelector : XamlIlSelectorNode
    {
        public XamlIlSelectorNode Argument { get; }

        public XamlIlNotSelector(XamlIlSelectorNode previous, XamlIlSelectorNode argument) : base(previous)
        {
            Argument = argument;
        }

        public override IXamlIlType TargetType => Previous?.TargetType;
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            context.Emit(Argument, codeGen, Type.GetClrType());
            EmitCall(context, codeGen,
                m => m.Name == "Not" && m.Parameters.Count == 2 && m.Parameters[1].Equals(Type.GetClrType()));
        }
    }

    class XamlIlPropertyEqualsSelector : XamlIlSelectorNode
    {
        public XamlIlPropertyEqualsSelector(XamlIlSelectorNode previous,
            IXamlIlProperty property,
            IXamlIlAstValueNode value)
            : base(previous)
        {
            Property = property;
            Value = value;
        }

        public IXamlIlProperty Property { get; set; }
        public IXamlIlAstValueNode Value { get; set; }
        
        public override IXamlIlType TargetType => Previous?.TargetType;
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!XamlIlAvaloniaPropertyHelper.Emit(context, codeGen, Property))
                throw new XamlIlLoadException(
                    $"{Property.Name} of {(Property.Setter ?? Property.Getter).DeclaringType.GetFqn()} doesn't seem to be an AvaloniaProperty",
                    this);
            context.Emit(Value, codeGen, context.Configuration.WellKnownTypes.Object);
            EmitCall(context, codeGen,
                m => m.Name == "PropertyEquals"
                     && m.Parameters.Count == 3
                     && m.Parameters[1].FullName == "Avalonia.AvaloniaProperty"
                     && m.Parameters[2].Equals(context.Configuration.WellKnownTypes.Object));
        }
    }

    class XamlIlOrSelectorNode : XamlIlSelectorNode
    {
        List<XamlIlSelectorNode> _selectors = new List<XamlIlSelectorNode>();
        public XamlIlOrSelectorNode(IXamlIlLineInfo info, IXamlIlType selectorType) : base(null, info, selectorType)
        {
        }

        public void Add(XamlIlSelectorNode node)
        {
            _selectors.Add(node);
        }
        
        //TODO: actually find the type
        public override IXamlIlType TargetType => _selectors.FirstOrDefault()?.TargetType;
        protected override void DoEmit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (_selectors.Count == 0)
                throw new XamlIlLoadException("Invalid selector count", this);
            if (_selectors.Count == 1)
            {
                _selectors[0].Emit(context, codeGen);
                return;
            }
            var listType = context.Configuration.TypeSystem.FindType("System.Collections.Generic.List`1")
                .MakeGenericType(base.Type.GetClrType());
            var add = listType.FindMethod("Add", context.Configuration.WellKnownTypes.Void, false, Type.GetClrType());
            codeGen
                .Newobj(listType.FindConstructor());
            foreach (var s in _selectors)
            {
                codeGen.Dup();
                context.Emit(s, codeGen, Type.GetClrType());
                codeGen.EmitCall(add, true);
            }

            EmitCall(context, codeGen,
                m => m.Name == "Or" && m.Parameters.Count == 1 && m.Parameters[0].Name.StartsWith("IReadOnlyList"));
        }
    }
}
