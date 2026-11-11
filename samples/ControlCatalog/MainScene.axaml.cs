using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog
{
    [AndroidScene(label:"Control Catalog", androidTheme: "@style/MyTheme.NoActionBar", isExported: true, isLauncher:true, iconResource: "@drawable/icon")]
    public partial class MainScene : Scene
    {
        public MainScene()
        {
            InitializeComponent();

            DataContext = (App.Current as App)?.MainWindowViewModel;
        }
    }
}
