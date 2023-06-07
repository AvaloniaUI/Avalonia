using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ButtonsPage : UserControl
    {
        private int repeatButtonClickCount = 0;

        public ButtonsPage()
        {
            InitializeComponent();

            this.Get<RepeatButton>("RepeatButton").Click += OnRepeatButtonClick;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnRepeatButtonClick(object? sender, object args)
        {
            repeatButtonClickCount++;
            var textBlock = this.Get<TextBlock>("RepeatButtonTextBlock");
            textBlock.Text = $"Repeat Button: {repeatButtonClickCount}";
        }
    }
}
