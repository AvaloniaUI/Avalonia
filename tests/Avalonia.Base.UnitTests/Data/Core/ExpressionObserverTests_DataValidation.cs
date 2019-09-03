// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_DataValidation : IClassFixture<InvariantCultureFixture>
    {
        [Fact]
        public void Doesnt_Send_DataValidationError_When_DataValidatation_Not_Enabled()
        {
            var data = new ExceptionTest { MustBePositive = 5 };
            var observer = ExpressionObserver.Create(data, o => o.MustBePositive, false);
            var validationMessageFound = false;

            observer.OfType<BindingNotification>()
                .Where(x => x.ErrorType == BindingErrorType.DataValidationError)
                .Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);

            Assert.False(validationMessageFound);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Exception_Validation_Sends_DataValidationError()
        {
            var data = new ExceptionTest { MustBePositive = 5 };
            var observer = ExpressionObserver.Create(data, o => o.MustBePositive, true);
            var validationMessageFound = false;

            observer.OfType<BindingNotification>()
                .Where(x => x.ErrorType == BindingErrorType.DataValidationError)
                .Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);

            Assert.True(validationMessageFound);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Indei_Validation_Does_Not_Subscribe_When_DataValidatation_Not_Enabled()
        {
            var data = new IndeiTest { MustBePositive = 5 };
            var observer = ExpressionObserver.Create(data, o => o.MustBePositive, false);

            observer.Subscribe(_ => { });

            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
        }

        [Fact]
        public void Enabled_Indei_Validation_Subscribes()
        {
            var data = new IndeiTest { MustBePositive = 5 };
            var observer = ExpressionObserver.Create(data, o => o.MustBePositive, true);
            var sub = observer.Subscribe(_ => { });

            Assert.Equal(1, data.ErrorsChangedSubscriptionCount);
            sub.Dispose();
            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
        }

        [Fact]
        public void Validation_Plugins_Send_Correct_Notifications()
        {
            var data = new IndeiTest();
            var observer = ExpressionObserver.Create(data, o => o.MustBePositive, true);
            var result = new List<object>();
            
            var errmsg = string.Empty;
            try { typeof(IndeiTest).GetProperty(nameof(IndeiTest.MustBePositive)).SetValue(data, "foo"); }
            catch(Exception e) { errmsg = e.Message; }

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
                new BindingNotification(new DataValidationException("Must be positive"), BindingErrorType.DataValidationError, -5),

                // Exception is thrown by trying to set value to "foo".
                new BindingNotification(
                    new ArgumentException(errmsg),
                    BindingErrorType.DataValidationError),

                // Value is set then validation is updated.
                new BindingNotification(new DataValidationException("Must be positive"), BindingErrorType.DataValidationError, 5),
                new BindingNotification(5),
            }, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Doesnt_Subscribe_To_Indei_Of_Intermediate_Object_In_Chain()
        {
            var data = new Container
            {
                Inner = new IndeiTest()
            };

            var observer = ExpressionObserver.Create(data, o => o.Inner.MustBePositive, true);

            observer.Subscribe(_ => { });

            // We may want to change this but I've never seen an example of data validation on an
            // intermediate object in a chain so for the moment I'm not sure what the result of 
            // validating such a thing should look like.
            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
            Assert.Equal(1, data.Inner.ErrorsChangedSubscriptionCount);
        }

        [Fact]
        public void Sends_Correct_Notifications_With_Property_Chain()
        {
            var container = new Container();

            var observer = ExpressionObserver.Create(container, o => o.Inner.MustBePositive, true);

            var result = new List<object>();

            observer.Subscribe(x => result.Add(x));

            Assert.Equal(new[]
            {
                new BindingNotification(
                    new MarkupBindingChainException("Null value", "o => o.Inner.MustBePositive", "Inner"),
                    BindingErrorType.Error,
                    AvaloniaProperty.UnsetValue),
            }, result);

            GC.KeepAlive(container);
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

        private class IndeiTest : IndeiBase
        {
            private int _mustBePositive;
            private Dictionary<string, IList<string>> _errors = new Dictionary<string, IList<string>>();

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
                        RaiseErrorsChanged(nameof(MustBePositive));
                    }
                    else
                    {
                        _errors[nameof(MustBePositive)] = new[] { "Must be positive" };
                        RaiseErrorsChanged(nameof(MustBePositive));
                    }
                }
            }

            public override bool HasErrors => _mustBePositive >= 0;

            public override IEnumerable GetErrors(string propertyName)
            {
                IList<string> result;
                _errors.TryGetValue(propertyName, out result);
                return result;
            }
        }

        private class Container : IndeiBase
        {
            private IndeiTest _inner;

            public IndeiTest Inner
            {
                get { return _inner; }
                set { _inner = value; RaisePropertyChanged(); }
            }

            public override bool HasErrors => false;
            public override IEnumerable GetErrors(string propertyName) => null;
        }
    }
}
