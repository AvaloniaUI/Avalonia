using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.CodeAnalysis;

namespace IntegrationTestApp
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeViewMenu();
            InitializeGesturesTab();
            this.AttachDevTools();

            var overlayPopups = this.Get<TextBlock>("AppOverlayPopups");
            overlayPopups.Text = Program.OverlayPopups ? "Overlay Popups" : "Native Popups";

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
            var viewMenu = (NativeMenuItem?)NativeMenu.GetMenu(this)?.Items[1];

            foreach (var tabItem in mainTabs.Items.Cast<TabItem>())
            {
                var menuItem = new NativeMenuItem
                {
                    Header = (string?)tabItem.Header,
                    ToolTip = (string?)tabItem.Header,
                    IsChecked = tabItem.IsSelected,
                    ToggleType = NativeMenuItemToggleType.Radio,
                };

                menuItem.Click += (_, _) => tabItem.IsSelected = true;
                viewMenu?.Menu?.Items.Add(menuItem);
            }
        }

        private void ShowWindow()
        {
            var sizeTextBox = this.GetControl<TextBox>("ShowWindowSize");
            var modeComboBox = this.GetControl<ComboBox>("ShowWindowMode");
            var locationComboBox = this.GetControl<ComboBox>("ShowWindowLocation");
            var stateComboBox = this.GetControl<ComboBox>("ShowWindowState");
            var size = !string.IsNullOrWhiteSpace(sizeTextBox.Text) ? Size.Parse(sizeTextBox.Text) : (Size?)null;
            var systemDecorations = this.GetControl<ComboBox>("ShowWindowSystemDecorations");
            var extendClientArea = this.GetControl<CheckBox>("ShowWindowExtendClientAreaToDecorationsHint");
            var canResizeCheckBox = this.GetControl<CheckBox>("ShowWindowCanResize");
            var owner = (Window)this.GetVisualRoot()!;

            var window = new ShowWindowTest
            {
                WindowStartupLocation = (WindowStartupLocation)locationComboBox.SelectedIndex,
                CanResize = canResizeCheckBox.IsChecked ?? false,
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                // Make sure the windows have unique names and AutomationIds.
                var existing = lifetime.Windows.OfType<ShowWindowTest>().Count();
                if (existing > 0)
                {
                    AutomationProperties.SetAutomationId(window, window.Name + (existing + 1));
                    window.Title += $" {existing + 1}";
                }
            }
            
            if (size.HasValue)
            {
                window.Width = size.Value.Width;
                window.Height = size.Value.Height;
            }

            sizeTextBox.Text = string.Empty;
            window.ExtendClientAreaToDecorationsHint = extendClientArea.IsChecked ?? false;
            window.SystemDecorations = (SystemDecorations)systemDecorations.SelectedIndex;
            window.WindowState = (WindowState)stateComboBox.SelectedIndex;

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

        private void ShowTransparentWindow()
        {
            // Show a background window to make sure the color behind the transparent window is
            // a known color (green).
            var backgroundWindow = new Window
            {
                Title = "Transparent Window Background",
                Name = "TransparentWindowBackground",
                Width = 300,
                Height = 300,
                Background = Brushes.Green,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // This is the transparent window with a red circle.
            var window = new Window
            {
                Title = "Transparent Window",
                Name = "TransparentWindow",
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Red,
                    CornerRadius = new CornerRadius(100),
                }
            };

            window.PointerPressed += (_, _) =>
            {
                window.Close();
                backgroundWindow.Close();
            };

            backgroundWindow.Show(this);
            window.Show(backgroundWindow);
        }

        private void ShowTransparentPopup()
        {
            var popup = new Popup
            {
                WindowManagerAddShadowHint = false,
                Placement = PlacementMode.AnchorAndGravity,
                PlacementAnchor = PopupAnchor.Top,
                PlacementGravity = PopupGravity.Bottom,
                Width= 200,
                Height= 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                    CornerRadius = new CornerRadius(100),
                }
            };

            // Show a background window to make sure the color behind the transparent window is
            // a known color (green).
            var backgroundWindow = new Window
            {
                Title = "Transparent Popup Background",
                Name = "TransparentPopupBackground",
                Width = 200,
                Height = 200,
                Background = Brushes.Green,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new Border
                {
                    Name = "PopupContainer",
                    Child = popup,
                    [AutomationProperties.AccessibilityViewProperty] = AccessibilityView.Content,
                }
            };

            backgroundWindow.PointerPressed += (_, _) => backgroundWindow.Close();
            backgroundWindow.Show(this);

            popup.Open();
        }

        private void SendToBack()
        {
            var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            foreach (var window in lifetime.Windows.ToArray())
            {
                window.Activate();
            }
        }

        private void RestoreAll()
        {
            var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            foreach (var window in lifetime.Windows.ToArray())
            {
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
            }
        }

        private void InitializeGesturesTab()
        {
            var gestureBorder = this.GetControl<Border>("GestureBorder");
            var gestureBorder2 = this.GetControl<Border>("GestureBorder2");
            var lastGesture = this.GetControl<TextBlock>("LastGesture");
            var resetGestures = this.GetControl<Button>("ResetGestures");
            gestureBorder.Tapped += (_, _) => lastGesture.Text = "Tapped";
            
            gestureBorder.DoubleTapped += (_, _) =>
            {
                lastGesture.Text = "DoubleTapped";

                // Testing #8733
                gestureBorder.IsVisible = false;
                gestureBorder2.IsVisible = true;
            };

            gestureBorder2.DoubleTapped += (_, _) =>
            {
                lastGesture.Text = "DoubleTapped2";
            };

            Gestures.AddRightTappedHandler(gestureBorder, (_, _) => lastGesture.Text = "RightTapped");
            
            resetGestures.Click += (_, _) =>
            {
                lastGesture.Text = string.Empty;
                gestureBorder.IsVisible = true;
                gestureBorder2.IsVisible = false;
            };
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
            if (source?.Name == "ResetSliders")
                this.Get<Slider>("HorizontalSlider").Value = 50;
            if (source?.Name == "ShowTransparentWindow")
                ShowTransparentWindow();
            if (source?.Name == "ShowTransparentPopup")
                ShowTransparentPopup();
            if (source?.Name == "ShowWindow")
                ShowWindow();
            if (source?.Name == "SendToBack")
                SendToBack();
            if (source?.Name == "EnterFullscreen")
                WindowState = WindowState.FullScreen;
            if (source?.Name == "ExitFullscreen")
                WindowState = WindowState.Normal;
            if (source?.Name == "RestoreAll")
                RestoreAll();
        }
    }
}
