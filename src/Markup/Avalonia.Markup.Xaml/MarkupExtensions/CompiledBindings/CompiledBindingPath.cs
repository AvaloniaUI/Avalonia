using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Parsers.Nodes;

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

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.CompiledBindingSafeSupressWarningMessage)]
        internal ExpressionNode BuildExpression(bool enableValidation)
        {
            ExpressionNode? pathRoot = null;
            ExpressionNode? path = null;
            foreach (var element in _elements)
            {
                ExpressionNode? node;
                switch (element)
                {
                    case NotExpressionPathElement:
                        node = new LogicalNotNode();
                        break;
                    case PropertyElement prop:
                        node = new PropertyAccessorNode(prop.Property.Name, enableValidation, new PropertyInfoAccessorPlugin(prop.Property, prop.AccessorFactory));
                        break;
                    case MethodAsCommandElement methodAsCommand:
                        node = new PropertyAccessorNode(methodAsCommand.MethodName, enableValidation, new CommandAccessorPlugin(methodAsCommand.ExecuteMethod, methodAsCommand.CanExecuteMethod, methodAsCommand.DependsOnProperties));
                        break;
                    case MethodAsDelegateElement methodAsDelegate:
                        node = new PropertyAccessorNode(methodAsDelegate.Method.Name, enableValidation, new MethodAccessorPlugin(methodAsDelegate.Method, methodAsDelegate.DelegateType));
                        break;
                    case ArrayElementPathElement arr:
                        node = new PropertyAccessorNode(CommonPropertyNames.IndexerName, enableValidation, new ArrayElementPlugin(arr.Indices, arr.ElementType));
                        break;
                    case VisualAncestorPathElement visualAncestor:
                        node = new FindVisualAncestorNode(visualAncestor.AncestorType, visualAncestor.Level);
                        break;
                    case AncestorPathElement ancestor:
                        node = new FindAncestorNode(ancestor.AncestorType, ancestor.Level);
                        break;
                    case SelfPathElement:
                        node = new SelfNode();
                        break;
                    case ElementNameElement name:
                        node = new ElementNameNode(name.NameScope, name.Name);
                        break;
                    case IStronglyTypedStreamElement stream:
                        node = new StreamNode(stream.CreatePlugin());
                        break;
                    case ITypeCastElement typeCast:
                        node = new StrongTypeCastNode(typeCast.Type, typeCast.Cast);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown binding path element type {element.GetType().FullName}");
                }

                path = pathRoot is null ? (pathRoot = node) : path!.Next = node;
            }

            return pathRoot ?? new EmptyExpressionNode();
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
        private object? _rawSource;
        private readonly List<ICompiledBindingPathElement> _elements = new();

        public CompiledBindingPathBuilder Not()
        {
            _elements.Add(new NotExpressionPathElement());
            return this;
        }

        public CompiledBindingPathBuilder Property(IPropertyInfo info, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory)
        {
            _elements.Add(new PropertyElement(info, accessorFactory, _elements.Count == 0));
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
            DelegateType = Type.GetTypeFromHandle(delegateType);
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
