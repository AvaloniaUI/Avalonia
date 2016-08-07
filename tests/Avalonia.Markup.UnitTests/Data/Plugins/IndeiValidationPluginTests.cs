// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Markup.Data.Plugins;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data.Plugins
{
    public class IndeiValidationPluginTests
    {
        [Fact]
        public void Produces_BindingNotifications()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new IndeiValidationPlugin();
            var data = new Data { Maximum = 5 };
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.Value));
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.Value), accessor);
            var result = new List<object>();

            validator.Subscribe(x => result.Add(x));
            validator.SetValue(5, BindingPriority.LocalValue);
            validator.SetValue(6, BindingPriority.LocalValue);
            data.Maximum = 10;
            data.Maximum = 5;

            Assert.Equal(new[]
            {
                new BindingNotification(0),
                new BindingNotification(5),

                // Value is first signalled without an error as validation hasn't been updated.
                new BindingNotification(6),
                
                // Then the ErrorsChanged event is fired.
                new BindingNotification(new Exception("Must be less than Maximum"), BindingErrorType.DataValidationError, 6),

                // Maximum is changed to 10 so value is now valid.
                new BindingNotification(6),

                // And Maximum is changed back to 5.
                new BindingNotification(new Exception("Must be less than Maximum"), BindingErrorType.DataValidationError, 6),
            }, result);
        }

        [Fact]
        public void Subscribes_And_Unsubscribes()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new IndeiValidationPlugin();
            var data = new Data { Maximum = 5 };
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.Value));
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.Value), accessor);

            Assert.Equal(0, data.SubscriptionCount);
            var sub = validator.Subscribe(_ => { });
            Assert.Equal(1, data.SubscriptionCount);
            sub.Dispose();
            Assert.Equal(0, data.SubscriptionCount);
        }

        public class Data : INotifyDataErrorInfo, INotifyPropertyChanged
        {
            private int _value;
            private int _maximum;
            private string _error;
            private EventHandler<DataErrorsChangedEventArgs> _errorsChanged;

            public bool HasErrors => _error != null;
            public int SubscriptionCount { get; private set; }

            public int Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    UpdateError();
                }
            }

            public int Maximum
            {
                get { return _maximum; }
                set
                {
                    _maximum = value;
                    UpdateError();
                }
            }

            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged
            {
                add { _errorsChanged += value; ++SubscriptionCount; }
                remove { _errorsChanged -= value; --SubscriptionCount; }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public IEnumerable GetErrors(string propertyName)
            {
                if (propertyName == nameof(Value) && _error != null)
                {
                    return new[] { _error };
                }

                return null;
            }

            private void UpdateError()
            {
                if (_value <= _maximum)
                {
                    if (_error != null)
                    {
                        _error = null;
                        _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
                    }
                }
                else
                {
                    if (_error == null)
                    {
                        _error = "Must be less than Maximum";
                        _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
                    }
                }
            }
        }
    }
}
