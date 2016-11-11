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

            target.SetValue(Class1.NonValidatedDirectProperty, 6);

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Setting_Non_Validated_Direct_Property_Does_Not_Call_UpdateDataValidation()
        {
            var target = new Class1();

            target.SetValue(Class1.NonValidatedDirectProperty, 6);

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Setting_Validated_Direct_Property_Calls_UpdateDataValidation()
        {
            var target = new Class1();

            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(6));
            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(new Exception(), BindingErrorType.Error));
            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            target.SetValue(Class1.ValidatedDirectProperty, new BindingNotification(7));

            Assert.Equal(
                new[]
                {
                    new BindingNotification(6),
                    new BindingNotification(new Exception(), BindingErrorType.Error),
                    new BindingNotification(new Exception(), BindingErrorType.DataValidationError),
                    new BindingNotification(7),
                },
                target.Notifications.AsEnumerable());
        }

        [Fact]
        public void Binding_Non_Validated_Property_Does_Not_Call_UpdateDataValidation()
        {
            var source = new Subject<object>();
            var target = new Class1
            {
                [!Class1.NonValidatedProperty] = source.ToBinding(),
            };

            source.OnNext(new BindingNotification(6));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            source.OnNext(new BindingNotification(7));

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Binding_Validated_Direct_Property_Calls_UpdateDataValidation()
        {
            var source = new Subject<object>();
            var target = new Class1
            {
                [!Class1.ValidatedDirectProperty] = source.ToBinding(),
            };

            source.OnNext(new BindingNotification(6));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            source.OnNext(new BindingNotification(7));

            Assert.Equal(
                new[]
                {
                    new BindingNotification(6),
                    new BindingNotification(new Exception(), BindingErrorType.Error),
                    new BindingNotification(new Exception(), BindingErrorType.DataValidationError),
                    new BindingNotification(7),
                },
                target.Notifications.AsEnumerable());
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<int> NonValidatedProperty =
                AvaloniaProperty.Register<Class1, int>(
                    nameof(NonValidated));

            public static readonly DirectProperty<Class1, int> NonValidatedDirectProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
                    nameof(NonValidatedDirect),
                    o => o.NonValidatedDirect,
                    (o, v) => o.NonValidatedDirect = v);

            public static readonly DirectProperty<Class1, int> ValidatedDirectProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
                    nameof(ValidatedDirect),
                    o => o.ValidatedDirect,
                    (o, v) => o.ValidatedDirect = v,
                    enableDataValidation: true);

            private int _nonValidatedDirect;
            private int _direct;

            public int NonValidated
            {
                get { return GetValue(NonValidatedProperty); }
                set { SetValue(NonValidatedProperty, value); }
            }

            public int NonValidatedDirect
            {
                get { return _direct; }
                set { SetAndRaise(NonValidatedDirectProperty, ref _nonValidatedDirect, value); }
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
