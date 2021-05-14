using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;
using Avalonia.Interactivity;
namespace ControlCatalog.Pages
{
    public class ContextFlyoutPage : UserControl
    {
        private TextBox _textBox;

        public ContextFlyoutPage()
        {
            InitializeComponent();

            var vm = new ContextFlyoutPageViewModel();
            vm.View = this;
            DataContext = vm;

            _textBox = this.FindControl<TextBox>("TextBox");

            var cutButton = this.FindControl<Button>("CutButton");
            cutButton.Click += CloseFlyout;

            var copyButton = this.FindControl<Button>("CopyButton");
            copyButton.Click += CloseFlyout;

            var pasteButton = this.FindControl<Button>("PasteButton");
            pasteButton.Click += CloseFlyout;

            var clearButton = this.FindControl<Button>("ClearButton");
            clearButton.Click += CloseFlyout;
        }

        private void CloseFlyout(object sender, RoutedEventArgs e)
        {
            _textBox.ContextFlyout.Hide();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
