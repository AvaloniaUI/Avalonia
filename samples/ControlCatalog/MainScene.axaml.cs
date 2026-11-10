using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog
{
    public partial class MainScene : Scene
    {
        public MainScene()
        {
            InitializeComponent();

            DataContext = (App.Current as App)?.MainWindowViewModel;
        }
    }
}
