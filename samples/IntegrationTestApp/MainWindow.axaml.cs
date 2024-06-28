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
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace IntegrationTestApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Set name in code behind, so source generator will ignore it.
            Name = "MainWindow";

            InitializeComponent();
            InitializeViewMenu();
            InitializeGesturesTab();
            this.AttachDevTools();

            AppOverlayPopups.Text = Program.OverlayPopups ? "Overlay Popups" : "Native Popups";

            AddHandler(Button.ClickEvent, OnButtonClick);
            ListBoxItems = Enumerable.Range(0, 100).Select(x => "Item " + x).ToList();
            DataContext = this;
        }

        public List<string> ListBoxItems { get; }

        private void InitializeViewMenu()
        {
            var viewMenu = (NativeMenuItem?)NativeMenu.GetMenu(this)?.Items[1];

            foreach (var tabItem in MainTabs.Items.Cast<TabItem>())
            {
                var menuItem = new NativeMenuItem
                {
                    Header = (string?)tabItem.Header,
                    ToolTip = $"Tip:{(string?)tabItem.Header}",
                    IsChecked = tabItem.IsSelected,
                    ToggleType = NativeMenuItemToggleType.Radio,
                };

                menuItem.Click += (_, _) => tabItem.IsSelected = true;
                viewMenu?.Menu?.Items.Add(menuItem);
            }
        }

        private void OnShowWindow()
        {
            var sizeTextBox = ShowWindowSize;
            var modeComboBox = ShowWindowMode;
            var locationComboBox = ShowWindowLocation;
            var stateComboBox = ShowWindowState;
            var size = !string.IsNullOrWhiteSpace(sizeTextBox.Text) ? Size.Parse(sizeTextBox.Text) : (Size?)null;
            var systemDecorations = ShowWindowSystemDecorations;
            var extendClientArea = ShowWindowExtendClientAreaToDecorationsHint;
            var canResizeCheckBox = ShowWindowCanResize;
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

        private void OnShowTransparentWindow()
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

        private void OnShowTransparentPopup()
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

        private void OnSendToBack()
        {
            var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            foreach (var window in lifetime.Windows.ToArray())
            {
                window.Activate();
            }
        }

        private void OnRestoreAll()
        {
            var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            foreach (var window in lifetime.Windows.ToArray())
            {
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
            }
        }
        
        private void OnShowTopmostWindow()
        {
            var mainWindow = new TopmostWindowTest("OwnerWindow") { Topmost = true, Title = "Owner Window"};
            var ownedWindow = new TopmostWindowTest("OwnedWindow") { WindowStartupLocation = WindowStartupLocation.CenterOwner, Title = "Owned Window"};
            mainWindow.Show();
            
            ownedWindow.Show(mainWindow);
        }

        private void InitializeGesturesTab()
        {
            var gestureBorder = GestureBorder;
            var gestureBorder2 = GestureBorder2;
            var lastGesture = LastGesture;
            var resetGestures = ResetGestures;
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
            var clickedMenuItemTextBlock = ClickedMenuItem;
            clickedMenuItemTextBlock.Text = (sender as MenuItem)?.Header?.ToString();
        }

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            var source = e.Source as Button;

            if (source?.Name == nameof(ComboBoxSelectionClear))
                BasicComboBox.SelectedIndex = -1;
            if (source?.Name == nameof(ComboBoxSelectFirst))
                BasicComboBox.SelectedIndex = 0;
            if (source?.Name == nameof(ListBoxSelectionClear))
                BasicListBox.SelectedIndex = -1;
            if (source?.Name == nameof(MenuClickedMenuItemReset))
                ClickedMenuItem.Text = "None";
            if (source?.Name == nameof(ResetSliders))
                HorizontalSlider.Value = 50;
            if (source?.Name == nameof(ShowTransparentWindow))
                OnShowTransparentWindow();
            if (source?.Name == nameof(ShowTransparentPopup))
                OnShowTransparentPopup();
            if (source?.Name == nameof(ShowWindow))
                OnShowWindow();
            if (source?.Name == nameof(SendToBack))
                OnSendToBack();
            if (source?.Name == nameof(EnterFullscreen))
                WindowState = WindowState.FullScreen;
            if (source?.Name == nameof(ExitFullscreen))
                WindowState = WindowState.Normal;
            if (source?.Name == nameof(ShowTopmostWindow))
                OnShowTopmostWindow();
            if (source?.Name == nameof(RestoreAll))
                OnRestoreAll();
            if (source?.Name == nameof(ApplyWindowDecorations))
                OnApplyWindowDecorations(this);
            if (source?.Name == nameof(ShowNewWindowDecorations))
                OnShowNewWindowDecorations();
        }

        private void OnApplyWindowDecorations(Window window)
        {
            window.ExtendClientAreaToDecorationsHint = WindowExtendClientAreaToDecorationsHint.IsChecked!.Value;
            window.ExtendClientAreaTitleBarHeightHint =
                int.TryParse(WindowTitleBarHeightHint.Text, out var val) ? val / DesktopScaling : -1;
            window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome
                | (WindowForceSystemChrome.IsChecked == true ? ExtendClientAreaChromeHints.SystemChrome : 0)
                | (WindowPreferSystemChrome.IsChecked == true ? ExtendClientAreaChromeHints.PreferSystemChrome : 0)
                | (WindowMacThickSystemChrome.IsChecked == true ? ExtendClientAreaChromeHints.OSXThickTitleBar : 0);
            AdjustOffsets(window);

            window.Background = Brushes.Transparent;
            window.PropertyChanged += WindowOnPropertyChanged;

            void WindowOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                var window = (Window)sender!;
                if (e.Property == OffScreenMarginProperty || e.Property == WindowDecorationMarginProperty)
                {
                    AdjustOffsets(window);
                }
            }

            void AdjustOffsets(Window window)
            {
                window.Padding = window.OffScreenMargin;
                ((Control)window.Content!).Margin = window.WindowDecorationMargin;

                WindowDecorationProperties.Text =
                    $"{window.OffScreenMargin.Top * DesktopScaling} {window.WindowDecorationMargin.Top * DesktopScaling} {window.IsExtendedIntoWindowDecorations}";
            }
        }

        private void OnShowNewWindowDecorations()
        {
            var window = new ShowWindowTest();
            OnApplyWindowDecorations(window);
            window.Show();
        }
    }
}
