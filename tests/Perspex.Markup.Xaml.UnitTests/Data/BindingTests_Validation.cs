using Perspex.Controls;
using Perspex.Data;
using Perspex.Markup.Data;
using Perspex.Markup.Xaml.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_Validation
    {
        [Fact]
        public void Disabled_Validation_Should_Not_Trigger_Validation_Change()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl { DataContext = source };
            var binding = new Binding
            {
                Path = nameof(source.MustBePositive),
                Mode = BindingMode.TwoWay,
                ValidationMethods = ValidationMethods.None
            };
            target.Bind(TestControl.ValidationTestProperty, binding);
            
            target.ValidationTest = -5;

            Assert.True(target.ValidationStatus.IsValid);
        }

        [Fact]
        public void Enabled_Validation_Should_Trigger_Validation_Change()
        {
            var source = new ValidationTestModel { MustBePositive = 5 };
            var target = new TestControl { DataContext = source };
            var binding = new Binding
            {
                Path = nameof(source.MustBePositive),
                Mode = BindingMode.TwoWay,
                ValidationMethods = ValidationMethods.All
            };
            target.Bind(TestControl.ValidationTestProperty, binding);

            target.ValidationTest = -5;
            Assert.False(target.ValidationStatus.IsValid);
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
                ValidationMethods = ValidationMethods.All
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
                ValidationMethods = ValidationMethods.All
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
                ValidationMethods = ValidationMethods.All
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
                = PerspexProperty.Register<TestControl, int>(nameof(ValidationTest), 1, defaultBindingMode: BindingMode.TwoWay);

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

            protected override void DataValidationChanged(PerspexProperty property, IValidationStatus status)
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
