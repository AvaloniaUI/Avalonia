using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;
using System.Globalization;

namespace Avalonia.Controls.UnitTests
{
    public class CalendarDatePickerTests : ScopedTestBase
    {
        private static bool CompareDates(DateTime first, DateTime second)
        {
            return first.Year == second.Year &&
                first.Month == second.Month &&
                first.Day == second.Day;
        }

        [Fact(Skip = "FIX ME ASAP")]
        public void SelectedDateChanged_Should_Fire_When_SelectedDate_Set()
        {
            using (UnitTestApplication.Start(Services))
            {
                bool handled = false;
                CalendarDatePicker datePicker = CreateControl();
                datePicker.SelectedDateChanged += (s,e) =>
                {
                    handled = true;
                };
                DateTime value = new DateTime(2000, 10, 10);
                datePicker.SelectedDate = value;
                Threading.Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.True(handled);
            }
        }

        [Fact]
        public void Setting_Selected_Date_To_Blackout_Date_Should_Throw()
        {
            using (UnitTestApplication.Start(Services))
            {
                CalendarDatePicker datePicker = CreateControl();
                Assert.NotNull(datePicker.BlackoutDates);
                datePicker.BlackoutDates.AddDatesInPast();

                DateTime goodValue = DateTime.Today.AddDays(1);
                datePicker.SelectedDate = goodValue;
                Assert.True(CompareDates(datePicker.SelectedDate.Value, goodValue));

                DateTime badValue = DateTime.Today.AddDays(-1);
                Assert.ThrowsAny<ArgumentOutOfRangeException>(
                    () => datePicker.SelectedDate = badValue);
            }
        }

        [Fact]
        public void Adding_Blackout_Dates_Containing_Selected_Date_Should_Throw()
        {
            using (UnitTestApplication.Start(Services))
            {
                CalendarDatePicker datePicker = CreateControl();
                datePicker.SelectedDate = DateTime.Today.AddDays(5);

                Assert.ThrowsAny<ArgumentOutOfRangeException>(
                    () => datePicker.BlackoutDates!.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10))));
            }
        }

        [Fact]
        public void Setting_Date_Manually_With_CustomDateFormatString_Should_Be_Accepted()
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            using (UnitTestApplication.Start(Services))
            {
                CalendarDatePicker datePicker = CreateControl();
                datePicker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
                datePicker.CustomDateFormatString = "dd.MM.yyyy";

                var tb = GetTextBox(datePicker);

                tb.Clear();
                RaiseTextEvent(tb, "17.10.2024");
                RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

                Assert.Equal("17.10.2024", datePicker.Text);
                Assert.True(CompareDates(datePicker.SelectedDate!.Value, new DateTime(2024, 10, 17)));

                tb.Clear();
                RaiseTextEvent(tb, "12.10.2024");
                RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

                Assert.Equal("12.10.2024", datePicker.Text);
                Assert.True(CompareDates(datePicker.SelectedDate.Value, new DateTime(2024, 10, 12)));
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>());

        private static CalendarDatePicker CreateControl()
        {
            var datePicker =
                new CalendarDatePicker
                {
                    Template = CreateTemplate()
                };

            datePicker.ApplyTemplate();
            return datePicker;
        }

        private static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<CalendarDatePicker>((control, scope) =>
            {
                var textBox = 
                    new TextBox
                    {
                        Name = "PART_TextBox"
                    }.RegisterInNameScope(scope);
                var button =
                    new Button
                    {
                        Name = "PART_Button"
                    }.RegisterInNameScope(scope);
                var calendar =
                    new Calendar
                    {
                        Name = "PART_Calendar", 
                        [!Calendar.SelectedDateProperty] = control[!CalendarDatePicker.SelectedDateProperty],
                        [!Calendar.DisplayDateProperty] = control[!CalendarDatePicker.DisplayDateProperty],
                        [!Calendar.DisplayDateStartProperty] = control[!CalendarDatePicker.DisplayDateStartProperty],
                        [!Calendar.DisplayDateEndProperty] = control[!CalendarDatePicker.DisplayDateEndProperty]
                    }.RegisterInNameScope(scope);
                var popup =
                    new Popup
                    {
                        Name = "PART_Popup"
                    }.RegisterInNameScope(scope);

                var panel = new Panel();
                panel.Children.Add(textBox);
                panel.Children.Add(button);
                panel.Children.Add(popup);
                panel.Children.Add(calendar);

                return panel;
            });

        }

        private TextBox GetTextBox(CalendarDatePicker control)
        {
            return control.GetTemplateChildren()
                .OfType<TextBox>()
                .First();
        }

        private static void RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
        {
            textBox.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private static void RaiseTextEvent(TextBox textBox, string text)
        {
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        }

    }
}
