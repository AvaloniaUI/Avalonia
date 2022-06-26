using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            _toggleFocusButton = this.FindControl<Button>("ToggleFocusButton");
            _removeItemsStackPanel = this.FindControl<StackPanel>("RemoveItemsStackPanel");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.F5)
            {
                _toggleFocusButton.IsEnabled = !_toggleFocusButton.IsEnabled;
                e.Handled = true;
            }
            else if (e.Key == Key.F6)
            {
                if (_removeItemsStackPanel.Children[^1] is Button)
                {
                    _removeItemsStackPanel.Children.RemoveAt(_removeItemsStackPanel.Children.Count - 1);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private Button _toggleFocusButton;
        private StackPanel _removeItemsStackPanel;
    }
}
