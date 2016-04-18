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
            // iOS seems to be stripping out this assembly for some reason, so
            // going to force link it
            var theme = new Perspex.Themes.Default.DefaultTheme();
            theme.FindResource("test");


            this.LoadFromXaml();
        }
    }
}
