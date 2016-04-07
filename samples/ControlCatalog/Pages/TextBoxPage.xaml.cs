using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class TextBoxPage : UserControl
    {
        public TextBoxPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
