using Avalonia.Controls;

namespace ControlCatalog
{
    [AndroidScene(label: "Sample Scene", androidTheme: "@style/MyTheme.NoActionBar", isExported: true, launchMode:AndroidSceneLaunchMode.SingleInstance, isLauncher: false, iconResource: "@drawable/icon", dataMimeTypes: ["image/*"],
       dataSchemes:["content", "file"], actions: ["android.intent.action.VIEW"], categories: [AndroidSceneCategory.Default, AndroidSceneCategory.Browsable])]
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
