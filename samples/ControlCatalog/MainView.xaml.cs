using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Platform;
using ControlCatalog.Pages;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            DataContext = new MainWindowViewModel();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            var topLevel = this.VisualRoot as TopLevel;

            new ManagedPointer(topLevel);
        }
    }
}
