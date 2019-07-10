using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;
using XamlIl;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    static class XamlIlBindingPathHelper
    {
        public static IXamlIlType UpdateCompiledBindingExtension(XamlIlAstTransformationContext context, XamlIlAstObjectNode binding, IXamlIlType startType)
        {
            IXamlIlType bindingResultType = null;
            if (binding.Arguments.Count > 0 && binding.Arguments[0] is XamlIlAstTextNode bindingPathText)
            {
                var reader = new CharacterReader(bindingPathText.Text.AsSpan());
                var grammar = BindingExpressionGrammar.Parse(ref reader);

                var transformed = TransformBindingPath(
                    context,
                    bindingPathText,
                    startType,
                    grammar.Nodes);

                bindingResultType = transformed.BindingResultType;
                binding.Arguments[0] = transformed;
            }
            else
            {
                var bindingPathAssignment = binding.Children.OfType<XamlIlAstXamlPropertyValueNode>()
                    .FirstOrDefault(v => v.Property.GetClrProperty().Name == "Path");

                if (bindingPathAssignment is null)
                {
                    return startType;
                }

                if (bindingPathAssignment.Values[0] is XamlIlAstTextNode pathValue)
                {
                    var reader = new CharacterReader(pathValue.Text.AsSpan());
                    var grammar = BindingExpressionGrammar.Parse(ref reader);

                    var transformed = TransformBindingPath(
                        context,
                        pathValue,
                        startType,
                        grammar.Nodes);

                    bindingResultType = transformed.BindingResultType;
                    bindingPathAssignment.Values[0] = transformed;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return bindingResultType;
        }

        private static IXamlIlBindingPathNode TransformBindingPath(XamlIlAstTransformationContext context, IXamlIlLineInfo lineInfo, IXamlIlType startType, IEnumerable<BindingExpressionGrammar.INode> bindingExpression)
        {
            List<IXamlIlBindingPathElementNode> transformNodes = new List<IXamlIlBindingPathElementNode>();
            List<IXamlIlBindingPathElementNode> nodes = new List<IXamlIlBindingPathElementNode>();
            foreach (var astNode in bindingExpression)
            {
                var targetType = nodes.Count == 0 ? startType : nodes[nodes.Count - 1].Type;
                switch (astNode)
                {
                    case BindingExpressionGrammar.EmptyExpressionNode _:
                        break;
                    case BindingExpressionGrammar.NotNode _:
                        transformNodes.Add(new XamlIlNotPathElementNode(context.Configuration.WellKnownTypes.Boolean));
                        break;
                    case BindingExpressionGrammar.StreamNode _:
                        IXamlIlType observableType;
                        if (targetType.GenericTypeDefinition?.Equals(context.Configuration.TypeSystem.FindType("System.IObservable`1")) == true)
                        {
                            observableType = targetType;
                        }
                        else
                        {
                            observableType = targetType.GetAllInterfaces().FirstOrDefault(i => i.GenericTypeDefinition?.Equals(context.Configuration.TypeSystem.FindType("System.IObservable`1")) ?? false);
                        }

                        if (observableType != null)
                        {
                            nodes.Add(new XamlIlStreamObservablePathElementNode(observableType.GenericArguments[0]));
                            break;
                        }
                        bool foundTask = false;
                        for (var currentType = targetType; currentType != null; currentType = currentType.BaseType)
                        {
                            if (currentType.GenericTypeDefinition.Equals(context.Configuration.TypeSystem.GetType("System.Threading.Tasks.Task`1")))
                            {
                                foundTask = true;
                                nodes.Add(new XamlIlStreamTaskPathElementNode(currentType.GenericArguments[0]));
                                break;
                            }
                        }
                        if (foundTask)
                        {
                            break;
                        }
                        throw new XamlIlParseException($"Compiled bindings do not support stream bindings for objects of type {targetType.FullName}.", lineInfo);
                    case BindingExpressionGrammar.PropertyNameNode propName:
                        var avaloniaPropertyFieldNameMaybe = propName.PropertyName + "Property";
                        var avaloniaPropertyFieldMaybe = targetType.GetAllFields().FirstOrDefault(f =>
                            f.IsStatic && f.IsPublic && f.Name == avaloniaPropertyFieldNameMaybe);

                        if (avaloniaPropertyFieldMaybe != null)
                        {
                            nodes.Add(new XamlIlAvaloniaPropertyPropertyPathElementNode(avaloniaPropertyFieldMaybe,
                                XamlIlAvaloniaPropertyHelper.GetAvaloniaPropertyType(avaloniaPropertyFieldMaybe, context.GetAvaloniaTypes(), lineInfo)));
                        }
                        else
                        {
                            var clrProperty = targetType.GetAllProperties().FirstOrDefault(p => p.Name == propName.PropertyName);
                            nodes.Add(new XamlIlClrPropertyPathElementNode(clrProperty));
                        }
                        break;
                    case BindingExpressionGrammar.IndexerNode indexer:
                        {
                            IXamlIlProperty property = null;
                            for (var currentType = targetType; currentType != null; currentType = currentType.BaseType)
                            {
                                var defaultMemberAttribute = currentType.CustomAttributes.FirstOrDefault(x => x.Type.GetFullName() == "System.Reflection.DefaultMemberAttribute");
                                if (defaultMemberAttribute != null)
                                {
                                    property = targetType.GetAllProperties().FirstOrDefault(x => x.Name == (string)defaultMemberAttribute.Parameters[0]);
                                    break;
                                }

                            };
                            if (property is null)
                            {
                                throw new XamlIlParseException($"The type '${targetType}' does not have an indexer.", lineInfo);
                            }

                            IEnumerable<IXamlIlType> parameters = property.IndexerParameters;

                            List<IXamlIlAstValueNode> values = new List<IXamlIlAstValueNode>();
                            int currentParamIndex = 0;
                            foreach (var param in parameters)
                            {
                                var textNode = new XamlIlAstTextNode(lineInfo, indexer.Arguments[currentParamIndex]);
                                if (!XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, textNode,
                                        param, out var converted))
                                    throw new XamlIlParseException(
                                        $"Unable to convert indexer parameter value of '{indexer.Arguments[currentParamIndex]}' to {param.GetFqn()}",
                                        textNode);

                                values.Add(converted);
                                currentParamIndex++;
                            }

                            bool isNotifyingCollection = targetType.GetAllInterfaces().Any(i => i.FullName == "System.Collections.Specialized.INotifyCollectionChanged");

                            nodes.Add(new XamlIlClrIndexerPathElementNode(property, values, isNotifyingCollection));
                            break;
                        }
                    case BindingExpressionGrammar.AttachedPropertyNameNode attachedProp:
                        var avaloniaPropertyFieldName = attachedProp.PropertyName + "Property";
                        var avaloniaPropertyField = GetType(attachedProp.Namespace, attachedProp.TypeName).GetAllFields().FirstOrDefault(f =>
                            f.IsStatic && f.IsPublic && f.Name == avaloniaPropertyFieldName);
                        nodes.Add(new XamlIlAvaloniaPropertyPropertyPathElementNode(avaloniaPropertyField,
                            XamlIlAvaloniaPropertyHelper.GetAvaloniaPropertyType(avaloniaPropertyField, context.GetAvaloniaTypes(), lineInfo)));
                        break;
                    case BindingExpressionGrammar.SelfNode _:
                        nodes.Add(new SelfPathElementNode(targetType));
                        break;
                    case BindingExpressionGrammar.AncestorNode ancestor:
                        nodes.Add(new FindAncestorPathElementNode(GetType(ancestor.Namespace, ancestor.TypeName), ancestor.Level));
                        break;
                    case BindingExpressionGrammar.NameNode elementName:
                        IXamlIlType elementType = null;
                        foreach (var deferredContent in context.ParentNodes().OfType<NestedScopeMetadataNode>())
                        {
                            elementType = ScopeRegistrationFinder.GetControlType(deferredContent, elementName.Name);
                            if (!(elementType is null))
                            {
                                break;
                            }
                        }
                        if (elementType is null)
                        {
                            elementType = ScopeRegistrationFinder.GetControlType(context.RootObject, elementName.Name);
                        }

                        if (elementType is null)
                        {
                            throw new XamlIlParseException($"Unable to find element '{elementName.Name}' in the current namescope. Unable to use a compiled binding with a name binding if the name cannot be found at compile time.", lineInfo);
                        }
                        nodes.Add(new ElementNamePathElementNode(elementName.Name, elementType));
                        break;
                }
            }

            return new XamlIlBindingPathNode(lineInfo, context.GetAvaloniaTypes().CompiledBindingPath, transformNodes, nodes);

            IXamlIlType GetType(string ns, string name)
            {
                return XamlIlTypeReferenceResolver.ResolveType(context, $"{ns}:{name}", false,
                    lineInfo, true).GetClrType();
            }
        }

        class ScopeRegistrationFinder : IXamlIlAstVisitor
        {
            private Stack<IXamlIlAstNode> _stack = new Stack<IXamlIlAstNode>();
            private Stack<IXamlIlAstNode> _childScopesStack = new Stack<IXamlIlAstNode>();

            private ScopeRegistrationFinder(string name)
            {
                Name = name;
            }

            string Name { get; }

            IXamlIlType ControlType { get; set; }

            public static IXamlIlType GetControlType(IXamlIlAstNode namescopeRoot, string name)
            {
                var finder = new ScopeRegistrationFinder(name);
                namescopeRoot.Visit(finder);
                return finder.ControlType;
            }

            void IXamlIlAstVisitor.Pop()
            {
                var node = _stack.Pop();
                if (_childScopesStack.Count > 0 && node == _childScopesStack.Peek())
                {
                    _childScopesStack.Pop();
                }
            }

            void IXamlIlAstVisitor.Push(IXamlIlAstNode node)
            {
                _stack.Push(node);
                if (node is NestedScopeMetadataNode)
                {
                    _childScopesStack.Push(node);
                }
            }

            IXamlIlAstNode IXamlIlAstVisitor.Visit(IXamlIlAstNode node)
            {
                if (_childScopesStack.Count == 0 && node is ScopeRegistrationNode registration)
                {
                    if (registration.Value is XamlIlAstTextNode text && text.Text == Name)
                    {
                        ControlType = registration.ControlType;
                    }
                }
                return node;
            }
        }

        interface IXamlIlBindingPathElementNode
        {
            IXamlIlType Type { get; }

            void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen);
        }

        class XamlIlNotPathElementNode : IXamlIlBindingPathElementNode
        {
            public XamlIlNotPathElementNode(IXamlIlType boolType)
            {
                Type = boolType;
            }

            public IXamlIlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "Not"));
            }
        }

        class XamlIlStreamObservablePathElementNode : IXamlIlBindingPathElementNode
        {
            public XamlIlStreamObservablePathElementNode(IXamlIlType type)
            {
                Type = type;
            }

            public IXamlIlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "StreamObservable").MakeGenericMethod(new[] { Type }));
            }
        }

        class XamlIlStreamTaskPathElementNode : IXamlIlBindingPathElementNode
        {
            public XamlIlStreamTaskPathElementNode(IXamlIlType type)
            {
                Type = type;
            }

            public IXamlIlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "StreamTask").MakeGenericMethod(new[] { Type }));
            }
        }

        class SelfPathElementNode : IXamlIlBindingPathElementNode
        {
            public SelfPathElementNode(IXamlIlType type)
            {
                Type = type;
            }

            public IXamlIlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "Self"));
            }
        }

        class FindAncestorPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly int _level;

            public FindAncestorPathElementNode(IXamlIlType ancestorType, int level)
            {
                Type = ancestorType;
                _level = level;
            }

            public IXamlIlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.Ldtype(Type)
                    .Ldc_I4(_level)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "FindAncestor"));
            }
        }

        class ElementNamePathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly string _name;

            public ElementNamePathElementNode(string name, IXamlIlType elementType)
            {
                _name = name;
                Type = elementType;
            }

            public IXamlIlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.Ldstr(_name)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "ElementName"));
            }
        }

        class XamlIlAvaloniaPropertyPropertyPathElementNode : IXamlIlBindingPathElementNode
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
                        .NotifyingPropertyInfoHelpers.FindMethod(m => m.Name == "CreateAvaloniaPropertyInfo"))
                    .EmitCall(context.GetAvaloniaTypes()
                        .CompiledBindingPathBuilder.FindMethod(m => m.Name == "Property"));

            public IXamlIlType Type { get; }
        }

        class XamlIlClrPropertyPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlIlProperty _property;

            public XamlIlClrPropertyPathElementNode(IXamlIlProperty property)
            {
                _property = property;
            }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property);

                codeGen
                    .EmitCall(context.GetAvaloniaTypes()
                        .NotifyingPropertyInfoHelpers.FindMethod(m => m.Name == "CreateINPCPropertyInfo"))
                    .EmitCall(context.GetAvaloniaTypes()
                        .CompiledBindingPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlIlType Type => _property.Getter?.ReturnType ?? _property.Setter?.Parameters[0];
        }

        class XamlIlClrIndexerPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlIlProperty _property;
            private readonly List<IXamlIlAstValueNode> _values;
            private readonly bool _isNotifyingCollection;

            public XamlIlClrIndexerPathElementNode(IXamlIlProperty property, List<IXamlIlAstValueNode> values, bool isNotifyingCollection)
            {
                _property = property;
                _values = values;
                _isNotifyingCollection = isNotifyingCollection;
            }

            public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                var intType = context.Configuration.TypeSystem.GetType("System.Int32");
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property, _values);

                if (_isNotifyingCollection
                    &&
                    _values.Count == 1
                    && _values[0].Type.GetClrType().Equals(intType))
                {
                    context.Emit(_values[0], codeGen, intType);
                    codeGen.EmitCall(context.GetAvaloniaTypes()
                        .NotifyingPropertyInfoHelpers.FindMethod(m => m.Name == "CreateIndexerPropertyInfo"));
                }
                else
                {
                    codeGen.EmitCall(context.GetAvaloniaTypes()
                        .NotifyingPropertyInfoHelpers.FindMethod(m => m.Name == "CreateINPCPropertyInfo"));
                }

                codeGen.EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlIlType Type => _property.Getter?.ReturnType ?? _property.Setter?.Parameters[0];
        }

        class XamlIlBindingPathNode : XamlIlAstNode, IXamlIlBindingPathNode, IXamlIlAstEmitableNode
        {
            private readonly List<IXamlIlBindingPathElementNode> _transformElements;
            private readonly List<IXamlIlBindingPathElementNode> _elements;

            public XamlIlBindingPathNode(IXamlIlLineInfo lineInfo,
                IXamlIlType bindingPathType,
                List<IXamlIlBindingPathElementNode> transformElements,
                List<IXamlIlBindingPathElementNode> elements) : base(lineInfo)
            {
                Type = new XamlIlAstClrTypeReference(lineInfo, bindingPathType, false);
                _transformElements = transformElements;
                _elements = elements;
            }

            public IXamlIlType BindingResultType
                => _transformElements.Count > 0
                    ? _transformElements[0].Type
                    : _elements[_elements.Count - 1].Type;

            public IXamlIlAstTypeReference Type { get; }

            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                var types = context.GetAvaloniaTypes();
                codeGen.Newobj(types.CompiledBindingPathBuilder.FindConstructor());

                foreach (var transform in _transformElements)
                {
                    transform.Emit(context, codeGen);
                }

                foreach (var element in _elements)
                {
                    element.Emit(context, codeGen);
                }

                codeGen.EmitCall(types.CompiledBindingPathBuilder.FindMethod(m => m.Name == "Build"));
                return XamlIlNodeEmitResult.Type(0, types.CompiledBindingPath);
            }
        }
    }

    interface IXamlIlBindingPathNode : IXamlIlAstValueNode
    {
        IXamlIlType BindingResultType { get; }
    }
}
