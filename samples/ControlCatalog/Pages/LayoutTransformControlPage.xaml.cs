using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class LayoutTransformControlPage : UserControl
    {
        public LayoutTransformControlPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
