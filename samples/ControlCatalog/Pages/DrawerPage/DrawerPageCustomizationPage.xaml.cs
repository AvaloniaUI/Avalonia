using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageCustomizationPage : UserControl
    {
        private bool _isLoaded;

        private static readonly string[] _iconPaths =
        {
            // 0 - 3 lines (default hamburger)
            "M3 17h18a1 1 0 0 1 .117 1.993L21 19H3a1 1 0 0 1-.117-1.993L3 17h18H3Zm0-6 18-.002a1 1 0 0 1 .117 1.993l-.117.007L3 13a1 1 0 0 1-.117-1.993L3 11l18-.002L3 11Zm0-6h18a1 1 0 0 1 .117 1.993L21 7H3a1 1 0 0 1-.117-1.993L3 5h18H3Z",
            // 1 - 2 lines
            "M3,13H21V11H3M3,6V8H21V6",
            // 2 - 4 squares
            "M3,11H11V3H3M3,21H11V13H3M13,21H21V13H13M13,3V11H21V3",
        };

        public DrawerPageCustomizationPage()
        {
            InitializeComponent();
            EnableMouseSwipeGesture(DemoDrawer);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _isLoaded = true;
        }

        private void OnToggleDrawer(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.IsOpen = !DemoDrawer.IsOpen;
        }

        private void OnBehaviorChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerBehavior = BehaviorCombo.SelectedIndex switch
            {
                0 => DrawerBehavior.Auto,
                1 => DrawerBehavior.Flyout,
                2 => DrawerBehavior.Locked,
                3 => DrawerBehavior.Disabled,
                _ => DrawerBehavior.Auto
            };
        }

        private void OnLayoutChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLayoutBehavior = LayoutCombo.SelectedIndex switch
            {
                0 => DrawerLayoutBehavior.Overlay,
                1 => DrawerLayoutBehavior.Split,
                2 => DrawerLayoutBehavior.CompactOverlay,
                3 => DrawerLayoutBehavior.CompactInline,
                _ => DrawerLayoutBehavior.Overlay
            };
        }

        private void OnPlacementChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerPlacement = PlacementCombo.SelectedIndex switch
            {
                1 => DrawerPlacement.Right,
                2 => DrawerPlacement.Top,
                3 => DrawerPlacement.Bottom,
                _ => DrawerPlacement.Left
            };
        }

        private void OnGestureToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (sender is CheckBox check)
                DemoDrawer.IsGestureEnabled = check.IsChecked == true;
        }

        private void OnDrawerLengthChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerLength = e.NewValue;
            DrawerLengthText.Text = ((int)e.NewValue).ToString();
        }

        private void OnDrawerBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerBackground = DrawerBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.SlateBlue),
                2 => new SolidColorBrush(Colors.DarkCyan),
                3 => new SolidColorBrush(Colors.DarkRed),
                4 => new SolidColorBrush(Colors.DarkGreen),
                _ => null
            };
        }

        private void OnHeaderBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerHeaderBackground = HeaderBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.DodgerBlue),
                2 => new SolidColorBrush(Colors.Orange),
                3 => new SolidColorBrush(Colors.Teal),
                4 => new SolidColorBrush(Colors.Purple),
                _ => null
            };
        }

        private void OnFooterBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerFooterBackground = FooterBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.DimGray),
                2 => new SolidColorBrush(Colors.DarkSlateBlue),
                3 => new SolidColorBrush(Colors.DarkOliveGreen),
                4 => new SolidColorBrush(Colors.Maroon),
                _ => null
            };
        }

        private void OnIconChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.DrawerIcon = Geometry.Parse(_iconPaths[IconCombo.SelectedIndex]);
        }

        private void OnBackdropChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;
            DemoDrawer.BackdropBrush = BackdropCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Color.FromArgb(102, 0, 0, 0)),
                2 => new SolidColorBrush(Color.FromArgb(179, 0, 0, 0)),
                3 => new SolidColorBrush(Color.FromArgb(102, 255, 255, 255)),
                _ => null
            };
        }

        private void OnShowHeaderToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (ShowHeaderCheck.IsChecked == true)
                DemoDrawer.DrawerHeader = HeaderTemplateCombo.SelectedIndex == 0 ? DrawerHeaderBorder : (object)"My Application";
            else
                DemoDrawer.DrawerHeader = null;
        }

        private void OnShowFooterToggled(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (ShowFooterCheck.IsChecked == true)
            {
                DemoDrawer.DrawerFooter = FooterTemplateCombo.SelectedIndex switch
                {
                    1 => (object)"v11.0",
                    2 => (object)"Avalonia",
                    _ => DrawerFooterBorder
                };
            }
            else
            {
                DemoDrawer.DrawerFooter = null;
            }
        }

        private void OnHeaderTemplateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            switch (HeaderTemplateCombo.SelectedIndex)
            {
                case 1:
                    DemoDrawer.DrawerHeader = "My Application";
                    DemoDrawer.DrawerHeaderTemplate = new FuncDataTemplate<string>((data, _) =>
                        new Border
                        {
                            Padding = new Avalonia.Thickness(16),
                            Child = new StackPanel
                            {
                                Spacing = 2,
                                Children =
                                {
                                    new TextBlock { Text = data, FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                                    new TextBlock { Text = "Navigation", FontSize = 12, Foreground = Brushes.White, Opacity = 0.7 }
                                }
                            }
                        });
                    break;

                case 2:
                    DemoDrawer.DrawerHeader = "My Application";
                    DemoDrawer.DrawerHeaderTemplate = new FuncDataTemplate<string>((data, _) =>
                    {
                        var initial = data?.Length > 0 ? data[0].ToString().ToUpperInvariant() : "?";
                        var avatar = new Border
                        {
                            Width = 40,
                            Height = 40,
                            CornerRadius = new Avalonia.CornerRadius(20),
                            Background = new SolidColorBrush(Color.Parse("#1976D2")),
                            Child = new TextBlock
                            {
                                Text = initial,
                                FontSize = 18,
                                FontWeight = FontWeight.Bold,
                                Foreground = Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        };
                        var label = new TextBlock { Text = data, FontSize = 14, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                        row.Children.Add(avatar);
                        row.Children.Add(label);
                        return new Border { Padding = new Avalonia.Thickness(12), Child = row };
                    });
                    break;

                default:
                    DemoDrawer.DrawerHeader = DrawerHeaderBorder;
                    DemoDrawer.DrawerHeaderTemplate = null;
                    break;
            }
        }

        private void OnFooterTemplateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            switch (FooterTemplateCombo.SelectedIndex)
            {
                case 1:
                    DemoDrawer.DrawerFooter = "v11.0";
                    DemoDrawer.DrawerFooterTemplate = new FuncDataTemplate<string>((data, _) =>
                        new Border
                        {
                            Padding = new Avalonia.Thickness(12, 8),
                            Child = new Border
                            {
                                Padding = new Avalonia.Thickness(8, 4),
                                CornerRadius = new Avalonia.CornerRadius(4),
                                Background = new SolidColorBrush(Color.Parse("#1976D2")),
                                Child = new TextBlock { Text = data, FontSize = 11, Foreground = Brushes.White, FontWeight = FontWeight.SemiBold }
                            }
                        });
                    break;

                case 2:
                    DemoDrawer.DrawerFooter = "Avalonia";
                    DemoDrawer.DrawerFooterTemplate = new FuncDataTemplate<string>((data, _) =>
                    {
                        var icon = new PathIcon
                        {
                            Width = 14,
                            Height = 14,
                            Data = Geometry.Parse("M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"),
                            Opacity = 0.5
                        };
                        var label = new TextBlock { Text = data, FontSize = 12, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center };
                        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                        row.Children.Add(icon);
                        row.Children.Add(label);
                        return new Border { Padding = new Avalonia.Thickness(14, 10), Child = row };
                    });
                    break;

                default:
                    DemoDrawer.DrawerFooter = DrawerFooterBorder;
                    DemoDrawer.DrawerFooterTemplate = null;
                    break;
            }
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;
            if (sender is not Button button) return;
            var item = button.Tag?.ToString() ?? "Home";

            DetailTitleText.Text = item;

            if (DemoDrawer.DrawerBehavior != DrawerBehavior.Locked)
                DemoDrawer.IsOpen = false;
        }

        private static void EnableMouseSwipeGesture(Control control)
        {
            var recognizer = control.GestureRecognizers
                .OfType<SwipeGestureRecognizer>()
                .FirstOrDefault();

            if (recognizer is not null)
                recognizer.IsMouseEnabled = true;
        }
    }
}
