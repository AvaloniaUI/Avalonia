﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using XamlX;
using XamlX.Emit;
using XamlX.IL;
using Avalonia.Utilities;

using XamlIlEmitContext = XamlX.Emit.XamlEmitContext<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    static class XamlIlBindingPathHelper
    {
        public static IXamlType UpdateCompiledBindingExtension(AstTransformationContext context, XamlAstConstructableObjectNode binding, IXamlType startType)
        {
            IXamlType bindingResultType = null;
            if (binding.Arguments.Count > 0 && binding.Arguments[0] is ParsedBindingPathNode bindingPath)
            {
                var transformed = TransformBindingPath(
                    context,
                    bindingPath,
                    startType,
                    bindingPath.Path);

                bindingResultType = transformed.BindingResultType;
                binding.Arguments[0] = transformed;
            }
            else
            {
                var bindingPathAssignment = binding.Children.OfType<XamlPropertyAssignmentNode>()
                    .FirstOrDefault(v => v.Property.Name == "Path");

                if (bindingPathAssignment is null)
                {
                    return startType;
                }

                if (bindingPathAssignment.Values[0] is ParsedBindingPathNode bindingPathNode)
                {
                    var transformed = TransformBindingPath(
                        context,
                        bindingPathNode,
                        startType,
                        bindingPathNode.Path);

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

        private static IXamlIlBindingPathNode TransformBindingPath(AstTransformationContext context, IXamlLineInfo lineInfo, IXamlType startType, IEnumerable<BindingExpressionGrammar.INode> bindingExpression)
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
                        IXamlType observableType;
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
                        throw new XamlX.XamlParseException($"Compiled bindings do not support stream bindings for objects of type {targetType.FullName}.", lineInfo);
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
                            var clrProperty = GetAllDefinedProperties(targetType).FirstOrDefault(p => p.Name == propName.PropertyName);

                            if (clrProperty is null)
                            {
                                throw new XamlX.XamlParseException($"Unable to resolve property of name '{propName.PropertyName}' on type '{targetType}'.", lineInfo);
                            }
                            nodes.Add(new XamlIlClrPropertyPathElementNode(clrProperty));
                        }
                        break;
                    case BindingExpressionGrammar.IndexerNode indexer:
                        {
                            if (targetType.IsArray)
                            {
                                nodes.Add(new XamlIlArrayIndexerPathElementNode(targetType, indexer.Arguments, lineInfo));
                                break;
                            }

                            IXamlProperty property = null;
                            foreach (var currentType in TraverseTypeHierarchy(targetType))
                            {
                                var defaultMemberAttribute = currentType.CustomAttributes.FirstOrDefault(x => x.Type.Namespace == "System.Reflection" && x.Type.Name == "DefaultMemberAttribute");
                                if (defaultMemberAttribute != null)
                                {
                                    property = currentType.GetAllProperties().FirstOrDefault(x => x.Name == (string)defaultMemberAttribute.Parameters[0]);
                                    break;
                                }
                            }
                            if (property is null)
                            {
                                throw new XamlX.XamlParseException($"The type '${targetType}' does not have an indexer.", lineInfo);
                            }

                            IEnumerable<IXamlType> parameters = property.IndexerParameters;

                            List<IXamlAstValueNode> values = new List<IXamlAstValueNode>();
                            int currentParamIndex = 0;
                            foreach (var param in parameters)
                            {
                                var textNode = new XamlAstTextNode(lineInfo, indexer.Arguments[currentParamIndex], type: context.Configuration.WellKnownTypes.String);
                                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, textNode,
                                        param, out var converted))
                                    throw new XamlX.XamlParseException(
                                        $"Unable to convert indexer parameter value of '{indexer.Arguments[currentParamIndex]}' to {param.GetFqn()}",
                                        textNode);

                                values.Add(converted);
                                currentParamIndex++;
                            }

                            bool isNotifyingCollection = targetType.GetAllInterfaces().Any(i => i.FullName == "System.Collections.Specialized.INotifyCollectionChanged");

                            nodes.Add(new XamlIlClrIndexerPathElementNode(property, values, string.Join(",", indexer.Arguments), isNotifyingCollection));
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
                    case VisualAncestorBindingExpressionNode visualAncestor:
                        nodes.Add(new FindVisualAncestorPathElementNode(visualAncestor.Type, visualAncestor.Level));
                        break;
                    case TemplatedParentBindingExpressionNode templatedParent:
                        var templatedParentField = context.GetAvaloniaTypes().StyledElement.GetAllFields()
                            .FirstOrDefault(f => f.IsStatic && f.IsPublic && f.Name == "TemplatedParentProperty");
                        nodes.Add(new XamlIlAvaloniaPropertyPropertyPathElementNode(
                            templatedParentField,
                            templatedParent.Type));
                        break;
                    case BindingExpressionGrammar.AncestorNode ancestor:
                        if (ancestor.Namespace is null && ancestor.TypeName is null)
                        {
                            var styledElementType = context.GetAvaloniaTypes().StyledElement;
                            var ancestorType = context
                                .ParentNodes()
                                .OfType<XamlAstConstructableObjectNode>()
                                .Where(x => styledElementType.IsAssignableFrom(x.Type.GetClrType()))
                                .ElementAtOrDefault(ancestor.Level)
                                ?.Type.GetClrType();

                            if (ancestorType is null)
                            {
                                throw new XamlX.XamlParseException("Unable to resolve implicit ancestor type based on XAML tree.", lineInfo);
                            }

                            nodes.Add(new FindAncestorPathElementNode(ancestorType, ancestor.Level));
                        }
                        else
                        {
                            nodes.Add(new FindAncestorPathElementNode(GetType(ancestor.Namespace, ancestor.TypeName), ancestor.Level));
                        }
                        break;
                    case BindingExpressionGrammar.NameNode elementName:
                        IXamlType elementType = null;
                        foreach (var deferredContent in context.ParentNodes().OfType<NestedScopeMetadataNode>())
                        {
                            elementType = ScopeRegistrationFinder.GetTargetType(deferredContent, elementName.Name);
                            if (!(elementType is null))
                            {
                                break;
                            }
                        }
                        if (elementType is null)
                        {
                            elementType = ScopeRegistrationFinder.GetTargetType(context.ParentNodes().Last(), elementName.Name);
                        }

                        if (elementType is null)
                        {
                            throw new XamlX.XamlParseException($"Unable to find element '{elementName.Name}' in the current namescope. Unable to use a compiled binding with a name binding if the name cannot be found at compile time.", lineInfo);
                        }
                        nodes.Add(new ElementNamePathElementNode(elementName.Name, elementType));
                        break;
                    case RawSourceBindingExpressionNode rawSource:
                        nodes.Add(new RawSourcePathElementNode(rawSource.RawSource));
                        break;
                }
            }

            return new XamlIlBindingPathNode(lineInfo, context.GetAvaloniaTypes().CompiledBindingPath, transformNodes, nodes);

            IXamlType GetType(string ns, string name)
            {
                return TypeReferenceResolver.ResolveType(context, $"{ns}:{name}", false,
                    lineInfo, true).GetClrType();
            }

            static IEnumerable<IXamlProperty> GetAllDefinedProperties(IXamlType type)
            {
                foreach (var t in TraverseTypeHierarchy(type))
                {
                    foreach (var p in t.Properties)
                    {
                        yield return p;
                    }
                }
            }

            static IEnumerable<IXamlType> TraverseTypeHierarchy(IXamlType type)
            {
                if (type.IsInterface)
                {
                    yield return type;
                    foreach (var iface in type.Interfaces)
                    {
                        foreach (var h in TraverseTypeHierarchy(iface))
                        {
                            yield return h;
                        }
                    }
                }
                else
                {
                    for (var currentType = type; currentType != null; currentType = currentType.BaseType)
                    {
                        yield return currentType;
                    }
                }
            }
        }

        class ScopeRegistrationFinder : IXamlAstVisitor
        {
            private Stack<IXamlAstNode> _stack = new Stack<IXamlAstNode>();
            private Stack<IXamlAstNode> _childScopesStack = new Stack<IXamlAstNode>();

            private ScopeRegistrationFinder(string name)
            {
                Name = name;
            }

            string Name { get; }

            IXamlType TargetType { get; set; }

            public static IXamlType GetTargetType(IXamlAstNode namescopeRoot, string name)
            {
                var finder = new ScopeRegistrationFinder(name);
                namescopeRoot.Visit(finder);
                return finder.TargetType;
            }

            void IXamlAstVisitor.Pop()
            {
                var node = _stack.Pop();
                if (_childScopesStack.Count > 0 && node == _childScopesStack.Peek())
                {
                    _childScopesStack.Pop();
                }
            }

            void IXamlAstVisitor.Push(IXamlAstNode node)
            {
                _stack.Push(node);
                if (node is NestedScopeMetadataNode)
                {
                    _childScopesStack.Push(node);
                }
            }

            IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
            {
                if (_childScopesStack.Count == 0 && node is AvaloniaNameScopeRegistrationXamlIlNode registration)
                {
                    if (registration.Name is XamlAstTextNode text && text.Text == Name)
                    {
                        TargetType = registration.TargetType;
                    }
                }
                return node;
            }
        }

        interface IXamlIlBindingPathElementNode
        {
            IXamlType Type { get; }

            void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen);
        }

        class XamlIlNotPathElementNode : IXamlIlBindingPathElementNode
        {
            public XamlIlNotPathElementNode(IXamlType boolType)
            {
                Type = boolType;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "Not"));
            }
        }

        class XamlIlStreamObservablePathElementNode : IXamlIlBindingPathElementNode
        {
            public XamlIlStreamObservablePathElementNode(IXamlType type)
            {
                Type = type;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "StreamObservable").MakeGenericMethod(new[] { Type }));
            }
        }

        class XamlIlStreamTaskPathElementNode : IXamlIlBindingPathElementNode
        {
            public XamlIlStreamTaskPathElementNode(IXamlType type)
            {
                Type = type;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "StreamTask").MakeGenericMethod(new[] { Type }));
            }
        }

        class SelfPathElementNode : IXamlIlBindingPathElementNode
        {
            public SelfPathElementNode(IXamlType type)
            {
                Type = type;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "Self"));
            }
        }

        class FindAncestorPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly int _level;

            public FindAncestorPathElementNode(IXamlType ancestorType, int level)
            {
                Type = ancestorType;
                _level = level;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.Ldtype(Type)
                    .Ldc_I4(_level)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "FindAncestor"));
            }
        }

        class FindVisualAncestorPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly int _level;

            public FindVisualAncestorPathElementNode(IXamlType ancestorType, int level)
            {
                Type = ancestorType;
                _level = level;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.Ldtype(Type)
                    .Ldc_I4(_level)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "VisualAncestor"));
            }
        }

        class ElementNamePathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly string _name;

            public ElementNamePathElementNode(string name, IXamlType elementType)
            {
                _name = name;
                Type = elementType;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                var scopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                    f.Name == AvaloniaXamlIlLanguage.ContextNameScopeFieldName);

                codeGen
                    .Ldloc(context.ContextLocal)
                    .Ldfld(scopeField)
                    .Ldstr(_name)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.FindMethod(m => m.Name == "ElementName"));
            }
        }

        class XamlIlAvaloniaPropertyPropertyPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlField _field;

            public XamlIlAvaloniaPropertyPropertyPathElementNode(IXamlField field, IXamlType propertyType)
            {
                _field = field;
                Type = propertyType;
            }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.Ldsfld(_field);
                context.Configuration.GetExtra<XamlIlPropertyInfoAccessorFactoryEmitter>()
                    .EmitLoadAvaloniaPropertyAccessorFactory(context, codeGen);
                codeGen.EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlType Type { get; }
        }

        class XamlIlClrPropertyPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlProperty _property;

            public XamlIlClrPropertyPathElementNode(IXamlProperty property)
            {
                _property = property;
            }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property);

                context.Configuration.GetExtra<XamlIlPropertyInfoAccessorFactoryEmitter>()
                    .EmitLoadInpcPropertyAccessorFactory(context, codeGen);

                codeGen
                    .EmitCall(context.GetAvaloniaTypes()
                        .CompiledBindingPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlType Type => _property.Getter?.ReturnType ?? _property.Setter?.Parameters[0];
        }

        class XamlIlClrIndexerPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlProperty _property;
            private readonly List<IXamlAstValueNode> _values;
            private readonly string _indexerKey;
            private readonly bool _isNotifyingCollection;

            public XamlIlClrIndexerPathElementNode(IXamlProperty property, List<IXamlAstValueNode> values, string indexerKey, bool isNotifyingCollection)
            {
                _property = property;
                _values = values;
                _indexerKey = indexerKey;
                _isNotifyingCollection = isNotifyingCollection;
            }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                var intType = context.Configuration.TypeSystem.GetType("System.Int32");
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property, _values, _indexerKey);

                if (_isNotifyingCollection
                    &&
                    _values.Count == 1
                    && _values[0].Type.GetClrType().Equals(intType))
                {
                    context.Configuration.GetExtra<XamlIlPropertyInfoAccessorFactoryEmitter>()
                        .EmitLoadIndexerAccessorFactory(context, codeGen, _values[0]);
                }
                else
                {
                    context.Configuration.GetExtra<XamlIlPropertyInfoAccessorFactoryEmitter>()
                        .EmitLoadInpcPropertyAccessorFactory(context, codeGen);
                }

                codeGen.EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.FindMethod(m => m.Name == "Property"));
            }

            public IXamlType Type => _property.Getter?.ReturnType ?? _property.Setter?.Parameters[0];
        }

        class XamlIlArrayIndexerPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlType _arrayType;
            private readonly List<int> _values;

            public XamlIlArrayIndexerPathElementNode(IXamlType arrayType, IList<string> values, IXamlLineInfo lineInfo)
            {
                _arrayType = arrayType;
                _values = new List<int>(values.Count);
                foreach (var item in values)
                {
                    if (!int.TryParse(item, out var index))
                    {
                        throw new XamlX.XamlParseException($"Unable to convert '{item}' to an integer.", lineInfo.Line, lineInfo.Position);
                    }
                    _values.Add(index);
                }
            }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                var intType = context.Configuration.TypeSystem.GetType("System.Int32");
                var indices = codeGen.DefineLocal(intType.MakeArrayType(1));
                codeGen.Ldc_I4(_values.Count)
                    .Newarr(intType)
                    .Stloc(indices);
                for (int i = 0; i < _values.Count; i++)
                {
                    codeGen.Ldloc(indices)
                        .Ldc_I4(i)
                        .Ldc_I4(_values[i])
                        .Emit(OpCodes.Stelem_I4);
                }

                codeGen.Ldloc(indices)
                    .Ldtype(Type)
                    .EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.FindMethod(m => m.Name == "ArrayElement"));
            }

            public IXamlType Type => _arrayType.ArrayElementType;
        }

        class RawSourcePathElementNode : XamlAstNode, IXamlIlBindingPathElementNode
        {
            private readonly IXamlAstValueNode _rawSource;

            public RawSourcePathElementNode(IXamlAstValueNode rawSource)
                :base(rawSource)
            {
                _rawSource = rawSource;
                
            }

            public IXamlType Type => _rawSource.Type.GetClrType();

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                context.Emit(_rawSource, codeGen, Type);
                codeGen
                    .EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.FindMethod(m => m.Name == "SetRawSource"));
            }
        }

        class XamlIlBindingPathNode : XamlAstNode, IXamlIlBindingPathNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
        {
            private readonly List<IXamlIlBindingPathElementNode> _transformElements;
            private readonly List<IXamlIlBindingPathElementNode> _elements;

            public XamlIlBindingPathNode(IXamlLineInfo lineInfo,
                IXamlType bindingPathType,
                List<IXamlIlBindingPathElementNode> transformElements,
                List<IXamlIlBindingPathElementNode> elements) : base(lineInfo)
            {
                Type = new XamlAstClrTypeReference(lineInfo, bindingPathType, false);
                _transformElements = transformElements;
                _elements = elements;
            }

            public IXamlType BindingResultType
                => _transformElements.Count > 0
                    ? _transformElements[0].Type
                    : _elements[_elements.Count - 1].Type;

            public IXamlAstTypeReference Type { get; }

            public XamlILNodeEmitResult Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
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
                return XamlILNodeEmitResult.Type(0, types.CompiledBindingPath);
            }

            public override void VisitChildren(IXamlAstVisitor visitor)
            {
                for (int i = 0; i < _transformElements.Count; i++)
                {
                    if (_transformElements[i] is IXamlAstNode ast)
                    {
                        _transformElements[i] = (IXamlIlBindingPathElementNode)ast.Visit(visitor);
                    }
                }
                for (int i = 0; i < _elements.Count; i++)
                {
                    if (_elements[i] is IXamlAstNode ast)
                    {
                        _elements[i] = (IXamlIlBindingPathElementNode)ast.Visit(visitor);
                    }
                }
            }
        }
    }

    interface IXamlIlBindingPathNode : IXamlAstValueNode
    {
        IXamlType BindingResultType { get; }
    }
}
