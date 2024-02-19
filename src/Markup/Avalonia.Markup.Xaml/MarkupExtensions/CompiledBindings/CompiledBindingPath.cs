﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Parsers;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    public class CompiledBindingPath
    {
        private readonly ICompiledBindingPathElement[] _elements;

        public CompiledBindingPath()
            => _elements = Array.Empty<ICompiledBindingPathElement>();

        internal CompiledBindingPath(ICompiledBindingPathElement[] elements, object? rawSource)
        {
            _elements = elements;
            RawSource = rawSource;
        }

        internal void BuildExpression(List<ExpressionNode> result, out bool isRooted)
        {
            var negated = 0;

            isRooted = false;

            foreach (var element in _elements)
            {
                ExpressionNode? node;
                switch (element)
                {
                    case NotExpressionPathElement:
                        ++negated;
                        node = null;
                        break;
                    case PropertyElement prop:
                        node = new PropertyAccessorNode(prop.Property.Name, new PropertyInfoAccessorPlugin(prop.Property, prop.AccessorFactory));
                        break;
                    case MethodAsCommandElement methodAsCommand:
                        node = new MethodCommandNode(
                            methodAsCommand.MethodName,
                            methodAsCommand.ExecuteMethod,
                            methodAsCommand.CanExecuteMethod,
                            methodAsCommand.DependsOnProperties);
                        break;
                    case MethodAsDelegateElement methodAsDelegate:
                        node = new PropertyAccessorNode(methodAsDelegate.Method.Name, new MethodAccessorPlugin(methodAsDelegate.Method, methodAsDelegate.DelegateType));
                        break;
                    case ArrayElementPathElement arr:
                        node = new ArrayIndexerNode(arr.Indices);
                        break;
                    case VisualAncestorPathElement visualAncestor:
                        node = new VisualAncestorElementNode(visualAncestor.AncestorType, visualAncestor.Level);
                        isRooted = true;
                        break;
                    case AncestorPathElement ancestor:
                        node = new LogicalAncestorElementNode(ancestor.AncestorType, ancestor.Level);
                        isRooted = true;
                        break;
                    case SelfPathElement:
                        node = null;
                        isRooted = true;
                        break;
                    case ElementNameElement name:
                        node = new NamedElementNode(name.NameScope, name.Name);
                        isRooted = true;
                        break;
                    case IStronglyTypedStreamElement stream:
                        node = new StreamNode(stream.CreatePlugin());
                        break;
                    case ITypeCastElement typeCast:
                        node = new FuncTransformNode(typeCast.Cast);
                        break;
                    case TemplatedParentPathElement:
                        node = new TemplatedParentNode();
                        isRooted = true;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown binding path element type {element.GetType().FullName}");
                }

                if (node is not null)
                    result.Add(node);
            }


            for (var i = 0; i < negated; ++i)
                result.Add(new LogicalNotNode());
        }

        internal IEnumerable<ICompiledBindingPathElement> Elements => _elements;

        internal SourceMode SourceMode => Array.Exists(_elements, e => e is IControlSourceBindingPathElement)
            ? SourceMode.Control : SourceMode.Data;

        internal object? RawSource { get; }

        /// <inheritdoc />
        public override string ToString()
            => string.Concat((IEnumerable<ICompiledBindingPathElement>) _elements);
    }

    public class CompiledBindingPathBuilder
    {
        private readonly int _apiVersion;
        private object? _rawSource;
        private readonly List<ICompiledBindingPathElement> _elements = new();

        public CompiledBindingPathBuilder()
        {
        }

        // TODO12: Remove this constructor. apiVersion is only needed for compatibility with
        // versions of Avalonia which used $self.Property() for building TemplatedParent bindings.
        public CompiledBindingPathBuilder(int apiVersion) => _apiVersion = apiVersion;

        public CompiledBindingPathBuilder Not()
        {
            _elements.Add(new NotExpressionPathElement());
            return this;
        }

        public CompiledBindingPathBuilder Property(IPropertyInfo info, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory)
        {
            // Older versions of Avalonia used $self.Property() for building TemplatedParent bindings.
            // Try to detect this and upgrade to using a TemplatedParentPathElement so that logging works
            // correctly.
            if (_apiVersion == 0 && 
                info.Name == "TemplatedParent" && 
                _elements.Count >= 1 &&
                _elements[_elements.Count - 1] is SelfPathElement)
            {
                _elements.Add(new TemplatedParentPathElement());
            }
            else
            {
                _elements.Add(new PropertyElement(info, accessorFactory, _elements.Count == 0));
            }

            return this;
        }

        public CompiledBindingPathBuilder Method(RuntimeMethodHandle handle, RuntimeTypeHandle delegateType)
        {
            _elements.Add(new MethodAsDelegateElement(handle, delegateType));
            return this;
        }

        public CompiledBindingPathBuilder Command(string methodName, Action<object, object?> executeHelper, Func<object, object?, bool>? canExecuteHelper, string[]? dependsOnProperties)
        {
            _elements.Add(new MethodAsCommandElement(methodName, executeHelper, canExecuteHelper, dependsOnProperties ?? Array.Empty<string>()));
            return this;
        }

        public CompiledBindingPathBuilder StreamTask<T>()
        {
            _elements.Add(new TaskStreamPathElement<T>());
            return this;
        }

        public CompiledBindingPathBuilder StreamObservable<T>()
        {
            _elements.Add(new ObservableStreamPathElement<T>());
            return this;
        }

        public CompiledBindingPathBuilder Self()
        {
            _elements.Add(new SelfPathElement());
            return this;
        }

        public CompiledBindingPathBuilder Ancestor(Type ancestorType, int level)
        {
            _elements.Add(new AncestorPathElement(ancestorType, level));
            return this;
        }

        public CompiledBindingPathBuilder VisualAncestor(Type ancestorType, int level)
        {
            _elements.Add(new VisualAncestorPathElement(ancestorType, level));
            return this;
        }

        public CompiledBindingPathBuilder ElementName(INameScope nameScope, string name)
        {
            _elements.Add(new ElementNameElement(nameScope, name));
            return this;
        }

        public CompiledBindingPathBuilder ArrayElement(int[] indices, Type elementType)
        {
            _elements.Add(new ArrayElementPathElement(indices, elementType));
            return this;
        }

        public CompiledBindingPathBuilder TypeCast<T>()
        {
            _elements.Add(new TypeCastPathElement<T>());
            return this;
        }

        public CompiledBindingPathBuilder TemplatedParent()
        {
            _elements.Add(new TemplatedParentPathElement());
            return this;
        }

        public CompiledBindingPathBuilder SetRawSource(object? rawSource)
        {
            _rawSource = rawSource;
            return this;
        }

        public CompiledBindingPath Build() => new CompiledBindingPath(_elements.ToArray(), _rawSource);
    }

    internal interface ICompiledBindingPathElement
    {
    }

    internal interface IControlSourceBindingPathElement { }

    internal class NotExpressionPathElement : ICompiledBindingPathElement
    {
        public static readonly NotExpressionPathElement Instance = new NotExpressionPathElement();
    }

    internal class PropertyElement : ICompiledBindingPathElement
    {
        private readonly bool _isFirstElement;

        public PropertyElement(IPropertyInfo property, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory, bool isFirstElement)
        {
            Property = property;
            AccessorFactory = accessorFactory;
            _isFirstElement = isFirstElement;
        }

        public IPropertyInfo Property { get; }

        public Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> AccessorFactory { get; }

        public override string ToString()
            => _isFirstElement ? Property.Name : $".{Property.Name}";
    }

    internal class MethodAsDelegateElement : ICompiledBindingPathElement
    {
        public MethodAsDelegateElement(RuntimeMethodHandle method, RuntimeTypeHandle delegateType)
        {
            Method = MethodBase.GetMethodFromHandle(method) as MethodInfo
                ?? throw new ArgumentException("Invalid method handle", nameof(method));
            DelegateType = Type.GetTypeFromHandle(delegateType)
                ?? throw new ArgumentException("Unexpected null returned from Type.GetTypeFromHandle in MethodAsDelegateElement");
        }

        public MethodInfo Method { get; }

        public Type DelegateType { get; }
    }

    internal class MethodAsCommandElement : ICompiledBindingPathElement
    {
        public MethodAsCommandElement(string methodName, Action<object, object?> executeHelper, Func<object, object?, bool>? canExecuteHelper, string[] dependsOnElements)
        {
            MethodName = methodName;
            ExecuteMethod = executeHelper;
            CanExecuteMethod = canExecuteHelper;
            DependsOnProperties = new HashSet<string>(dependsOnElements);
        }

        public string MethodName { get; }
        public Action<object, object?> ExecuteMethod { get; }
        public Func<object, object?, bool>? CanExecuteMethod { get; }
        public HashSet<string> DependsOnProperties { get; }
    }

    internal interface IStronglyTypedStreamElement : ICompiledBindingPathElement
    {
        IStreamPlugin CreatePlugin();
    }

    internal interface ITypeCastElement : ICompiledBindingPathElement
    {
        Type Type { get; }

        Func<object?, object?> Cast { get; }
    }

    internal class TaskStreamPathElement<T> : IStronglyTypedStreamElement
    {
        public static readonly TaskStreamPathElement<T> Instance = new TaskStreamPathElement<T>();

        public IStreamPlugin CreatePlugin() => new TaskStreamPlugin<T>();
    }

    internal class ObservableStreamPathElement<T> : IStronglyTypedStreamElement
    {
        public static readonly ObservableStreamPathElement<T> Instance = new ObservableStreamPathElement<T>();

        public IStreamPlugin CreatePlugin() => new ObservableStreamPlugin<T>();
    }

    internal class SelfPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public static readonly SelfPathElement Instance = new SelfPathElement();

        public override string ToString()
            => "$self";
    }

    internal class AncestorPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public AncestorPathElement(Type? ancestorType, int level)
        {
            AncestorType = ancestorType;
            Level = level;
        }

        public Type? AncestorType { get; }
        public int Level { get; }

        public override string ToString()
           => FormattableString.Invariant($"$parent[{AncestorType?.Name},{Level}]");
    }

    internal class VisualAncestorPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public VisualAncestorPathElement(Type? ancestorType, int level)
        {
            AncestorType = ancestorType;
            Level = level;
        }

        public Type? AncestorType { get; }
        public int Level { get; }
    }

    internal class ElementNameElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public ElementNameElement(INameScope nameScope, string name)
        {
            NameScope = nameScope;
            Name = name;
        }

        public INameScope NameScope { get; }
        public string Name { get; }

        public override string ToString()
            => $"#{Name}";
    }

    internal class TemplatedParentPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public override string ToString()
            => $"$templatedParent";
    }

    internal class ArrayElementPathElement : ICompiledBindingPathElement
    {
        public ArrayElementPathElement(int[] indices, Type elementType)
        {
            Indices = indices;
            ElementType = elementType;
        }

        public int[] Indices { get; }
        public Type ElementType { get; }
        public override string ToString()
            => FormattableString.Invariant($"[{string.Join(",", Indices)}]");
    }

    internal class TypeCastPathElement<T> : ITypeCastElement
    {
        private static object? TryCast(object? obj)
        {
            if (obj is T result)
                return result;
            return null;
        }

        public Type Type => typeof(T);

        public Func<object?, object?> Cast => TryCast;

        public override string ToString()
            => $"({Type.FullName})";
    }
}
