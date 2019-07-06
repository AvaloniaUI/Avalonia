using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    class PropertyInfoAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly INotifyingPropertyInfo _propertyInfo;

        public PropertyInfoAccessorPlugin(INotifyingPropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The PropertyInfoAccessorPlugin does not support dynamic matching");
        }

        public IPropertyAccessor Start(WeakReference reference, string propertyName)
        {
            Debug.Assert(_propertyInfo.Name == propertyName);
            return new Accessor(reference, _propertyInfo);
        }

        class Accessor : PropertyAccessorBase
        {
            private WeakReference _reference;
            private INotifyingPropertyInfo _propertyInfo;
            private bool _eventRaised;

            public Accessor(WeakReference reference, INotifyingPropertyInfo propertyInfo)
            {
                _reference = reference;
                _propertyInfo = propertyInfo;
            }

            public override Type PropertyType => _propertyInfo.PropertyType;

            public override object Value
            {
                get
                {
                    var o = _reference.Target;
                    return (o != null) ? _propertyInfo.Get(o) : null;
                }
            }

            public override bool SetValue(object value, BindingPriority priority)
            {
                if (_propertyInfo.CanSet)
                {
                    _eventRaised = false;
                    _propertyInfo.Set(_reference.Target, value);

                    if (!_eventRaised)
                    {
                        SendCurrentValue();
                    }

                    return true;
                }

                return false;
            }

            void OnChanged(object sender, EventArgs e)
            {
                _eventRaised = true;
                SendCurrentValue();
            }

            protected override void SubscribeCore()
            {
                SendCurrentValue();
                SubscribeToChanges();
            }

            protected override void UnsubscribeCore()
            {
                var target = _reference.Target;
                if (target != null)
                {
                    _propertyInfo.RemoveListener(target, OnChanged); 
                }
            }

            private void SendCurrentValue()
            {
                try
                {
                    var value = Value;
                    PublishValue(value);
                }
                catch { }
            }

            private void SubscribeToChanges()
            {
                var target = _reference.Target;
                if (target != null)
                {
                    _propertyInfo.OnPropertyChanged(target, OnChanged); 
                }
            }
        }
    }
}
