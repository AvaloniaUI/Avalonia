using Avalonia.Controls;

namespace ControlCatalog
{
    [AndroidScene(label: "Sample Scene", androidTheme: "@style/MyTheme.NoActionBar", isExported: true, isLauncher: false, iconResource: "@drawable/icon")]
    public partial class SampleScene : Scene
    {
        public SampleScene()
        {
            InitializeComponent();
        }

        private void CloseScene_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
