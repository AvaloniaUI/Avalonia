using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ToolTipPage : UserControl
    {
        public ToolTipPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
