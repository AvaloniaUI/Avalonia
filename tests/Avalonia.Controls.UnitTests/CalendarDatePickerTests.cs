using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class CalendarDatePickerTests
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
                Threading.Dispatcher.UIThread.RunJobs();
                Assert.True(handled);
            }
        }

        [Fact]
        public void Setting_Selected_Date_To_Blackout_Date_Should_Throw()
        {
            using (UnitTestApplication.Start(Services))
            {
                CalendarDatePicker datePicker = CreateControl();
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
                    () => datePicker.BlackoutDates.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10))));
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<IStandardCursorFactory>());

        private CalendarDatePicker CreateControl()
        {
            var datePicker =
                new CalendarDatePicker
                {
                    Template = CreateTemplate()
                };

            datePicker.ApplyTemplate();
            return datePicker;
        }

        private IControlTemplate CreateTemplate()
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
                        Name = "PART_Calendar"
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
    }
}
