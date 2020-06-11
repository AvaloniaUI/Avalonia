using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ButtonPage : UserControl
    {
        private int repeatButtonClickCount = 0;

        public ButtonPage()
        {
            InitializeComponent();

            this.FindControl<RepeatButton>("RepeatButton").Click += OnRepeatButtonClick;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnRepeatButtonClick(object sender, object args)
        {
            repeatButtonClickCount++;
            var textBlock = this.FindControl<TextBlock>("RepeatButtonTextBlock");
            textBlock.Text = $"Repeat Button: {repeatButtonClickCount}";
        }
    }
}
