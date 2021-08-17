using System;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Templates;
using Avalonia.Data;
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

        private void RunTest(Action<NumericUpDown, TextBox> test)
        {
            using (UnitTestApplication.Start(Services))
            {
                var control = CreateControl();
                TextBox textBox = GetTextBox(control);
                var window = new Window { Content = control };
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
        private TextBox GetTextBox(NumericUpDown control)
        {
            return control.GetTemplateChildren()
                          .OfType<ButtonSpinner>()
                          .Select(b => b.Content)
                          .OfType<TextBox>()
                          .First();
        }
        private IControlTemplate CreateTemplate()
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
