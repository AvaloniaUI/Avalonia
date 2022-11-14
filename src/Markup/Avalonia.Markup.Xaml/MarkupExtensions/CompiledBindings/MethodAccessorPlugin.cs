using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

#nullable enable

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class MethodAccessorPlugin : IPropertyAccessorPlugin
    {
        private MethodInfo _method;
        private readonly Type _delegateType;

        public MethodAccessorPlugin(MethodInfo method, Type delegateType)
        {
            _method = method;
            _delegateType = delegateType;
        }

        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The MethodAccessorPlugin does not support dynamic matching");
        }

        public IPropertyAccessor Start(WeakReference<object?> reference, string propertyName)
        {
            Debug.Assert(_method.Name == propertyName);
            return new Accessor(reference, _method, _delegateType);
        }

        private sealed class Accessor : PropertyAccessorBase
        {
            public Accessor(WeakReference<object?> reference, MethodInfo method, Type delegateType)
            {
                _ = reference ?? throw new ArgumentNullException(nameof(reference));
                _ = method ?? throw new ArgumentNullException(nameof(method));

                PropertyType = delegateType;

                if (method.IsStatic)
                {
                    Value = method.CreateDelegate(PropertyType);
                }
                else if (reference.TryGetTarget(out var target))
                {
                    Value = method.CreateDelegate(PropertyType, target);
                }
            }

            public override Type? PropertyType { get; }

            public override object? Value { get; }

            public override bool SetValue(object? value, BindingPriority priority) => false;

            protected override void SubscribeCore()
            {
                try
                {
                    PublishValue(Value);
                }
                catch { }
            }

            protected override void UnsubscribeCore()
            {
            }
        }
    }
}
