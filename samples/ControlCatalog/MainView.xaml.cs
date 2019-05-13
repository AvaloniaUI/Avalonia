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

            var light = new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default")
            };
            var dark = new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("resm:Avalonia.Themes.Default.Accents.BaseDark.xaml?assembly=Avalonia.Themes.Default")
            };

            
            var themes = this.Find<ComboBox>("Themes");
            themes.SelectionChanged += (sender, e) =>
            {
                switch (themes.SelectedIndex)
                {
                    case 0:
                        Styles[0] = light;
                        break;
                    case 1:
                        Styles[0] = dark;
                        break;
                }
            };
            Styles.Add(light);
        }
    }
}
