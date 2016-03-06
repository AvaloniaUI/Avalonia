using BindingTest.ViewModels;
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
            App.AttachDevTools(this);
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
