using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use BindingPath.")]
    public class CompiledBindingPath : BindingPath
    {
        internal CompiledBindingPath(bool isSelf, List<ExpressionNode> nodes) : base(isSelf, nodes) { }
    }

    public class CompiledBindingPathBuilder
    {
        private readonly List<ExpressionNode> _elements = [];
        private bool _isSelf;
        private int _notCount;

        public CompiledBindingPathBuilder()
        {
        }

        [Obsolete("This method only exists for compatibility with XAML compiled for Avalonia 11.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CompiledBindingPathBuilder(int apiVersion)
        {
        }

        public CompiledBindingPathBuilder Not()
        {
            ++_notCount;
            return this;
        }

        public CompiledBindingPathBuilder Property(IPropertyInfo info, Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory)
        {
            return Property(info, accessorFactory, acceptsNull: false);
        }

        public CompiledBindingPathBuilder Property(
            IPropertyInfo info,
            Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory,
            bool acceptsNull)
        {
            _elements.Add(new PropertyAccessorNode(
                info.Name,
                new PropertyInfoAccessorPlugin(info, accessorFactory), 
                acceptsNull));
            return this;
        }

        public CompiledBindingPathBuilder Method(RuntimeMethodHandle handle, RuntimeTypeHandle delegateType)
        {
            Method(handle, delegateType, acceptsNull: false);
            return this;
        }

        public CompiledBindingPathBuilder Method(
            RuntimeMethodHandle handle,
            RuntimeTypeHandle delegateType,
            bool acceptsNull)
        {
            var method = MethodBase.GetMethodFromHandle(handle) as MethodInfo
                ?? throw new ArgumentException("Invalid method handle.", nameof(handle));
            var type = Type.GetTypeFromHandle(delegateType)
                ?? throw new ArgumentException("Unexpected null returned from 'Type.GetTypeFromHandle'.");
            var plugin = new MethodAccessorPlugin(method, type);
            _elements.Add(new PropertyAccessorNode(method.Name, plugin, acceptsNull));
            return this;
        }

        public CompiledBindingPathBuilder Command(string methodName, Action<object, object?> executeHelper, Func<object, object?, bool>? canExecuteHelper, string[]? dependsOnProperties)
        {
            _elements.Add(new MethodCommandNode(methodName, executeHelper, canExecuteHelper, new HashSet<string>(dependsOnProperties ?? [])));
            return this;
        }

        public CompiledBindingPathBuilder StreamTask<T>()
        {
            _elements.Add(new StreamNode(new TaskStreamPlugin<T>()));
            return this;
        }

        public CompiledBindingPathBuilder StreamObservable<T>()
        {
            _elements.Add(new StreamNode(new ObservableStreamPlugin<T>()));
            return this;
        }

        public CompiledBindingPathBuilder Self()
        {
            _isSelf = true;
            return this;
        }

        public CompiledBindingPathBuilder Ancestor(Type ancestorType, int level)
        {
            _elements.Add(new LogicalAncestorElementNode(ancestorType, level));
            return this;
        }

        public CompiledBindingPathBuilder VisualAncestor(Type ancestorType, int level)
        {
            _elements.Add(new VisualAncestorElementNode(ancestorType, level));
            return this;
        }

        public CompiledBindingPathBuilder ElementName(INameScope nameScope, string name)
        {
            _elements.Add(new NamedElementNode(nameScope, name));
            return this;
        }

        public CompiledBindingPathBuilder ArrayElement(int[] indices, Type elementType)
        {
            _elements.Add(new ArrayIndexerNode(indices));
            return this;
        }

        public CompiledBindingPathBuilder TypeCast<T>()
        {
            static object? TryCast(object? obj)
            {
                if (obj is T result)
                    return result;
                return null;
            }

            _elements.Add(new FuncTransformNode(TryCast));
            return this;
        }

        public CompiledBindingPathBuilder TemplatedParent()
        {
            _elements.Add(new TemplatedParentNode());
            return this;
        }

        public CompiledBindingPath Build()
        {
            for (var i = 0; i < _notCount; ++i)
                _elements.Add(new LogicalNotNode());
            return new(_isSelf, _elements);
        }
    }
}
