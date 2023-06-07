using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_DataValidation
    {
        public abstract class TestBase<T>
            where T : AvaloniaProperty<int>
        {
            [Fact]
            public void Binding_Non_Validated_Property_Does_Not_Call_UpdateDataValidation()
            {
                var target = new Class1();
                var source = new Subject<BindingValue<int>>();
                var property = GetNonValidatedProperty();

                target.Bind(property, source);
                source.OnNext(6);
                source.OnNext(BindingValue<int>.BindingError(new Exception()));
                source.OnNext(BindingValue<int>.DataValidationError(new Exception()));
                source.OnNext(6);

                Assert.Empty(target.Notifications);
            }

            [Fact]
            public void Binding_Validated_Property_Calls_UpdateDataValidation()
            {
                var target = new Class1();
                var source = new Subject<BindingValue<int>>();
                var property = GetProperty();
                var error1 = new Exception();
                var error2 = new Exception();

                target.Bind(property, source);
                source.OnNext(6);
                source.OnNext(BindingValue<int>.DataValidationError(error1));
                source.OnNext(BindingValue<int>.BindingError(error2));
                source.OnNext(7);

                Assert.Equal(new Notification[]
                {
                    new(BindingValueType.Value, 6, null),
                    new(BindingValueType.DataValidationError, 6, error1),
                    new(BindingValueType.BindingError, 0, error2),
                    new(BindingValueType.Value, 7, null),
                }, target.Notifications);
            }

            [Fact]
            public void Binding_Validated_Property_Calls_UpdateDataValidation_Untyped()
            {
                var target = new Class1();
                var source = new Subject<object>();
                var property = GetProperty();
                var error1 = new Exception();
                var error2 = new Exception();

                target.Bind(property, source);
                source.OnNext(6);
                source.OnNext(new BindingNotification(error1, BindingErrorType.DataValidationError));
                source.OnNext(new BindingNotification(error2, BindingErrorType.Error));
                source.OnNext(7);

                Assert.Equal(new Notification[]
                {
                    new(BindingValueType.Value, 6, null),
                    new(BindingValueType.DataValidationError, 6, error1),
                    new(BindingValueType.BindingError, 0, error2),
                    new(BindingValueType.Value, 7, null),
                }, target.Notifications);
            }

            [Fact]
            public void Binding_Overridden_Validated_Property_Calls_UpdateDataValidation()
            {
                var target = new Class2();
                var source = new Subject<BindingValue<int>>();
                var property = GetNonValidatedProperty();

                // Class2 overrides the non-validated property metadata to enable data validation.
                target.Bind(property, source);
                source.OnNext(1);

                Assert.Equal(1, target.Notifications.Count);
            }

            [Fact]
            public void Disposing_Binding_Subscription_Clears_DataValidation()
            {
                var target = new Class1();
                var source = new Subject<BindingValue<int>>();
                var property = GetProperty();
                var error = new Exception();
                var sub = target.Bind(property, source);

                source.OnNext(6);
                source.OnNext(BindingValue<int>.DataValidationError(error));
                sub.Dispose();

                Assert.Equal(new Notification[]
                {
                    new(BindingValueType.Value, 6, null),
                    new(BindingValueType.DataValidationError, 6, error),
                    new(BindingValueType.UnsetValue, 6, null),
                }, target.Notifications);
            }

            [Fact]
            public void Completing_Binding_Clears_DataValidation()
            {
                var target = new Class1();
                var source = new Subject<BindingValue<int>>();
                var property = GetProperty();
                var error = new Exception();
                
                target.Bind(property, source);
                source.OnNext(6);
                source.OnNext(BindingValue<int>.DataValidationError(error));
                source.OnCompleted();

                Assert.Equal(new Notification[]
                {
                    new(BindingValueType.Value, 6, null),
                    new(BindingValueType.DataValidationError, 6, error),
                    new(BindingValueType.UnsetValue, 6, null),
                }, target.Notifications);
            }

            protected abstract T GetProperty();
            protected abstract T GetNonValidatedProperty();
        }

        public class DirectPropertyTests : TestBase<DirectPropertyBase<int>>
        {
            [Fact]
            public void Bound_Validated_String_Property_Can_Be_Set_To_Null()
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

            protected override DirectPropertyBase<int> GetProperty() => Class1.ValidatedDirectIntProperty;
            protected override DirectPropertyBase<int> GetNonValidatedProperty() => Class1.NonValidatedDirectIntProperty;
        }

        public class StyledPropertyTests : TestBase<StyledProperty<int>>
        {
            [Fact]
            public void Bound_Validated_String_Property_Can_Be_Set_To_Null()
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

            protected override StyledProperty<int> GetProperty() => Class1.ValidatedStyledIntProperty;
            protected override StyledProperty<int> GetNonValidatedProperty() => Class1.NonValidatedStyledIntProperty;
        }

        private record class Notification(BindingValueType type, object? value, Exception? error);

        private class Class1 : AvaloniaObject
        {
            public static readonly DirectProperty<Class1, int> NonValidatedDirectIntProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
                    nameof(NonValidatedDirectInt),
                    o => o.NonValidatedDirectInt,
                    (o, v) => o.NonValidatedDirectInt = v);

            public static readonly DirectProperty<Class1, int> ValidatedDirectIntProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
                    nameof(ValidatedDirectInt),
                    o => o.ValidatedDirectInt,
                    (o, v) => o.ValidatedDirectInt = v,
                    enableDataValidation: true);

            public static readonly DirectProperty<Class1, string?> ValidatedDirectStringProperty =
                AvaloniaProperty.RegisterDirect<Class1, string?>(
                    nameof(ValidatedDirectString),
                    o => o.ValidatedDirectString,
                    (o, v) => o.ValidatedDirectString = v,
                    enableDataValidation: true);

            public static readonly StyledProperty<int> NonValidatedStyledIntProperty =
                AvaloniaProperty.Register<Class1, int>(
                    nameof(NonValidatedStyledInt));

            public static readonly StyledProperty<int> ValidatedStyledIntProperty =
                AvaloniaProperty.Register<Class1, int>(
                    nameof(ValidatedStyledInt),
                    enableDataValidation: true);

            private int _nonValidatedDirect;
            private int _directInt;
            private string? _directString;

            public int NonValidatedDirectInt
            {
                get { return _directInt; }
                set { SetAndRaise(NonValidatedDirectIntProperty, ref _nonValidatedDirect, value); }
            }

            public int ValidatedDirectInt
            {
                get { return _directInt; }
                set { SetAndRaise(ValidatedDirectIntProperty, ref _directInt, value); }
            }

            public string? ValidatedDirectString
            {
                get { return _directString; }
                set { SetAndRaise(ValidatedDirectStringProperty, ref _directString, value); }
            }

            public int NonValidatedStyledInt
            {
                get { return GetValue(NonValidatedStyledIntProperty); }
                set { SetValue(NonValidatedStyledIntProperty, value); }
            }

            public int ValidatedStyledInt
            {
                get => GetValue(ValidatedStyledIntProperty);
                set => SetValue(ValidatedStyledIntProperty, value);
            }

            public List<Notification> Notifications { get; } = new();

            protected override void UpdateDataValidation(
                AvaloniaProperty property,
                BindingValueType state,
                Exception? error)
            {
                Notifications.Add(new(state, GetValue(property), error));
            }
        }

        private class Class2 : Class1
        {
            static Class2()
            {
                NonValidatedDirectIntProperty.OverrideMetadata<Class2>(
                    new DirectPropertyMetadata<int>(enableDataValidation: true));
                NonValidatedStyledIntProperty.OverrideMetadata<Class2>(
                    new StyledPropertyMetadata<int>(enableDataValidation: true));
            }
        }

        public class ViewModel : NotifyingBase
        {
            private string? _stringValue;

            public string? StringValue
            {
                get { return _stringValue; }
                set { _stringValue = value; RaisePropertyChanged(); }
            }
        }
    }
}
