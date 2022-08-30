using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace IntegrationTestApp
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeViewMenu();
            this.AttachDevTools();
            AddHandler(Button.ClickEvent, OnButtonClick);
            ListBoxItems = Enumerable.Range(0, 100).Select(x => "Item " + x).ToList();
            DataContext = this;
        }

        public List<string> ListBoxItems { get; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeViewMenu()
        {
            var mainTabs = this.Get<TabControl>("MainTabs");
            var viewMenu = (NativeMenuItem)NativeMenu.GetMenu(this).Items[1];

            if (mainTabs.Items is not null)
            {
                foreach (TabItem tabItem in mainTabs.Items)
                {
                    var menuItem = new NativeMenuItem
                    {
                        Header = (string)tabItem.Header!,
                        IsChecked = tabItem.IsSelected,
                        ToggleType = NativeMenuItemToggleType.Radio,
                    };

                    menuItem.Click += (s, e) => tabItem.IsSelected = true;
                    viewMenu?.Menu?.Items.Add(menuItem);
                }
            }
        }

        private void ShowWindow()
        {
            var sizeTextBox = this.GetControl<TextBox>("ShowWindowSize");
            var modeComboBox = this.GetControl<ComboBox>("ShowWindowMode");
            var locationComboBox = this.GetControl<ComboBox>("ShowWindowLocation");
            var size = !string.IsNullOrWhiteSpace(sizeTextBox.Text) ? Size.Parse(sizeTextBox.Text) : (Size?)null;
            var owner = (Window)this.GetVisualRoot()!;

            var window = new ShowWindowTest
            {
                WindowStartupLocation = (WindowStartupLocation)locationComboBox.SelectedIndex,
            };

            if (size.HasValue)
            {
                window.Width = size.Value.Width;
                window.Height = size.Value.Height;
            }

            sizeTextBox.Text = string.Empty;

            switch (modeComboBox.SelectedIndex)
            {
                case 0:
                    window.Show();
                    break;
                case 1:
                    window.Show(owner);
                    break;
                case 2:
                    window.ShowDialog(owner);
                    break;
            }
        }

        private void SendToBack()
        {
            var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            foreach (var window in lifetime.Windows)
            {
                window.Activate();
            }
        }

        private void RestoreAll()
        {
            var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            foreach (var window in lifetime.Windows)
            {
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
            }
        }

        private void MenuClicked(object? sender, RoutedEventArgs e)
        {
            var clickedMenuItemTextBlock = this.Get<TextBlock>("ClickedMenuItem");
            clickedMenuItemTextBlock.Text = (sender as MenuItem)?.Header?.ToString();
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            var source = e.Source as Button;

            if (source?.Name == "ComboBoxSelectionClear")
                this.Get<ComboBox>("BasicComboBox").SelectedIndex = -1;
            if (source?.Name == "ComboBoxSelectFirst")
                this.Get<ComboBox>("BasicComboBox").SelectedIndex = 0;
            if (source?.Name == "ListBoxSelectionClear")
                this.Get<ListBox>("BasicListBox").SelectedIndex = -1;
            if (source?.Name == "MenuClickedMenuItemReset")
                this.Get<TextBlock>("ClickedMenuItem").Text = "None";
            if (source?.Name == "ShowWindow")
                ShowWindow();
            if (source?.Name == "SendToBack")
                SendToBack();
            if (source?.Name == "ExitFullscreen")
                WindowState = WindowState.Normal;
            if (source?.Name == "RestoreAll")
                RestoreAll();
        }
    }
}
