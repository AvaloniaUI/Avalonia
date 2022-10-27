using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class NavigationControlPage : UserControl
    {

        private NavigationControl? _navigationControl;

        public NavigationControlPage()
        {
            InitializeComponent();

            DataContext = new NavigationPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
