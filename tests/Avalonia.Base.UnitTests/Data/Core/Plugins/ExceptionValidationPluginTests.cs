using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Plugins
{
    public class ExceptionValidationPluginTests
    {
        [Fact]
        public void Produces_BindingNotifications()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new ExceptionValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.MustBePositive));
            var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.MustBePositive), accessor);
            var result = new List<object>();

            validator.Subscribe(x => result.Add(x));
            validator.SetValue(5, BindingPriority.LocalValue);
            validator.SetValue(-2, BindingPriority.LocalValue);
            validator.SetValue(6, BindingPriority.LocalValue);

            Assert.Equal(new[]
            {
                new BindingNotification(0),
                new BindingNotification(5),
                new BindingNotification(new ArgumentOutOfRangeException("value"), BindingErrorType.DataValidationError),
                new BindingNotification(6),
            }, result);

            GC.KeepAlive(data);
        }

        public class Data : NotifyingBase
        {
            private int _mustBePositive;

            public int MustBePositive
            {
                get { return _mustBePositive; }
                set
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    if (value != _mustBePositive)
                    {
                        _mustBePositive = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }
    }
}
