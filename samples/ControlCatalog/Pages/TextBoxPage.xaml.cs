using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class TextBoxPage : UserControl
    {
        public TextBoxPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.Get<TextBox>("numericWatermark")
                .TextInputOptionsQuery += (s, a) =>
                {
                    a.ContentType = Avalonia.Input.TextInput.TextInputContentType.Number;
                };
        }
    }
}
