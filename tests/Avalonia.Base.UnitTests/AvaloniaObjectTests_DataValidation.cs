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
        public void Binding_Non_Validated_Styled_Property_Does_Not_Call_UpdateDataValidation()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<int>>();

            target.Bind(Class1.NonValidatedProperty, source);
            source.OnNext(6);
            source.OnNext(BindingValue<int>.BindingError(new Exception()));
            source.OnNext(BindingValue<int>.DataValidationError(new Exception()));
            source.OnNext(6);

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Binding_Non_Validated_Direct_Property_Does_Not_Call_UpdateDataValidation()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<int>>();

            target.Bind(Class1.NonValidatedDirectProperty, source);
            source.OnNext(6);
            source.OnNext(BindingValue<int>.BindingError(new Exception()));
            source.OnNext(BindingValue<int>.DataValidationError(new Exception()));
            source.OnNext(6);

            Assert.Empty(target.Notifications);
        }

        [Fact]
        public void Binding_Validated_Direct_Property_Calls_UpdateDataValidation()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<int>>();

            target.Bind(Class1.ValidatedDirectIntProperty, source);
            source.OnNext(6);
            source.OnNext(BindingValue<int>.BindingError(new Exception()));
            source.OnNext(BindingValue<int>.DataValidationError(new Exception()));
            source.OnNext(7);

            var result = target.Notifications.Cast<BindingValue<int>>().ToList();
            Assert.Equal(4, result.Count);
            Assert.Equal(BindingValueType.Value, result[0].Type);
            Assert.Equal(6, result[0].Value);
            Assert.Equal(BindingValueType.BindingError, result[1].Type);
            Assert.Equal(BindingValueType.DataValidationError, result[2].Type);
            Assert.Equal(BindingValueType.Value, result[3].Type);
            Assert.Equal(7, result[3].Value);
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

            public IList<object> Notifications { get; } = new List<object>();

            protected override void UpdateDataValidation<T>(
                AvaloniaProperty<T> property,
                BindingValue<T> value)
            {
                Notifications.Add(value);
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
