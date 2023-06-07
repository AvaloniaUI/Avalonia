using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media.Immutable;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class PlatformInfoPage : UserControl
    {
        public PlatformInfoPage()
        {
            this.InitializeComponent();
            DataContext = new PlatformInformationViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
