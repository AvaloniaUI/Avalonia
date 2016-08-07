using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_Validation
    {
        [Fact]
        public void Disabled_Validation_Should_Trigger_Validation_Change_On_Exception()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl { DataContext = source };
            var binding = new Binding
            {
                Path = nameof(source.MustBePositive),
                Mode = BindingMode.TwoWay,

                // Even though EnableValidation = false, exception validation is enabled.
                EnableValidation = false,
            };

            target.Bind(TestControl.ValidationTestProperty, binding);

            target.ValidationTest = -5;

            Assert.True(false);
            //Assert.False(target.ValidationStatus.IsValid);
        }

        [Fact]
        public void Enabled_Validation_Should_Trigger_Validation_Change_On_Exception()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl { DataContext = source };
            var binding = new Binding
            {
                Path = nameof(source.MustBePositive),
                Mode = BindingMode.TwoWay,
                EnableValidation = true,
            };

            target.Bind(TestControl.ValidationTestProperty, binding);

            target.ValidationTest = -5;
            Assert.True(false);
            //Assert.False(target.ValidationStatus.IsValid);
        }


        [Fact]
        public void Passed_Validation_Should_Not_Add_Invalid_Pseudo_Class()
        {
            var control = new TestControl();
            var model = new ValidationTestModel { MustBePositive = 1 };
            var binding = new Binding
            {
                Path = nameof(model.MustBePositive),
                Mode = BindingMode.TwoWay,
                EnableValidation = true,
            };

            control.Bind(TestControl.ValidationTestProperty, binding);
            control.DataContext = model;
            Assert.DoesNotContain(control.Classes, x => x == ":invalid");
        }

        [Fact]
        public void Failed_Validation_Should_Add_Invalid_Pseudo_Class()
        {
            var control = new TestControl();
            var model = new ValidationTestModel { MustBePositive = 1 };
            var binding = new Binding
            {
                Path = nameof(model.MustBePositive),
                Mode = BindingMode.TwoWay,
                EnableValidation = true,
            };

            control.Bind(TestControl.ValidationTestProperty, binding);
            control.DataContext = model;
            control.ValidationTest = -5;
            Assert.Contains(control.Classes, x => x == ":invalid");
        }

        [Fact]
        public void Failed_Then_Passed_Validation_Should_Remove_Invalid_Pseudo_Class()
        {
            var control = new TestControl();
            var model = new ValidationTestModel { MustBePositive = 1 };

            var binding = new Binding
            {
                Path = nameof(model.MustBePositive),
                Mode = BindingMode.TwoWay,
                EnableValidation = true,
            };

            control.Bind(TestControl.ValidationTestProperty, binding);
            control.DataContext = model;


            control.ValidationTest = -5;
            Assert.Contains(control.Classes, x => x == ":invalid");
            control.ValidationTest = 5;
            Assert.DoesNotContain(control.Classes, x => x == ":invalid");
        }

        private class TestControl : Control
        {
            public static readonly StyledProperty<int> ValidationTestProperty
                = AvaloniaProperty.Register<TestControl, int>(nameof(ValidationTest), 1, defaultBindingMode: BindingMode.TwoWay);

            public int ValidationTest
            {
                get
                {
                    return GetValue(ValidationTestProperty);
                }
                set
                {
                    SetValue(ValidationTestProperty, value);
                }
            }

            protected override void DataValidationChanged(AvaloniaProperty property, BindingNotification status)
            {
                if (property == ValidationTestProperty)
                {
                    UpdateValidationState(status);
                }
            }
        }
        
        private class ValidationTestModel
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
                }
            }
        }
    }
}
