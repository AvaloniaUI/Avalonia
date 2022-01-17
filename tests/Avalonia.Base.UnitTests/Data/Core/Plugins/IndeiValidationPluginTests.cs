using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Plugins
{
    public class IndeiValidationPluginTests
    {
        [Fact]
        public void Produces_BindingNotifications()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new IndeiValidationPlugin();
            var data = new Data { Maximum = 5 };
            var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.Value));
            var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.Value), accessor);
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
                new BindingNotification(new DataValidationException("Must be less than Maximum"), BindingErrorType.DataValidationError, 6),

                // Maximum is changed to 10 so value is now valid.
                new BindingNotification(6),

                // And Maximum is changed back to 5.
                new BindingNotification(new DataValidationException("Must be less than Maximum"), BindingErrorType.DataValidationError, 6),
            }, result);
        }

        [Fact]
        public void Subscribes_And_Unsubscribes()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new IndeiValidationPlugin();
            var data = new Data { Maximum = 5 };
            var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.Value));
            var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.Value), accessor);

            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
            validator.Subscribe(_ => { });
            Assert.Equal(1, data.ErrorsChangedSubscriptionCount);
            validator.Unsubscribe();
            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
        }

        internal class Data : IndeiBase
        {
            private int _value;
            private int _maximum;
            private string _error;

            public override bool HasErrors => _error != null;

            public int Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    RaisePropertyChanged();
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

            public override IEnumerable GetErrors(string propertyName)
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
                        RaiseErrorsChanged(nameof(Value));
                    }
                }
                else
                {
                    if (_error == null)
                    {
                        _error = "Must be less than Maximum";
                        RaiseErrorsChanged(nameof(Value));
                    }
                }
            }
        }
    }
}
