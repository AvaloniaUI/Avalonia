namespace ControlGallery.WinUI
{
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AvaloniaPanel.Content = App.Lifetime.MainView;
        }
    }
}
