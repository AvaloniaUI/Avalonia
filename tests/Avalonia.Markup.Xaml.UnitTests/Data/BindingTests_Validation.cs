using System;
using System.Collections;
using System.Collections.Generic;
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
        public void Validated_Property_Does_Not_Receive_BindingNotifications()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl
            {
                DataContext = source,
                [!TestControl.ValidatedProperty] = new Binding(nameof(source.MustBePositive)),
            };

            source.MustBePositive = 6;

            Assert.Equal(
                new[]
                {
                    new BindingNotification(5),
                    new BindingNotification(new ArgumentOutOfRangeException("value"), BindingErrorType.DataValidationError),
                    new BindingNotification(6),
                },
                target.Notifications);
        }

        //[Fact]
        //public void Disabled_Validation_Should_Trigger_Validation_Change_On_Exception()
        //{
        //    var source = new ValidationTestModel { MustBePositive = 5 };
        //    var target = new TestControl { DataContext = source };
        //    var binding = new Binding
        //    {
        //        Path = nameof(source.MustBePositive),
        //        Mode = BindingMode.TwoWay,

        //        // Even though EnableValidation = false, exception validation is enabled.
        //        EnableValidation = false,
        //    };

        //    target.Bind(TestControl.ValidationTestProperty, binding);

        //    target.ValidationTest = -5;

        //    Assert.True(false);
        //    //Assert.False(target.ValidationStatus.IsValid);
        //}

        //[Fact]
        //public void Enabled_Validation_Should_Trigger_Validation_Change_On_Exception()
        //{
        //    var source = new ValidationTestModel { MustBePositive = 5 };
        //    var target = new TestControl { DataContext = source };
        //    var binding = new Binding
        //    {
        //        Path = nameof(source.MustBePositive),
        //        Mode = BindingMode.TwoWay,
        //        EnableValidation = true,
        //    };

        //    target.Bind(TestControl.ValidationTestProperty, binding);

        //    target.ValidationTest = -5;
        //    Assert.True(false);
        //    //Assert.False(target.ValidationStatus.IsValid);
        //}


        //[Fact]
        //public void Passed_Validation_Should_Not_Add_Invalid_Pseudo_Class()
        //{
        //    var control = new TestControl();
        //    var model = new ValidationTestModel { MustBePositive = 1 };
        //    var binding = new Binding
        //    {
        //        Path = nameof(model.MustBePositive),
        //        Mode = BindingMode.TwoWay,
        //        EnableValidation = true,
        //    };

        //    control.Bind(TestControl.ValidationTestProperty, binding);
        //    control.DataContext = model;
        //    Assert.DoesNotContain(control.Classes, x => x == ":invalid");
        //}

        //[Fact]
        //public void Failed_Validation_Should_Add_Invalid_Pseudo_Class()
        //{
        //    var control = new TestControl();
        //    var model = new ValidationTestModel { MustBePositive = 1 };
        //    var binding = new Binding
        //    {
        //        Path = nameof(model.MustBePositive),
        //        Mode = BindingMode.TwoWay,
        //        EnableValidation = true,
        //    };

        //    control.Bind(TestControl.ValidationTestProperty, binding);
        //    control.DataContext = model;
        //    control.ValidationTest = -5;
        //    Assert.Contains(control.Classes, x => x == ":invalid");
        //}

        //[Fact]
        //public void Failed_Then_Passed_Validation_Should_Remove_Invalid_Pseudo_Class()
        //{
        //    var control = new TestControl();
        //    var model = new ValidationTestModel { MustBePositive = 1 };

        //    var binding = new Binding
        //    {
        //        Path = nameof(model.MustBePositive),
        //        Mode = BindingMode.TwoWay,
        //        EnableValidation = true,
        //    };

        //    control.Bind(TestControl.ValidationTestProperty, binding);
        //    control.DataContext = model;


        //    control.ValidationTest = -5;
        //    Assert.Contains(control.Classes, x => x == ":invalid");
        //    control.ValidationTest = 5;
        //    Assert.DoesNotContain(control.Classes, x => x == ":invalid");
        //}

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

            public IList<BindingNotification> Notifications { get; } = new List<BindingNotification>();

            protected override void BindingNotificationReceived(AvaloniaProperty property, BindingNotification notification)
            {
                Notifications.Add(notification);
            }
        }
        
        private class ValidationTestModel : NotifyingBase
        {
            private int mustBePositive;

            public int MustBePositive
            {
                get { return mustBePositive; }
                set
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    mustBePositive = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
