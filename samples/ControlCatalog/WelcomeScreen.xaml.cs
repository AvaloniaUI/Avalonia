using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
    public class WelcomeScreenView : UserControl
    {
        public WelcomeScreenView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}