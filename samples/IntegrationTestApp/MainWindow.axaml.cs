using System.Collections.Generic;
using Avalonia.Controls;
using IntegrationTestApp.Models;
using IntegrationTestApp.Pages;
using IntegrationTestApp.ViewModels;

namespace IntegrationTestApp
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new MainWindowViewModel(CreatePages());
            InitializeViewMenu(viewModel.Pages);

            DataContext = viewModel;
            AppOverlayPopups.Text = Program.OverlayPopups ? "Overlay Popups" : "Native Popups";
        }

        private MainWindowViewModel? ViewModel => (MainWindowViewModel?)DataContext;

        private void InitializeViewMenu(IEnumerable<Page> pages)
        {
            var mainTabs = this.Get<TabControl>("MainTabs");
            var viewMenu = (NativeMenuItem?)NativeMenu.GetMenu(this)?.Items[1];

            foreach (var page in pages)
            {
                var menuItem = new NativeMenuItem
                {
                    Header = (string?)page.Name,
                    ToolTip = $"Tip:{(string?)page.Name}",
                    ToggleType = NativeMenuItemToggleType.Radio,
                };

                menuItem.Click += (_, _) =>
                {
                    if (ViewModel is { } viewModel)
                        viewModel.SelectedPage = page;
                };

                viewMenu?.Menu?.Items.Add(menuItem);
            }
        }

        private void Pager_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Pager.SelectedItem is Page page)
                PagerContent.Child = page.CreateContent();
        }

        private static IEnumerable<Page> CreatePages()
        {
            return
            [
                new("Automation", () => new AutomationPage()),
                new("Button", () => new ButtonPage()),
                new("CheckBox", () => new CheckBoxPage()),
                new("ComboBox", () => new ComboBoxPage()),
                new("ContextMenu", () => new ContextMenuPage()),
                new("DesktopPage", () => new DesktopPage()),
                new("Gestures", () => new GesturesPage()),
                new("ListBox", () => new ListBoxPage()),
                new("Menu", () => new MenuPage()),
                new("Pointer", () => new PointerPage()),
                new("RadioButton", () => new RadioButtonPage()),
                new("Screens", () => new ScreensPage()),
                new("ScrollBar", () => new ScrollBarPage()),
                new("Slider", () => new SliderPage()),
                new("Window Decorations", () => new WindowDecorationsPage()),
                new("Window", () => new WindowPage()),
            ];
        }
    }
}
