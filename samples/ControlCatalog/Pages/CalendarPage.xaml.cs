using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ControlCatalog.ViewModels;
using System;

namespace ControlCatalog.Pages
{
    public class CalendarPage : UserControl
    {
        private MainWindowViewModel _viewModel => this.DataContext as MainWindowViewModel;

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

        private void Calendar_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            _viewModel.ShowNotification(
                new Notification("Congratulation",
                $"You have changed the pointer wheel by {e.Delta}",
                NotificationType.Success,
                TimeSpan.FromSeconds(2)));
        }
    }
}
