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
            if (!(node is XamlIlAstXamlPropertyValueNode pn
                  && pn.Property.GetClrProperty().PropertyType.FullName == "Avalonia.Styling.Selector"))
                return node;

            if (pn.Values.Count != 1)
                throw new XamlIlParseException("Selector property should should have exactly one value", node);
            if (!(pn.Values[0] is XamlIlAstTextNode tn))
                return node;

            var selectorType = pn.Property.GetClrProperty().PropertyType;

            XamlIlSelectorNode Create(IEnumerable<SelectorGrammar.ISyntax> syntax,
                Func<string, string, IXamlIlType> typeResolver)
            {
                XamlIlSelectorNode result = new XamlIlSelectorInitialNode(node, selectorType);

                foreach (var i in syntax)
                {
                    switch (i)
                    {

                        case SelectorGrammar.OfTypeSyntax ofType:
                            result = new XamlIlTypeSelector(result, typeResolver(ofType.Xmlns, ofType.TypeName), true);
                            break;
                        case SelectorGrammar.IsSyntax @is:
                            result = new XamlIlTypeSelector(result, typeResolver(@is.Xmlns, @is.TypeName), false);
                            break;
                        case SelectorGrammar.ClassSyntax @class:
                            result = new XamlIlStringSelector(result, XamlIlStringSelector.Type.Class, @class.Class);
                            break;
                        case SelectorGrammar.NameSyntax name:
                            result = new XamlIlStringSelector(result, XamlIlStringSelector.Type.Name, name.Name);
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
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.Type.Child);
                            break;
                        case SelectorGrammar.DescendantSyntax descendant:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.Type.Descendant);
                            break;
                        case SelectorGrammar.TemplateSyntax template:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.Type.Template);
                            break;
                        case SelectorGrammar.NotSyntax not:
                            result = new XamlIlNotSelector(result, Create(not.Argument, typeResolver));
                            break;
                        default:
                            throw new XamlIlParseException($"Unsupported selector grammar '{i.GetType()}'.", node);
                    }
                }

                return result;
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
                => XamlIlTypeReferenceResolver.ResolveType(context, $"{p}:{n}", node, true));
            pn.Values[0] = selector;
            return node;
        }

    }


    
    abstract class XamlIlSelectorNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlSelectorNode Previous { get; }
        public abstract IXamlIlType TargetType { get; }

        public XamlIlSelectorNode(XamlIlSelectorNode previous,
            IXamlIlLineInfo info = null,
            IXamlIlType selectorType = null) : base(info ?? previous)
        {
            Previous = previous;
            Type = selectorType == null ? previous.Type : new XamlIlAstClrTypeReference(this, selectorType);
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
        public enum Type
        {
            Class,
            Name
        }

        private Type _type;

        public XamlIlStringSelector(XamlIlSelectorNode previous, Type type, string s) : base(previous)
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
        private readonly Type _type;

        public enum Type
        {
            Child,
            Descendant,
            Template
        }
        public XamlIlCombinatorSelector(XamlIlSelectorNode previous, Type type) : base(previous)
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
            if (!AvaloniaPropertyDescriptorEmitter.Emit(context, codeGen, Property))
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
}
