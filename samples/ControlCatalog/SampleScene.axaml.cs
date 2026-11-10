using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
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
