using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Platform;
using ControlCatalog.Pages;

namespace ControlCatalog
{
    public class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
            if (AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().IsDesktop)
            {
                IList tabItems = ((IList)this.FindControl<TabControl>("Sidebar").Items);
                tabItems.Add(new TabItem()
                {
                    Header = "Dialogs",
                    Content = new DialogsPage()
                });
                tabItems.Add(new TabItem()
                {
                    Header = "Screens",
                    Content = new ScreenPage()
                });

            }

            var decorations = this.Find<ComboBox>("Decorations");
            decorations.SelectionChanged += (sender, e) =>
            {
                if (VisualRoot is Window window)
                    window.SystemDecorations = (SystemDecorations)decorations.SelectedIndex;
            };

            var transparencyLevels = this.Find<ComboBox>("TransparencyLevels");
            transparencyLevels.SelectionChanged += (sender, e) =>
            {
                if (VisualRoot is Window window)
                    window.TransparencyLevelHint = (WindowTransparencyLevel)transparencyLevels.SelectedIndex;
            };
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var decorations = this.Find<ComboBox>("Decorations");
            if (VisualRoot is Window window)
                decorations.SelectedIndex = (int)window.SystemDecorations;
        }
    }
}
