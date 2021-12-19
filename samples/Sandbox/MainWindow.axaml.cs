using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Win32.WinRT.Composition;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var button = new Button
            {
                Content = "Tabulate to this button"
            };

            Content = button;
            button.GotFocus += (_, _) =>
            {
                button.IsEnabled = false;
                button.Content =
                    $"Now this button is disabled ({nameof(IsEnabled)}:{button.IsEnabled},{nameof(IsEffectivelyEnabled)}:{button.IsEffectivelyEnabled}), but you can still press Enter";
                button.KeyDown += (_, _) =>
                {
                    button.Content = "button got fired by KeyDown event";
                };
            };
            button.Click += (_, _) =>
            {
                button.Content = "It just has been clicked.";
            };
        }
    }
}
