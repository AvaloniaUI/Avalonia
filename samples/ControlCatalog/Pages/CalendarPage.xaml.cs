using System;
using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class CalendarPage : UserControl
    {
        public CalendarPage()
        {
            InitializeComponent();

            var today = DateTime.Today;
            DisplayDatesCalendar.DisplayDateStart = today.AddDays(-25);
            DisplayDatesCalendar.DisplayDateEnd = today.AddDays(25);

            BlackoutDatesCalendar.BlackoutDates.AddDatesInPast();
            BlackoutDatesCalendar.BlackoutDates.Add(new CalendarDateRange(today.AddDays(6)));
        }
    }
}
