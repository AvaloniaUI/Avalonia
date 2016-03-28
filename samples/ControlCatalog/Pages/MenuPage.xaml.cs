using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class MenuPage : UserControl
    {
        public MenuPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
