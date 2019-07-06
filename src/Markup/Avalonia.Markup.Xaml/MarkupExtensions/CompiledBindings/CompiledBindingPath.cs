extern alias Markup;
using System;
using System.Collections.Generic;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Parsers.Nodes;
using SourceMode = Markup::Avalonia.Markup.Parsers.SourceMode;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    public class CompiledBindingPath
    {
        private readonly List<ICompiledBindingPathElement> _elements = new List<ICompiledBindingPathElement>();

        public CompiledBindingPath() { }

        internal CompiledBindingPath(IEnumerable<ICompiledBindingPathElement> bindingPath)
        {
            _elements = new List<ICompiledBindingPathElement>(bindingPath);
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
                        node = new PropertyAccessorNode(prop.Property.Name, enableValidation, new PropertyInfoAccessorPlugin(prop.Property));
                        break;
                    case AncestorPathElement ancestor:
                        node = new FindAncestorNode(ancestor.AncestorType, ancestor.Level);
                        break;
                    case SelfPathElement _:
                        node = new SelfNode();
                        break;
                    case ElementNameElement name:
                        node = new ElementNameNode(name.Name);
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

        public SourceMode SourceMode => _elements.Count > 0 && _elements[0] is IControlSourceBindingPathElement ? SourceMode.Control : SourceMode.Data;
    }

    public class CompiledBindingPathBuilder
    {
        private List<ICompiledBindingPathElement> _elements = new List<ICompiledBindingPathElement>();

        public CompiledBindingPathBuilder Not()
        {
            _elements.Add(new NotExpressionPathElement());
            return this;
        }

        public CompiledBindingPathBuilder Property(INotifyingPropertyInfo info)
        {
            _elements.Add(new PropertyElement(info));
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

        public CompiledBindingPathBuilder ElementName(string name)
        {
            _elements.Add(new ElementNameElement(name));
            return this;
        }

        public CompiledBindingPath Build() => new CompiledBindingPath(_elements);
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
        public PropertyElement(INotifyingPropertyInfo property)
        {
            Property = property;
        }

        public INotifyingPropertyInfo Property { get; }
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

    internal class ElementNameElement : ICompiledBindingPathElement, IControlSourceBindingPathElement
    {
        public ElementNameElement(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
