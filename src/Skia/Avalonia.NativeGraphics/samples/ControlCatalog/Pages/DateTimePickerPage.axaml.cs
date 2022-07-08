using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class DateTimePickerPage : UserControl
    {
        public DateTimePickerPage()
        {
            this.InitializeComponent();
            this.FindControl<TextBlock>("DatePickerDesc").Text = "Use a DatePicker to let users set a date in your app, " +
                "for example to schedule an appointment. The DatePicker displays three controls for month, day, and year. " +
                "These controls are easy to use with touch or mouse, and they can be styled and configured in several different ways. " +
                "Order of month, day, and year is dynamically set based on user date settings";

            this.FindControl<TextBlock>("TimePickerDesc").Text = "Use a TimePicker to let users set a time in your app, for example " +
                "to set a reminder. The TimePicker displays three controls for hour, minute, and AM / PM(if necessary).These controls " +
                "are easy to use with touch or mouse, and they can be styled and configured in several different ways. " +
                "12 - hour or 24 - hour clock and visiblility of AM / PM is dynamically set based on user time settings, or can be overridden.";


        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
