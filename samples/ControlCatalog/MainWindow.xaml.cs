using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            App.AttachDevTools(this);
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
