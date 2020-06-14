using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace ControlCatalog.Pages
{
    public class CalendarDatePickerPage : UserControl
    {
        public CalendarDatePickerPage()
        {
            InitializeComponent();
            
            var dp1 = this.FindControl<CalendarDatePicker>("DatePicker1");
            var dp2 = this.FindControl<CalendarDatePicker>("DatePicker2");
            var dp3 = this.FindControl<CalendarDatePicker>("DatePicker3");
            var dp4 = this.FindControl<CalendarDatePicker>("DatePicker4");
            var dp5 = this.FindControl<CalendarDatePicker>("DatePicker5");

            dp1.SelectedDate = DateTime.Today;
            dp2.SelectedDate = DateTime.Today.AddDays(10);
            dp3.SelectedDate = DateTime.Today.AddDays(20);
            dp5.SelectedDate = DateTime.Today;

            dp4.TemplateApplied += (s, e) =>
            {
                dp4.BlackoutDates.AddDatesInPast();
            };
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
