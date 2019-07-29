using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    class ArrayElementPlugin : IPropertyAccessorPlugin
    {
        private readonly int[] _indices;
        private readonly Type _elementType;

        public ArrayElementPlugin(int[] indices, Type elementType)
        {
            _indices = indices;
            _elementType = elementType;
        }

        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The ArrayElementPlugin does not support dynamic matching");
        }

        public IPropertyAccessor Start(WeakReference reference, string propertyName)
        {
            return new Accessor(reference, _indices, _elementType);
        }

        class Accessor : PropertyAccessorBase
        {
            private readonly int[] _indices;
            private readonly WeakReference _reference;

            public Accessor(WeakReference reference, int[] indices, Type elementType)
            {
                _reference = reference;
                _indices = indices;
                PropertyType = elementType;
            }

            public override Type PropertyType { get; }

            public override object Value => _reference.Target is Array arr ? arr.GetValue(_indices) : null;

            public override bool SetValue(object value, BindingPriority priority)
            {
                if (_reference.Target is Array arr)
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
