using System;
using System.Diagnostics;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins;

internal class MethodAccessorPlugin : IPropertyAccessorPlugin
{
    private readonly MethodInfo _method;
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
