using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Data.Core.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using XamlIlEmitContext = XamlX.Emit.XamlEmitContextWithLocals<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    static class XamlIlBindingPathHelper
    {
        public static IXamlType UpdateCompiledBindingExtension(AstTransformationContext context, XamlAstConstructableObjectNode binding, Func<IXamlType> startTypeResolver, IXamlType selfType)
        {
            IXamlType? bindingResultType;
            if (binding.Arguments.Count > 0 && binding.Arguments[0] is ParsedBindingPathNode bindingPath)
            {
                var transformed = TransformBindingPath(
                    context,
                    bindingPath,
                    startTypeResolver,
                    selfType,
                    bindingPath.Path);

                transformed = TransformForTargetTyping(transformed, context);

                bindingResultType = transformed.BindingResultType;
                binding.Arguments[0] = transformed;
            }
            else if (binding.Arguments.Count > 0 && binding.Arguments[0] is XamlIlBindingPathNode alreadyTransformed)
            {
                bindingResultType = alreadyTransformed.BindingResultType;
            }
            else
            {
                var bindingPathAssignment = binding.Children.OfType<XamlPropertyAssignmentNode>()
                    .FirstOrDefault(v => v.Property.Name == "Path");

                if (bindingPathAssignment is null)
                {
                    return startTypeResolver();
                }

                if (bindingPathAssignment.Values[0] is XamlIlBindingPathNode pathNode)
                {
                    bindingResultType = pathNode.BindingResultType;
                }
                else if (bindingPathAssignment.Values[0] is ParsedBindingPathNode bindingPathNode)
                {
                    var transformed = TransformBindingPath(
                        context,
                        bindingPathNode,
                        startTypeResolver,
                        selfType,
                        bindingPathNode.Path);

                    transformed = TransformForTargetTyping(transformed, context);

                    bindingResultType = transformed.BindingResultType;
                    bindingPathAssignment.Values[0] = transformed;
                }
                else
                {
                    throw new InvalidOperationException("Invalid state of Path property");
                }
            }

            return bindingResultType;
        }

        private static XamlIlBindingPathNode TransformForTargetTyping(XamlIlBindingPathNode transformed, AstTransformationContext context)
        {
            var parentNode = context.ParentNodes().OfType<XamlPropertyAssignmentNode>().FirstOrDefault();

            if (parentNode == null)
            {
                return transformed;
            }
            
            var lastElement = transformed.Elements.LastOrDefault();
            
            if (GetPropertyType(context, parentNode) == context.GetAvaloniaTypes().ICommand && lastElement is XamlIlClrMethodPathElementNode methodPathElement)
            {
                var executeMethod = methodPathElement.Method;
                var canExecuteMethod = executeMethod.DeclaringType.FindMethod(new FindMethodMethodSignature(
                    $"Can{executeMethod.Name}",
                    context.Configuration.WellKnownTypes.Boolean,
                    context.Configuration.WellKnownTypes.Object));

                List<string> dependsOnProperties = new();
                if (canExecuteMethod is not null)
                {
                    foreach (var attr in canExecuteMethod.CustomAttributes)
                    {
                        if (attr.Type == context.GetAvaloniaTypes().DependsOnAttribute)
                        {
                            dependsOnProperties.Add((string)attr.Parameters[0]!);
                        }
                    }
                }
                transformed.Elements.RemoveAt(transformed.Elements.Count - 1);
                transformed.Elements.Add(new XamlIlClrMethodAsCommandPathElementNode(context.GetAvaloniaTypes().ICommand, executeMethod, canExecuteMethod, dependsOnProperties));
            }

            return transformed;
        }

        private static IXamlType? GetPropertyType(AstTransformationContext context, XamlPropertyAssignmentNode node)
        {
            var setterType = context.GetAvaloniaTypes().Setter;

            if (node.Property.DeclaringType == setterType && node.Property.Name == "Value")
            {
                // The property is a Setter.Value property. We need to get the type of the property that the Setter.Value property is setting.
                var setter = context.ParentNodes()
                    .SkipWhile(x => x != node)
                    .OfType<XamlAstConstructableObjectNode>()
                    .Take(1)
                    .FirstOrDefault(x => x.Type.GetClrType() == setterType);
                var propertyAssignment = setter?.Children.OfType<XamlPropertyAssignmentNode>()
                    .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Property");
                var property = propertyAssignment?.Values.FirstOrDefault() as IXamlIlAvaloniaPropertyNode;

                if (property?.AvaloniaPropertyType is { } propertyType)
                    return propertyType;
            }

            return node.Property.Getter?.ReturnType;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
        private static XamlIlBindingPathNode TransformBindingPath(AstTransformationContext context, IXamlLineInfo lineInfo, Func<IXamlType> startTypeResolver, IXamlType selfType, IEnumerable<BindingExpressionGrammar.INode> bindingExpression)
        {
            List<IXamlIlBindingPathElementNode> transformNodes = new List<IXamlIlBindingPathElementNode>();
            List<IXamlIlBindingPathElementNode> nodes = new List<IXamlIlBindingPathElementNode>();
            foreach (var astNode in bindingExpression)
            {
                var targetTypeResolver = nodes.Count == 0 ? startTypeResolver : () => nodes[nodes.Count - 1].Type;
                switch (astNode)
                {
                    case BindingExpressionGrammar.EmptyExpressionNode _:
                        break;
                    case BindingExpressionGrammar.NotNode _:
                        transformNodes.Add(new XamlIlNotPathElementNode(context.Configuration.WellKnownTypes.Boolean));
                        break;
                    case BindingExpressionGrammar.StreamNode _:
                        {
                            IXamlType targetType = targetTypeResolver();
                            IXamlType? observableType;
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
                            var taskType = context.Configuration.TypeSystem.GetType("System.Threading.Tasks.Task`1");

                            for (var currentType = targetType; currentType != null; currentType = currentType.BaseType)
                            {
                                if (currentType.GenericTypeDefinition?.Equals(taskType) == true)
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
                            throw new XamlX.XamlTransformException($"Compiled bindings do not support stream bindings for objects of type {targetType.FullName}.", lineInfo);
                        }
                    case BindingExpressionGrammar.PropertyNameNode propName:
                        {
                            IXamlType targetType = targetTypeResolver();
                            var avaloniaPropertyFieldNameMaybe = propName.PropertyName + "Property";
                            var avaloniaPropertyFieldMaybe = targetType.GetAllFields().FirstOrDefault(f =>
                                f.IsStatic && f.IsPublic && f.Name == avaloniaPropertyFieldNameMaybe);

                            if (avaloniaPropertyFieldMaybe != null)
                            {
                                var isDataContextProperty = avaloniaPropertyFieldMaybe.Name == "DataContextProperty" && Equals(avaloniaPropertyFieldMaybe.DeclaringType, context.GetAvaloniaTypes().StyledElement);
                                var propertyType = isDataContextProperty
                                    ? (nodes.LastOrDefault() as IXamlIlBindingPathNodeWithDataContextType)?.DataContextType
                                    : null;
                                propertyType ??= XamlIlAvaloniaPropertyHelper.GetAvaloniaPropertyType(avaloniaPropertyFieldMaybe, context.GetAvaloniaTypes(), lineInfo);

                                nodes.Add(new XamlIlAvaloniaPropertyPropertyPathElementNode(avaloniaPropertyFieldMaybe, propertyType, propName.AcceptsNull));
                            }
                            else if (GetAllDefinedProperties(targetType).FirstOrDefault(p => p.Name == propName.PropertyName) is IXamlProperty clrProperty)
                            {
                                nodes.Add(new XamlIlClrPropertyPathElementNode(clrProperty, propName.AcceptsNull));
                            }
                            else if (GetAllDefinedMethods(targetType).FirstOrDefault(m => m.Name == propName.PropertyName) is IXamlMethod method)
                            {
                                nodes.Add(new XamlIlClrMethodPathElementNode(method, context.Configuration.WellKnownTypes.Delegate, propName.AcceptsNull));
                            }
                            else
                            {
                                throw new XamlX.XamlTransformException($"Unable to resolve property or method of name '{propName.PropertyName}' on type '{targetType}'.", lineInfo);
                            }
                            break;
                        }
                    case BindingExpressionGrammar.IndexerNode indexer:
                        {
                            IXamlType targetType = targetTypeResolver();
                            if (targetType.IsArray)
                            {
                                nodes.Add(new XamlIlArrayIndexerPathElementNode(targetType, indexer.Arguments, lineInfo));
                                break;
                            }

                            IXamlProperty? property = null;
                            foreach (var currentType in TraverseTypeHierarchy(targetType))
                            {
                                var defaultMemberAttribute = currentType.CustomAttributes.FirstOrDefault(x => x.Type.Namespace == "System.Reflection" && x.Type.Name == "DefaultMemberAttribute");
                                if (defaultMemberAttribute != null)
                                {
                                    property = currentType.GetAllProperties().FirstOrDefault(x => x.Name == (string)defaultMemberAttribute.Parameters[0]!);
                                    break;
                                }
                            }
                            if (property is null)
                            {
                                throw new XamlX.XamlTransformException($"The type '${targetType}' does not have an indexer.", lineInfo);
                            }

                            IEnumerable<IXamlType> parameters = property.IndexerParameters;

                            List<IXamlAstValueNode> values = new List<IXamlAstValueNode>();
                            int currentParamIndex = 0;
                            foreach (var param in parameters)
                            {
                                var textNode = new XamlAstTextNode(lineInfo, indexer.Arguments[currentParamIndex], type: context.Configuration.WellKnownTypes.String);
                                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, textNode,
                                        property.CustomAttributes, param, out var converted))
                                    throw new XamlX.XamlTransformException(
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
                        var propertyOwnerType = GetType(attachedProp.Namespace, attachedProp.TypeName);
                        var avaloniaPropertyField = propertyOwnerType.GetAllFields().FirstOrDefault(f =>
                            f.IsStatic && f.IsPublic && f.Name == avaloniaPropertyFieldName);

                        if (avaloniaPropertyField is null)
                        {
                            throw new XamlTransformException(
                                $"Unable to find {avaloniaPropertyFieldName} field on type {propertyOwnerType.GetFullName()}",
                                lineInfo);
                        }

                        nodes.Add(new XamlIlAvaloniaPropertyPropertyPathElementNode(avaloniaPropertyField,
                            XamlIlAvaloniaPropertyHelper.GetAvaloniaPropertyType(avaloniaPropertyField, context.GetAvaloniaTypes(), lineInfo),
                            attachedProp.AcceptsNull));
                        break;
                    case BindingExpressionGrammar.SelfNode _:
                        nodes.Add(new SelfPathElementNode(selfType));
                        break;
                    case VisualAncestorBindingExpressionNode visualAncestor:
                        nodes.Add(new FindVisualAncestorPathElementNode(visualAncestor.Type, visualAncestor.Level));
                        break;
                    case TemplatedParentBindingExpressionNode templatedParent:
                        var templatedParentType = context
                            .ParentNodes()
                            .OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                            .Where(x => x.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate)
                            .FirstOrDefault()?.TargetType;

                        if (templatedParentType is null)
                        {
                            throw new XamlTransformException("A binding with a TemplatedParent RelativeSource has to be in a ControlTemplate.", lineInfo);
                        }

                        nodes.Add(new TemplatedParentPathElementNode(templatedParentType.GetClrType()));
                        break;
                    case BindingExpressionGrammar.AncestorNode ancestor:
                        var styledElement = context.GetAvaloniaTypes().StyledElement;
                        var ancestorTypeFilter = !(ancestor.Namespace is null && ancestor.TypeName is null) ? GetType(ancestor.Namespace, ancestor.TypeName) : null;

                        var ancestorNode = context
                            .ParentNodes()
                            .OfType<XamlAstConstructableObjectNode>()
                            .Where(x => styledElement.IsAssignableFrom(x.Type.GetClrType()))
                            .Skip(1)
                            .Where(x => ancestorTypeFilter is not null
                                ? ancestorTypeFilter.IsAssignableFrom(x.Type.GetClrType()) : true)
                            .ElementAtOrDefault(ancestor.Level);

                        IXamlType? dataContextType = null;
                        if (ancestorNode is not null)
                        {
                            var isSkipping = true;
                            foreach (var node in context.ParentNodes())
                            {
                                if (node == ancestorNode)
                                    isSkipping = false;
                                if (node is AvaloniaNameScopeRegistrationXamlIlNode)
                                    break;
                                if (!isSkipping && node is AvaloniaXamlIlDataContextTypeMetadataNode metadataNode)
                                {
                                    dataContextType = metadataNode.DataContextType;
                                    break;
                                }
                            }
                        }

                        // We need actual ancestor for a correct DataContextType,
                        // but since in current design bindings do a double-work by enumerating the tree,
                        // we want to keep original ancestor type filter, if it was present.
                        var bindingAncestorType = ancestorTypeFilter is not null
                            ? ancestorTypeFilter
                            : ancestorNode?.Type.GetClrType();

                        if (bindingAncestorType is null)
                        {
                            throw new XamlX.XamlTransformException("Unable to resolve implicit ancestor type based on XAML tree.", lineInfo);
                        }

                        nodes.Add(new FindAncestorPathElementNode(bindingAncestorType, ancestor.Level, dataContextType));
                        break;
                    case BindingExpressionGrammar.NameNode elementName:
                        IXamlType? elementType = null, dataType = null;
                        foreach (var deferredContent in context.ParentNodes().OfType<NestedScopeMetadataNode>())
                        {
                            (elementType, dataType) = ScopeRegistrationFinder.GetTargetType(deferredContent, elementName.Name) ?? default;
                            if (!(elementType is null))
                            {
                                break;
                            }
                        }
                        if (elementType is null)
                        {
                            (elementType, dataType) = ScopeRegistrationFinder.GetTargetType(context.ParentNodes().Last(), elementName.Name) ?? default;
                        }

                        if (elementType is null)
                        {
                            throw new XamlX.XamlTransformException($"Unable to find element '{elementName.Name}' in the current namescope. Unable to use a compiled binding with a name binding if the name cannot be found at compile time.", lineInfo);
                        }
                        nodes.Add(new ElementNamePathElementNode(elementName.Name, elementType, dataType));
                        break;
                    case BindingExpressionGrammar.TypeCastNode typeCastNode:
                        var castType = GetType(typeCastNode.Namespace, typeCastNode.TypeName);

                        if (castType is null)
                        {
                            throw new XamlX.XamlTransformException($"Unable to resolve cast to type {typeCastNode.Namespace}:{typeCastNode.TypeName} based on XAML tree.", lineInfo);
                        }

                        nodes.Add(new TypeCastPathElementNode(castType));
                        break;
                }
            }

            return new XamlIlBindingPathNode(lineInfo, context.GetAvaloniaTypes().CompiledBindingPath, transformNodes, nodes);

            IXamlType GetType(string? ns, string? name)
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

            static IEnumerable<IXamlMethod> GetAllDefinedMethods(IXamlType type)
            {
                foreach (var t in TraverseTypeHierarchy(type))
                {
                    foreach (var m in t.Methods)
                    {
                        yield return m;
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

        private static void EmitPropertyCall(XamlIlEmitContext context, IXamlILEmitter codeGen, bool acceptsNull)
        {
            // By default use the 2-argument overload of CompiledBindingPathBuilder.Property,
            // unless a "?." null conditional operator appears in the path, in which case use
            // the 3-parameter version with the `acceptsNull` parameter. This ensures we don't
            // get a missing method exception if we run against an old version of Avalonia.
            var methodArgumentCount = 2;

            if (acceptsNull)
            {
                methodArgumentCount = 3;
                codeGen.Ldc_I4(1);
            }

            codeGen
                .EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.GetMethod(m =>
                        m.Name == "Property" &&
                        m.Parameters.Count == methodArgumentCount));
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

            IXamlType? TargetType { get; set; }
            IXamlType? DataContextType { get; set; }

            public static (IXamlType Target, IXamlType? DataContextType)? GetTargetType(IXamlAstNode namescopeRoot, string name)
            {
                // If we start from the nested scope - skip it.
                if (namescopeRoot is NestedScopeMetadataNode scope)
                {
                    namescopeRoot = scope.Value;
                }
                
                var finder = new ScopeRegistrationFinder(name);
                namescopeRoot.Visit(finder);
                return finder.TargetType is not null ? (finder.TargetType, finder.DataContextType) : null;
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
                // Ignore name registrations, if we are inside of the nested namescope.
                if (_childScopesStack.Count == 0)
                {
                    if (node is AvaloniaNameScopeRegistrationXamlIlNode registration
                        && registration.Name is XamlAstTextNode text && text.Text == Name)
                    {
                        TargetType = registration.TargetType;
                    }
                    // We are visiting nodes top to bottom.
                    // If we have already found target type by its name,
                    // it means all next nodes will be below, and not applicable for data context inheritance.
                    else if (TargetType is null && node is AvaloniaXamlIlDataContextTypeMetadataNode dataContextTypeMetadata)
                    {
                        DataContextType = dataContextTypeMetadata.DataContextType;
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

        interface IXamlIlBindingPathNodeWithDataContextType
        {
            IXamlType? DataContextType { get; }
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
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "Not"));
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
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "StreamObservable").MakeGenericMethod(new[] { Type }));
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
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "StreamTask").MakeGenericMethod(new[] { Type }));
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
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "Self"));
            }
        }

        class FindAncestorPathElementNode : IXamlIlBindingPathElementNode, IXamlIlBindingPathNodeWithDataContextType
        {
            private readonly int _level;

            public FindAncestorPathElementNode(IXamlType ancestorType, int level, IXamlType? dataContextType)
            {
                Type = ancestorType;
                _level = level;
                DataContextType = dataContextType;
            }

            public IXamlType Type { get; }
            public IXamlType? DataContextType { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.Ldtype(Type)
                    .Ldc_I4(_level)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "Ancestor"));
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
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "VisualAncestor"));
            }
        }

        class ElementNamePathElementNode : IXamlIlBindingPathElementNode, IXamlIlBindingPathNodeWithDataContextType
        {
            private readonly string _name;

            public ElementNamePathElementNode(string name, IXamlType elementType, IXamlType? dataType)
            {
                _name = name;
                Type = elementType;
                DataContextType = dataType;
            }

            public IXamlType Type { get; }
            public IXamlType? DataContextType { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                var scopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                    f.Name == AvaloniaXamlIlLanguage.ContextNameScopeFieldName);

                codeGen
                    .Ldloc(context.ContextLocal)
                    .Ldfld(scopeField)
                    .Ldstr(_name)
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "ElementName"));
            }
        }

        class TemplatedParentPathElementNode : IXamlIlBindingPathElementNode
        {
            public TemplatedParentPathElementNode(IXamlType elementType)
            {
                Type = elementType;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen
                    .EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "TemplatedParent"));
            }
        }

        class XamlIlAvaloniaPropertyPropertyPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlField _field;
            private readonly bool _acceptsNull;

            public XamlIlAvaloniaPropertyPropertyPathElementNode(IXamlField field, IXamlType propertyType, bool acceptsNull)
            {
                _field = field;
                Type = propertyType;
                _acceptsNull = acceptsNull;
            }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.Ldsfld(_field);
                context.Configuration.GetExtra<XamlIlPropertyInfoAccessorFactoryEmitter>()
                    .EmitLoadAvaloniaPropertyAccessorFactory(context, codeGen);

                EmitPropertyCall(context, codeGen, _acceptsNull);
            }

            public IXamlType Type { get; }
        }

        class XamlIlClrPropertyPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlProperty _property;
            private readonly bool _acceptsNull;

            public XamlIlClrPropertyPathElementNode(IXamlProperty property, bool acceptsNull)
            {
                _property = property;
                _acceptsNull = acceptsNull;
            }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                context.Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                    .Emit(context, codeGen, _property);

                context.Configuration.GetExtra<XamlIlPropertyInfoAccessorFactoryEmitter>()
                    .EmitLoadInpcPropertyAccessorFactory(context, codeGen);

                EmitPropertyCall(context, codeGen, _acceptsNull);
            }

            public IXamlType Type => _property.PropertyType;
        }

        class XamlIlClrMethodPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly bool _acceptsNull;

            public XamlIlClrMethodPathElementNode(IXamlMethod method, IXamlType systemDelegateType, bool acceptsNull)
            {
                Method = method;
                Type = systemDelegateType;
                _acceptsNull = acceptsNull;
            }
            public IXamlMethod Method { get; }

            public IXamlType Type { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
            [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "All Action<> and Func<> types are explicitly preserved.")]
            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                IXamlTypeBuilder<IXamlILEmitter>? newDelegateTypeBuilder = null;
                IXamlType specificDelegateType;
                if (Method.ReturnType == context.Configuration.WellKnownTypes.Void && Method.Parameters.Count == 0)
                {
                    specificDelegateType = context.Configuration.TypeSystem
                        .GetType("System.Action");
                }
                else if (Method.ReturnType == context.Configuration.WellKnownTypes.Void && Method.Parameters.Count <= 16)
                {
                    specificDelegateType = context.Configuration.TypeSystem
                        .GetType($"System.Action`{Method.Parameters.Count}")
                        .MakeGenericType(Method.Parameters);
                }
                else if (Method.Parameters.Count <= 16)
                {
                    List<IXamlType> genericParameters = new();
                    genericParameters.AddRange(Method.Parameters);
                    genericParameters.Add(Method.ReturnType);
                    specificDelegateType = context.Configuration.TypeSystem
                        .GetType($"System.Func`{Method.Parameters.Count + 1}")
                        .MakeGenericType(genericParameters);
                }
                else
                {
                    // In this case, we need to emit our own delegate type.
                    string delegateTypeName = context.Configuration.IdentifierGenerator.GenerateIdentifierPart();
                    specificDelegateType = newDelegateTypeBuilder = context.DeclaringType.DefineDelegateSubType(
                        delegateTypeName,
                        XamlVisibility.Private,
                        Method.ReturnType,
                        Method.Parameters);
                }

                codeGen
                    .Ldtoken(Method)
                    .Ldtoken(specificDelegateType);

                // By default use the 2-argument overload of CompiledBindingPathBuilder.Method,
                // unless a "?." null conditional operator appears in the path, in which case use
                // the 3-parameter version with the `acceptsNull` parameter. This ensures we don't
                // get a missing method exception if we run against an old version of Avalonia.
                var methodArgumentCount = 2;

                if (_acceptsNull)
                {
                    methodArgumentCount = 3;
                    codeGen.Ldc_I4(1);
                }

                codeGen.EmitCall(context.GetAvaloniaTypes()
                    .CompiledBindingPathBuilder.GetMethod(m => 
                        m.Name == "Method" &&
                        m.Parameters.Count == methodArgumentCount));


                newDelegateTypeBuilder?.CreateType();
            }
        }

        class XamlIlClrMethodAsCommandPathElementNode : IXamlIlBindingPathElementNode
        {
            private readonly IXamlMethod _executeMethod;
            private readonly IXamlMethod? _canExecuteMethod;
            private readonly IReadOnlyList<string> _dependsOnProperties;

            public XamlIlClrMethodAsCommandPathElementNode(
                IXamlType iCommandType,
                IXamlMethod executeMethod,
                IXamlMethod? canExecuteMethod,
                IReadOnlyList<string> dependsOnProperties)
            {
                Type = iCommandType;
                _executeMethod = executeMethod;
                _canExecuteMethod = canExecuteMethod;
                _dependsOnProperties = dependsOnProperties;
            }

            public IXamlType Type { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                var trampolineBuilder = context.Configuration.GetExtra<XamlIlTrampolineBuilder>();
                var objectType = context.Configuration.WellKnownTypes.Object;
                codeGen
                    .Ldstr(_executeMethod.Name)
                    .Ldnull()
                    .Ldftn(trampolineBuilder.EmitCommandExecuteTrampoline(context, _executeMethod))
                    .Newobj(context.Configuration.TypeSystem.GetType("System.Action`2")
                        .MakeGenericType(objectType, objectType)
                        .GetConstructor(new() { objectType, context.Configuration.TypeSystem.GetType("System.IntPtr") }));

                if (_canExecuteMethod is null)
                {
                    codeGen.Ldnull();
                }
                else
                {
                    codeGen
                        .Ldnull()
                        .Ldftn(trampolineBuilder.EmitCommandCanExecuteTrampoline(context, _canExecuteMethod))
                        .Newobj(context.Configuration.TypeSystem.GetType("System.Func`3")
                            .MakeGenericType(objectType, objectType, context.Configuration.WellKnownTypes.Boolean)
                            .GetConstructor(new() { objectType, context.Configuration.TypeSystem.GetType("System.IntPtr") }));
                }

                if (_dependsOnProperties is { Count:> 0 })
                {
                    using var dependsOnPropertiesArray = context.GetLocalOfType(context.Configuration.WellKnownTypes.String.MakeArrayType(1));
                    codeGen
                        .Ldc_I4(_dependsOnProperties.Count)
                        .Newarr(context.Configuration.WellKnownTypes.String)
                        .Stloc(dependsOnPropertiesArray.Local);

                    for (int i = 0; i < _dependsOnProperties.Count; i++)
                    {
                        codeGen
                            .Ldloc(dependsOnPropertiesArray.Local)
                            .Ldc_I4(i)
                            .Ldstr(_dependsOnProperties[i])
                            .Stelem_ref();
                    }
                    codeGen.Ldloc(dependsOnPropertiesArray.Local);
                }
                else
                {
                    codeGen.Ldnull();
                }

                codeGen
                    .EmitCall(context.GetAvaloniaTypes()
                        .CompiledBindingPathBuilder.GetMethod(m => m.Name == "Command"));
            }
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

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
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
                    .CompiledBindingPathBuilder.GetMethod(m => m.Name == "Property"));
            }

            public IXamlType Type => _property.PropertyType;
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
                        throw new XamlX.XamlTransformException($"Unable to convert '{item}' to an integer.", lineInfo);
                    }
                    _values.Add(index);
                }
            }

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
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
                    .CompiledBindingPathBuilder.GetMethod(m => m.Name == "ArrayElement"));
            }

            public IXamlType Type => _arrayType.ArrayElementType!;
        }

        class TypeCastPathElementNode : IXamlIlBindingPathElementNode
        {
            public TypeCastPathElementNode(IXamlType ancestorType)
            {
                Type = ancestorType;
            }

            public IXamlType Type { get; }

            public void Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                codeGen.EmitCall(context.GetAvaloniaTypes().CompiledBindingPathBuilder.GetMethod(m => m.Name == "TypeCast").MakeGenericMethod(new[] { Type }));
            }
        }

        class XamlIlBindingPathNode : XamlAstNode, IXamlIlBindingPathNode, IXamlAstLocalsEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
        {
            private readonly List<IXamlIlBindingPathElementNode> _transformElements;

            public XamlIlBindingPathNode(IXamlLineInfo lineInfo,
                IXamlType bindingPathType,
                List<IXamlIlBindingPathElementNode> transformElements,
                List<IXamlIlBindingPathElementNode> elements) : base(lineInfo)
            {
                Type = new XamlAstClrTypeReference(lineInfo, bindingPathType, false);
                _transformElements = transformElements;
                Elements = elements;
            }

            public IXamlType BindingResultType =>
                _transformElements.FirstOrDefault()?.Type
                    ?? Elements.LastOrDefault()?.Type
                    ?? XamlPseudoType.Unknown;

            public IXamlAstTypeReference Type { get; }

            public List<IXamlIlBindingPathElementNode> Elements { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
            public XamlILNodeEmitResult Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
            {
                var intType = context.Configuration.TypeSystem.GetType("System.Int32");
                var types = context.GetAvaloniaTypes();

                // We're calling the CompiledBindingPathBuilder(int apiVersion) with an apiVersion 
                // of 1 to indicate that we don't want TemplatedParent compatibility hacks enabled.
                codeGen
                    .Ldc_I4(1)
                    .Newobj(types.CompiledBindingPathBuilder.GetConstructor(new() { intType }));

                foreach (var transform in _transformElements)
                {
                    transform.Emit(context, codeGen);
                }

                foreach (var element in Elements)
                {
                    element.Emit(context, codeGen);
                }

                codeGen.EmitCall(types.CompiledBindingPathBuilder.GetMethod(m => m.Name == "Build"));
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
                for (int i = 0; i < Elements.Count; i++)
                {
                    if (Elements[i] is IXamlAstNode ast)
                    {
                        Elements[i] = (IXamlIlBindingPathElementNode)ast.Visit(visitor);
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
