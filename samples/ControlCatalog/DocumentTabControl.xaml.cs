using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
    public class DocumentTabControl : UserControl
    {
        public DocumentTabControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}