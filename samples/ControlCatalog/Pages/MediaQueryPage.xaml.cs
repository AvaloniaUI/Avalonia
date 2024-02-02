using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media.Immutable;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class MediaQueryPage : UserControl
    {
        public MediaQueryPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
