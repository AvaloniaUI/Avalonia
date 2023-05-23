using System.Windows;
using Avalonia;
using Avalonia.Controls;
using ControlCatalog;

namespace WindowsInteropTest
{
    public partial class EmbedToWpfDemo
    {
        public EmbedToWpfDemo()
        {
            InitializeComponent();
            Host.Content = new MainView();

            var btn = (Button) RightBtn.Content!;
            btn.Click += delegate
            {
                btn.Content += "!";
            };

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TopLevel.GetTopLevel((MainView)Host.Content)!.AttachDevTools();
        }
    }
}
