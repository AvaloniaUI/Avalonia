using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.Themes.DataGrid.Default
{
    public class DefaultTheme : Styles
    {
        public DefaultTheme()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
