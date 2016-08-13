using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_DataValidation
    {
        [Fact]
        public void Setting_Non_Validated_Property_Does_Not_Call_UpdateDataValidation()
        {
            var target = new Class1();

            target.SetValue(Class1.NonValidatedProperty, 6);
            target.SetValue(Class1.NonValidatedProperty, new BindingNotification(new Exception(), BindingErrorType.Error));
            target.SetValue(Class1.NonValidatedProperty, new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            target.SetValue(Class1.NonValidatedProperty, new BindingNotification(7));
            target.SetValue(Class1.NonValidatedProperty, 8);

            Assert.Empty(target.Notifications);
        }

        [Fact(Skip = "Data validation not yet implemented for non-direct properties")]
        public void Setting_Validated_Property_Calls_UpdateDataValidation()
        {
            var target = new Class1();

            target.SetValue(Class1.ValidatedProperty, 6);
            target.SetValue(Class1.ValidatedProperty, new BindingNotification(new Exception(), BindingErrorType.Error));
            target.SetValue(Class1.ValidatedProperty, new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            target.SetValue(Class1.ValidatedProperty, new BindingNotification(7));
            target.SetValue(Class1.ValidatedProperty, 8);

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Setting_Validated_Direct_Property_Calls_UpdateDataValidation()
        {
            var target = new Class1();

            target.SetValue(Class1.ValidatedDirectProperty, 6);
            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(new Exception(), BindingErrorType.Error));
            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(7));
            target.SetValue(Class1.ValidatedDirectProperty, 8);

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Binding_Non_Validated_Property_Does_Not_Call_UpdateDataValidation()
        {
            var source = new Subject<object>();
            var target = new Class1
            {
                [!Class1.NonValidatedProperty] = source.AsBinding(),
            };

            source.OnNext(6);
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            source.OnNext(new BindingNotification(7));
            source.OnNext(8);

            Assert.Empty(target.Notifications);
        }

        [Fact(Skip = "Data validation not yet implemented for non-direct properties")]
        public void Binding_Validated_Property_Calls_UpdateDataValidation()
        {
            var source = new Subject<object>();
            var target = new Class1
            {
                [!Class1.ValidatedProperty] = source.AsBinding(),
            };

            source.OnNext(6);
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            source.OnNext(new BindingNotification(7));
            source.OnNext(8);

            Assert.Equal(
                new[]
                {
                    null, // 6
                    new BindingNotification(new Exception(), BindingErrorType.Error),
                    new BindingNotification(new Exception(), BindingErrorType.DataValidationError),
                    new BindingNotification(7), // 7
                    null, // 8
                },
                target.Notifications.AsEnumerable());
        }

        [Fact]
        public void Binding_Validated_Direct_Property_Calls_UpdateDataValidation()
        {
            var source = new Subject<object>();
            var target = new Class1
            {
                [!Class1.ValidatedDirectProperty] = source.AsBinding(),
            };

            source.OnNext(6);
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            source.OnNext(new BindingNotification(7));
            source.OnNext(8);

            Assert.Equal(
                new[]
                {
                    null, // 6
                    new BindingNotification(new Exception(), BindingErrorType.Error),
                    new BindingNotification(new Exception(), BindingErrorType.DataValidationError),
                    new BindingNotification(7), // 7
                    null, // 8
                },
                target.Notifications.AsEnumerable());
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<int> NonValidatedProperty =
                AvaloniaProperty.Register<Class1, int>(
                    nameof(Validated),
                    enableDataValidation: false);

            public static readonly StyledProperty<int> ValidatedProperty =
                AvaloniaProperty.Register<Class1, int>(
                    nameof(Validated),
                    enableDataValidation: true);

            public static readonly DirectProperty<Class1, int> ValidatedDirectProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
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
    }
}
