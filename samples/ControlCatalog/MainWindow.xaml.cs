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
			this.LoadFromXaml();
        }
    }
}
