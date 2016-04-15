using BindingTest.ViewModels;
using Perspex;
using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace BindingTest
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = new MainWindowViewModel();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            this.LoadFromXaml();
        }
    }
}
