using System;
using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class CalendarDatePickerPage : UserControl
    {
        public CalendarDatePickerPage()
        {
            InitializeComponent();

            DatePicker1.SelectedDate = DateTime.Today;
            DatePicker2.SelectedDate = DateTime.Today.AddDays(10);
            DatePicker3.SelectedDate = DateTime.Today.AddDays(20);
            DatePicker5.SelectedDate = DateTime.Today;

            DatePicker4.TemplateApplied += (s, e) =>
            {
                DatePicker4.BlackoutDates?.AddDatesInPast();
            };
        }
    }
}
