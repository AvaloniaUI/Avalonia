using System;
using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class CalendarDatePickerPage : ContentPage
    {
        public CalendarDatePickerPage()
        {
            InitializeComponent();

            DataContext = new CalenderDatePickerViewModel();

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
