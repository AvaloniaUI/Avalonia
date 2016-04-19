using Perspex;
using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            // TODO: iOS does not support dynamically loading assemblies
            // so we must refer to this resource DLL statically. For
            // now I am doing that here. But we need a better solution!!
            var theme = new Perspex.Themes.Default.DefaultTheme();
            theme.FindResource("Button");


            this.LoadFromXaml();
        }
    }
}
