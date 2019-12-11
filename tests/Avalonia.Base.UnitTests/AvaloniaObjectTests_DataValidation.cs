using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.UnitTests;
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

            target.SetValue(Class1.ValidatedDirectIntProperty, new BindingNotification(6));
            target.SetValue(Class1.ValidatedDirectIntProperty, new BindingNotification(new Exception(), BindingErrorType.Error));
            target.SetValue(Class1.ValidatedDirectIntProperty, new BindingNotification(new Exception(), BindingErrorType.DataValidationError));
            target.SetValue(Class1.ValidatedDirectIntProperty, new BindingNotification(7));

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
                [!Class1.ValidatedDirectIntProperty] = source.ToBinding(),
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

        [Fact]
        public void Bound_Validated_Direct_String_Property_Can_Be_Set_To_Null()
        {
            var source = new ViewModel
            {
                StringValue = "foo",
            };

            var target = new Class1
            {
                [!Class1.ValidatedDirectStringProperty] = new Binding
                {
                    Path = nameof(ViewModel.StringValue),
                    Source = source,
                },
            };

            Assert.Equal("foo", target.ValidatedDirectString);

            source.StringValue = null;

            Assert.Null(target.ValidatedDirectString);
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

            public static readonly DirectProperty<Class1, int> ValidatedDirectIntProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
                    nameof(ValidatedDirectInt),
                    o => o.ValidatedDirectInt,
                    (o, v) => o.ValidatedDirectInt = v,
                    enableDataValidation: true);

            public static readonly DirectProperty<Class1, string> ValidatedDirectStringProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>(
                    nameof(ValidatedDirectString),
                    o => o.ValidatedDirectString,
                    (o, v) => o.ValidatedDirectString = v,
                    enableDataValidation: true);

            private int _nonValidatedDirect;
            private int _directInt;
            private string _directString;

            public int NonValidated
            {
                get { return GetValue(NonValidatedProperty); }
                set { SetValue(NonValidatedProperty, value); }
            }

            public int NonValidatedDirect
            {
                get { return _directInt; }
                set { SetAndRaise(NonValidatedDirectProperty, ref _nonValidatedDirect, value); }
            }

            public int ValidatedDirectInt
            {
                get { return _directInt; }
                set { SetAndRaise(ValidatedDirectIntProperty, ref _directInt, value); }
            }

            public string ValidatedDirectString
            {
                get { return _directString; }
                set { SetAndRaise(ValidatedDirectStringProperty, ref _directString, value); }
            }

            public IList<BindingNotification> Notifications { get; } = new List<BindingNotification>();

            protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification notification)
            {
                Notifications.Add(notification);
            }
        }

        public class ViewModel : NotifyingBase
        {
            private string _stringValue;

            public string StringValue
            {
                get { return _stringValue; }
                set { _stringValue = value; RaisePropertyChanged(); }
            }
        }
    }
}
