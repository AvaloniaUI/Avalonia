using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class RadioButtonPage : UserControl
    {
        public RadioButtonPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
