using BindingDemo.ViewModels;
using Avalonia.Controls;

namespace BindingDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
