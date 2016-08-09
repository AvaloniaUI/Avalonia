// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_DataValidation
    {
        [Fact]
        public void Doesnt_Send_DataValidationError_When_DataValidatation_Not_Enabled()
        {
            var data = new ExceptionTest { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), false);
            var validationMessageFound = false;

            observer.OfType<BindingNotification>()
                .Where(x => x.ErrorType == BindingErrorType.DataValidationError)
                .Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);

            Assert.False(validationMessageFound);
        }

        [Fact]
        public void Exception_Validation_Sends_DataValidationError()
        {
            var data = new ExceptionTest { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), true);
            var validationMessageFound = false;

            observer.OfType<BindingNotification>()
                .Where(x => x.ErrorType == BindingErrorType.DataValidationError)
                .Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);

            Assert.True(validationMessageFound);
        }

        [Fact]
        public void Indei_Validation_Does_Not_Subscribe_When_DataValidatation_Not_Enabled()
        {
            var data = new IndeiTest { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), false);

            observer.Subscribe(_ => { });

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
        }

        [Fact]
        public void Enabled_Indei_Validation_Subscribes()
        {
            var data = new IndeiTest { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), true);
            var sub = observer.Subscribe(_ => { });

            Assert.Equal(1, data.PropertyChangedSubscriptionCount);
            sub.Dispose();
            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
        }

        [Fact]
        public void Validation_Plugins_Send_Correct_Notifications()
        {
            var data = new IndeiTest();
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), true);
            var result = new List<object>();

            observer.Subscribe(x => result.Add(x));
            observer.SetValue(5);
            observer.SetValue(-5);
            observer.SetValue("foo");
            observer.SetValue(5);

            Assert.Equal(new[]
            {
                new BindingNotification(0),

                // Value is notified twice as ErrorsChanged is always called by IndeiTest.
                new BindingNotification(5),
                new BindingNotification(5),

                // Value is first signalled without an error as validation hasn't been updated.
                new BindingNotification(-5),
                new BindingNotification(new Exception("Must be positive"), BindingErrorType.DataValidationError, -5),

                // Exception is thrown by trying to set value to "foo".
                new BindingNotification(
                    new ArgumentException("Object of type 'System.String' cannot be converted to type 'System.Int32'."),
                    BindingErrorType.DataValidationError),

                // Value is set then validation is updated.
                new BindingNotification(new Exception("Must be positive"), BindingErrorType.DataValidationError, 5),
                new BindingNotification(5),
            }, result);
        }

        public class ExceptionTest : NotifyingBase
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

                    _mustBePositive = value;
                    RaisePropertyChanged();
                }
            }
        }

        private class IndeiTest : NotifyingBase, INotifyDataErrorInfo
        {
            private int _mustBePositive;
            private Dictionary<string, IList<string>> _errors = new Dictionary<string, IList<string>>();
            private EventHandler<DataErrorsChangedEventArgs> _errorsChanged;

            public int MustBePositive
            {
                get { return _mustBePositive; }
                set
                {
                    _mustBePositive = value;
                    RaisePropertyChanged();

                    if (value >= 0)
                    {
                        _errors.Remove(nameof(MustBePositive));
                        _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(MustBePositive)));
                    }
                    else
                    {
                        _errors[nameof(MustBePositive)] = new[] { "Must be positive" };
                        _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(MustBePositive)));
                    }
                }
            }

            public bool HasErrors => _mustBePositive >= 0;

            public int ErrorsChangedSubscriptionCount { get; private set; }

            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged
            {
                add { _errorsChanged += value; ++ErrorsChangedSubscriptionCount; }
                remove { _errorsChanged -= value; --ErrorsChangedSubscriptionCount; }
            }

            public IEnumerable GetErrors(string propertyName)
            {
                IList<string> result;
                _errors.TryGetValue(propertyName, out result);
                return result;
            }
        }
    }
}
