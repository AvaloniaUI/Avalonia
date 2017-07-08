// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Utilities;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Reads a property from a standard C# object that optionally supports the
    /// <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    public class InpcPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <inheritdoc/>
        public bool Match(object obj, string propertyName) => true;

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor Start(WeakReference reference, string propertyName)
        {
            Contract.Requires<ArgumentNullException>(reference != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);

            var instance = reference.Target;
            var p = instance.GetType().GetRuntimeProperties().FirstOrDefault(x => x.Name == propertyName);

            if (p != null)
            {
                return new Accessor(reference, p);
            }
            else
            {
                var message = $"Could not find CLR property '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        private class Accessor : PropertyAccessorBase, IWeakSubscriber<PropertyChangedEventArgs>
        {
            private readonly WeakReference _reference;
            private readonly PropertyInfo _property;
            private bool _eventRaised;

            public Accessor(WeakReference reference,  PropertyInfo property)
            {
                Contract.Requires<ArgumentNullException>(reference != null);
                Contract.Requires<ArgumentNullException>(property != null);

                _reference = reference;
                _property = property;
            }

            public override Type PropertyType => _property.PropertyType;

            public override object Value
            {
                get
                {
                    var o = _reference.Target;
                    return (o != null) ? _property.GetValue(o) : null;
                }
            }

            public override bool SetValue(object value, BindingPriority priority)
            {
                if (_property.CanWrite)
                {
                    _eventRaised = false;
                    _property.SetValue(_reference.Target, value);

                    if (!_eventRaised)
                    {
                        SendCurrentValue();
                    }

                    return true;
                }

                return false;
            }

            void IWeakSubscriber<PropertyChangedEventArgs>.OnEvent(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
                {
                    _eventRaised = true;
                    SendCurrentValue();
                }
            }

            protected override void Dispose(bool disposing)
            {
                var inpc = _reference.Target as INotifyPropertyChanged;

                if (inpc != null)
                {
                    WeakSubscriptionManager.Unsubscribe(
                        inpc,
                        nameof(inpc.PropertyChanged),
                        this);
                }
            }

            protected override void SubscribeCore(IObserver<object> observer)
            {
                SendCurrentValue();
                SubscribeToChanges();
            }

            private void SendCurrentValue()
            {
                try
                {
                    var value = Value;
                    Observer.OnNext(value);
                }
                catch { }
            }

            private void SubscribeToChanges()
            {
                var inpc = _reference.Target as INotifyPropertyChanged;

                if (inpc != null)
                {
                    WeakSubscriptionManager.Subscribe(
                        inpc,
                        nameof(inpc.PropertyChanged),
                        this);
                }
            }
        }
    }
}
