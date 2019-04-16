using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Views
{
    public class NotificationView : UserControl
    {
        public NotificationView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
