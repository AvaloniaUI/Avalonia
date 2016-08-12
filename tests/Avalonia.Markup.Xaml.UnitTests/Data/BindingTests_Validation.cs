using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_Validation
    {
        [Fact]
        public void Non_Validated_Property_Does_Not_Receive_BindingNotifications()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl
            {
                DataContext = source,
                [!TestControl.NonValidatedProperty] = new Binding(nameof(source.MustBePositive)),
            };

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Validated_Direct_Property_Receives_BindingNotifications()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl
            {
                DataContext = source,
            };

            target.Bind(
                TestControl.ValidatedDirectProperty,
                new Binding(nameof(source.MustBePositive), BindingMode.TwoWay));

            target.ValidatedDirect = 6;
            target.ValidatedDirect = -1;
            target.ValidatedDirect = 7;

            Assert.Equal(
                new[]
                {
                    null, // 5
                    null, // 6
                    new BindingNotification(new ArgumentOutOfRangeException("value"), BindingErrorType.DataValidationError),
                    null, // 7
                },
                target.Notifications.AsEnumerable());
        }

        private class TestControl : Control
        {
            public static readonly StyledProperty<int> NonValidatedProperty =
                AvaloniaProperty.Register<TestControl, int>(
                    nameof(Validated),
                    enableDataValidation: false);

            public static readonly StyledProperty<int> ValidatedProperty =
                AvaloniaProperty.Register<TestControl, int>(
                    nameof(Validated),
                    enableDataValidation: true);

            public static readonly DirectProperty<TestControl, int> ValidatedDirectProperty =
                AvaloniaProperty.RegisterDirect<TestControl, int>(
                    nameof(Validated),
                    o => o.ValidatedDirect,
                    (o, v) => o.ValidatedDirect = v,
                    enableDataValidation: true);

            private int _direct;

            public int NonValidated
            {
                get { return GetValue(NonValidatedProperty); }
                set { SetValue(NonValidatedProperty, value); }
            }

            public int Validated
            {
                get { return GetValue(ValidatedProperty); }
                set { SetValue(ValidatedProperty, value); }
            }

            public int ValidatedDirect
            {
                get { return _direct; }
                set { SetAndRaise(ValidatedDirectProperty, ref _direct, value); }
            }

            public IList<BindingNotification> Notifications { get; } = new List<BindingNotification>();

            protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification notification)
            {
                Notifications.Add(notification);
            }
        }
        
        private class ValidationTestModel : NotifyingBase
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

                    if (_mustBePositive != value)
                    {
                        _mustBePositive = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }
    }
}
