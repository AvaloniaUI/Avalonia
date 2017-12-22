using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace ControlCatalog.Pages
{
    public class CalendarPage : UserControl
    {
        public CalendarPage()
        {
            this.InitializeComponent();

            var today = DateTime.Today; 
            var cal1 = this.FindControl<Calendar>("DisplayDatesCalendar");
            cal1.DisplayDateStart = today.AddDays(-25);
            cal1.DisplayDateEnd = today.AddDays(25);

            var cal2 = this.FindControl<Calendar>("BlackoutDatesCalendar");
            cal2.BlackoutDates.AddDatesInPast();
            cal2.BlackoutDates.Add(new CalendarDateRange(today.AddDays(6)));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
