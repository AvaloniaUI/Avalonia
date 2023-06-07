using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RenderDemo.ViewModels;

namespace RenderDemo.Pages
{
    public class TransitionsPage : UserControl
    {
        public TransitionsPage()
        {
            InitializeComponent();
            this.DataContext = new AnimationsPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
