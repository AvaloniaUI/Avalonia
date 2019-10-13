using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Parsers.Nodes;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    public class CompiledBindingPath
    {
        private readonly List<ICompiledBindingPathElement> _elements = new List<ICompiledBindingPathElement>();

        public CompiledBindingPath() { }

        internal CompiledBindingPath(IEnumerable<ICompiledBindingPathElement> bindingPath, object rawSource)
        {
            _elements = new List<ICompiledBindingPathElement>(bindingPath);
            RawSource = rawSource;
        }

        public ExpressionNode BuildExpression(bool enableValidation)
        {
            ExpressionNode pathRoot = null;
            ExpressionNode path = null;
            foreach (var element in _elements)
            {
                ExpressionNode node = null;
                switch (element)
                {
                    case NotExpressionPathElement _:
                        node = new LogicalNotNode();
                        break;
                    case PropertyElement prop:
                        node = new PropertyAccessorNode(prop.Property.Name, enableValidation, new PropertyInfoAccessorPlugin(prop.Property, prop.AccessorFactory));
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
                    case SelfPathElement _:
                        node = new SelfNode();
                        break;
                    case ElementNameElement name:
                        node = new ElementNameNode(name.NameScope, name.Name);
                        break;
                    case IStronglyTypedStreamElement stream:
                        node = new StreamNode(stream.CreatePlugin());
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown binding path element type {element.GetType().FullName}");
                }

                path = pathRoot is null ? (pathRoot = node) : path.Next = node;
            }

            return pathRoot ?? new EmptyExpressionNode();
        }

        internal SourceMode SourceMode => _elements.Count > 0 && _elements[0] is IControlSourceBindingPathElement ? SourceMode.Control : SourceMode.Data;

        internal object RawSource { get; }
    }

    public class CompiledBindingPathBuilder
    {
        private object _rawSource;
        private List<ICompiledBindingPathElement> _elements = new List<ICompiledBindingPathElement>();

        public CompiledBindingPathBuilder Not()
        {
            _elements.Add(new NotExpressionPathElement());
            return this;
        }

        public CompiledBindingPathBuilder Property(IPropertyInfo info, Func<WeakReference<object>, IPropertyInfo, IPropertyAccessor> accessorFactory)
        {
            _elements.Add(new PropertyElement(info, accessorFactory));
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

        public CompiledBindingPathBuilder SetRawSource(object rawSource)
        {
            _rawSource = rawSource;
            return this;
        }

        public CompiledBindingPath Build() => new CompiledBindingPath(_elements, _rawSource);
    }

    public interface ICompiledBindingPathElement
    {
    }

    internal interface IControlSourceBindingPathElement { }

    internal class NotExpressionPathElement : ICompiledBindingPathElement
    {
        public static readonly NotExpressionPathElement Instance = new NotExpressionPathElement();
    }

    internal class PropertyElement : ICompiledBindingPathElement
    {
        public PropertyElement(IPropertyInfo property, Func<WeakReference<object>, IPropertyInfo, IPropertyAccessor> accessorFactory)
        {
            Property = property;
            AccessorFactory = accessorFactory;
        }

        public IPropertyInfo Property { get; }

        public Func<WeakReference<object>, IPropertyInfo, IPropertyAccessor> AccessorFactory { get; }
    }

    internal interface IStronglyTypedStreamElement : ICompiledBindingPathElement
    {
        IStreamPlugin CreatePlugin();
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
    }

    internal class AncestorPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public AncestorPathElement(Type ancestorType, int level)
        {
            AncestorType = ancestorType;
            Level = level;
        }

        public Type AncestorType { get; }
        public int Level { get; }
    }

    internal class VisualAncestorPathElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public VisualAncestorPathElement(Type ancestorType, int level)
        {
            AncestorType = ancestorType;
            Level = level;
        }

        public Type AncestorType { get; }
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
    }
}
