using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class ArrayElementPlugin : IPropertyAccessorPlugin
    {
        private readonly int[] _indices;
        private readonly Type _elementType;

        public ArrayElementPlugin(int[] indices, Type elementType)
        {
            _indices = indices;
            _elementType = elementType;
        }

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The ArrayElementPlugin does not support dynamic matching");
        }

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
        {
            if (reference.TryGetTarget(out var target) && target is Array arr)
            {
                return new Accessor(new WeakReference<Array>(arr), _indices, _elementType);
            }
            return null;
        }

        class Accessor : PropertyAccessorBase
        {
            private readonly int[] _indices;
            private readonly WeakReference<Array> _reference;

            public Accessor(WeakReference<Array> reference, int[] indices, Type elementType)
            {
                _reference = reference;
                _indices = indices;
                PropertyType = elementType;
            }

            public override Type PropertyType { get; }

            public override object? Value => _reference.TryGetTarget(out var arr) ? arr.GetValue(_indices) : null;

            public override bool SetValue(object? value, BindingPriority priority)
            {
                if (_reference.TryGetTarget(out var arr))
                {
                    arr.SetValue(value, _indices);
                    return true;
                }
                return false;
            }

            protected override void SubscribeCore()
            {
                PublishValue(Value);
            }

            protected override void UnsubscribeCore()
            {
            }
        }
    }

}
