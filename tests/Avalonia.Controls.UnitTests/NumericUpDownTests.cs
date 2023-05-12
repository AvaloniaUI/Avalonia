using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NumericUpDownTests
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
                Assert.True(DataValidationErrors.GetErrors(control).SequenceEqual(new[] { exception }));
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
                Assert.True(DataValidationErrors.GetErrors(control).SequenceEqual(new[] { exception }));
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

        public static IEnumerable<object[]> Increment_Decrement_TestData()
        {
            // if min and max are not defined and value was null, 0 should be ne new value after spin
            yield return new object[] { decimal.MinValue, decimal.MaxValue, null, SpinDirection.Decrease, 0m };
            yield return new object[] { decimal.MinValue, decimal.MaxValue, null, SpinDirection.Increase, 0m };
            
            // if no value was defined, but Min or Max are defined, use these as the new value
            yield return new object[] { -400m, -200m, null, SpinDirection.Decrease, -200m };
            yield return new object[] { 200m, 400m, null, SpinDirection.Increase, 200m };
            
            // Value should be clamped to Min / Max after spinning
            yield return new object[] { 200m, 400m, 5m, SpinDirection.Increase, 200m };
            yield return new object[] { 200m, 400m, 200m, SpinDirection.Decrease, 200m };
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
                window.Presenter.ApplyTemplate();
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
    }
}
