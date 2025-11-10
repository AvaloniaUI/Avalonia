using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ExpanderPage : UserControl
    {
        public ExpanderPage()
        {
            InitializeComponent();
            DataContext = new ExpanderPageViewModel();

            CollapsingDisabledExpander.Collapsing += (s, e) => { e.Cancel = true; };
            ExpandingDisabledExpander.Expanding += (s, e) => { e.Cancel = true; };
        }
    }
}
