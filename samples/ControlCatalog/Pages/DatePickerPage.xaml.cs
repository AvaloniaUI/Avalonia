using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace ControlCatalog.Pages
{
    public class DatePickerPage : UserControl
    {
        public DatePickerPage()
        {
            InitializeComponent();
            
            var dp1 = this.FindControl<DatePicker>("DatePicker1");
            var dp2 = this.FindControl<DatePicker>("DatePicker2");
            var dp3 = this.FindControl<DatePicker>("DatePicker3");
            var dp4 = this.FindControl<DatePicker>("DatePicker4");
            var dp5 = this.FindControl<DatePicker>("DatePicker5");

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
