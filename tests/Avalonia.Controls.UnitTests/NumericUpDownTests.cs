using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NumericUpDownTests : ScopedTestBase
    {
        private static TestServices Services => TestServices.StyledWindow;

        [Fact]
        public void Text_Validation()
        {
            RunTest((control, textbox) =>
            {
                var exception = new InvalidCastException("failed validation");
                var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                control.Bind(NumericUpDown.TextProperty, textObservable);
                Dispatcher.UIThread.RunJobs();

                Assert.True(DataValidationErrors.GetHasErrors(control));
                Assert.Equal([exception], DataValidationErrors.GetErrors(control));
            });
        }

        [Fact]
        public void Value_Validation()
        {
            RunTest((control, textbox) =>
            {
                var exception = new InvalidCastException("failed validation");
                var valueObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                control.Bind(NumericUpDown.ValueProperty, valueObservable);
                Dispatcher.UIThread.RunJobs();

                Assert.True(DataValidationErrors.GetHasErrors(control));
                Assert.Equal([exception], DataValidationErrors.GetErrors(control));
            });
        }

        [Theory]
        [MemberData(nameof(Increment_Decrement_TestData))]
        public void Increment_Decrement_Tests(decimal min, decimal max, decimal? value, SpinDirection direction,
            decimal? expected)
        {
            var control = CreateControl();
            if (min > decimal.MinValue) control.Minimum = min;
            if (max < decimal.MaxValue) control.Maximum = max;
            control.Value = value;

            var spinner = GetSpinner(control);

            spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, direction));
            
            Assert.Equal(control.Value, expected);
        }

        [Fact]
        public void FormatString_Is_Applied_Immediately()
        {
            RunTest((control, textbox) =>
            {
                const decimal value = 10.11m;

                // Establish and verify initial conditions.
                control.FormatString = "F0";
                control.Value = value;
                Assert.Equal(value.ToString("F0"), control.Text);

                // Check that FormatString is applied.
                control.FormatString = "F2";
                Assert.Equal(value.ToString("F2"), control.Text);
            });
        }

        [Fact]
        public void NumberFormat_Is_Applied_Immediately()
        {
            RunTest((control, textbox) =>
            {
                const decimal value = 10.11m;
                var initialNumberFormat = new NumberFormatInfo { NumberDecimalSeparator = "." };
                var newNumberFormat = new NumberFormatInfo { NumberDecimalSeparator = ";" };

                // Establish and verify initial conditions.
                control.NumberFormat = initialNumberFormat;
                control.Value = value;
                Assert.Equal(value.ToString(initialNumberFormat), control.Text);

                // Check that NumberFormat is applied.
                control.NumberFormat = newNumberFormat;
                Assert.Equal(value.ToString(newNumberFormat), control.Text);
            });
        }

        public static IEnumerable<object?[]> Increment_Decrement_TestData()
        {
            // if min and max are not defined and value was null, 0 should be ne new value after spin
            yield return [decimal.MinValue, decimal.MaxValue, null, SpinDirection.Decrease, 0m];
            yield return [decimal.MinValue, decimal.MaxValue, null, SpinDirection.Increase, 0m];
            
            // if no value was defined, but Min or Max are defined, use these as the new value
            yield return [-400m, -200m, null, SpinDirection.Decrease, -200m];
            yield return [200m, 400m, null, SpinDirection.Increase, 200m];
            
            // Value should be clamped to Min / Max after spinning
            yield return [200m, 400m, 5m, SpinDirection.Increase, 200m];
            yield return [200m, 400m, 200m, SpinDirection.Decrease, 200m];
        }
        
        private void RunTest(Action<NumericUpDown, TextBox> test)
        {
            using (UnitTestApplication.Start(Services))
            {
                var control = CreateControl();
                TextBox textBox = GetTextBox(control);
                var window = new Window { Content = control };
                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                Dispatcher.UIThread.RunJobs();
                test.Invoke(control, textBox);
            }
        }

        private NumericUpDown CreateControl()
        {
            var control = new NumericUpDown
            {
                Template = CreateTemplate()
            };

            control.ApplyTemplate();
            return control;
        }
        private static TextBox GetTextBox(NumericUpDown control)
        {
            return control.GetTemplateChildren()
                          .OfType<ButtonSpinner>()
                          .Select(b => b.Content)
                          .OfType<TextBox>()
                          .First();
        }
        
        private static ButtonSpinner GetSpinner(NumericUpDown control)
        {
            return control.GetTemplateChildren()
                .OfType<ButtonSpinner>()
                .First();
        }
        
        private static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<NumericUpDown>((control, scope) =>
            {
                var textBox =
                    new TextBox
                    {
                        Name = "PART_TextBox"
                    }.RegisterInNameScope(scope);
                return new ButtonSpinner
                    {
                        Name = "PART_Spinner",
                        Content = textBox,
                    }.RegisterInNameScope(scope);
            });
        }

        [Fact]
        public void TabIndex_Should_Be_Synchronized_With_Inner_TextBox()
        {
            RunTest((control, textbox) =>
            {
                // Set TabIndex on NumericUpDown
                control.TabIndex = 5;
                
                // The inner TextBox should inherit the same TabIndex
                Assert.Equal(5, textbox.TabIndex);
                
                // Change TabIndex and verify it gets synchronized
                control.TabIndex = 10;
                Assert.Equal(10, textbox.TabIndex);
            });
        }
    }
}
