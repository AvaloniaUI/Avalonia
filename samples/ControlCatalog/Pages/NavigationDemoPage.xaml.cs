using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationDemoPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            // Overview
            ("Overview", "First Look",        "Basic NavigationPage with push/pop navigation and back button support.",                     () => new NavigationPageFirstLookPage()),
            ("Overview", "Modal Navigation",  "Push and pop modal pages that appear on top of the navigation stack.",                      () => new NavigationPageModalPage()),
            ("Overview", "Navigation Events", "Subscribe to Pushed, Popped, PoppedToRoot, ModalPushed, and ModalPopped events.",            () => new NavigationPageEventsPage()),

            // Appearance
            ("Appearance", "Bar Customization", "Customize the navigation bar background, foreground, shadow, and visibility.",            () => new NavigationPageAppearancePage()),
            ("Appearance", "Header",            "Set page header content: a string, icon, or any custom control in the navigation bar.",   () => new NavigationPageTitlePage()),

            // Data
            ("Data", "Pass Data", "Pass data during navigation via constructor arguments or DataContext.",                                 () => new NavigationPagePassDataPage()),

            // Features
            ("Features", "Attached Methods",    "Per-page navigation bar and back button control via static attached methods.",              () => new NavigationPageAttachedMethodsPage()),
            ("Features", "Back Button",         "Customize, hide, or intercept the back button.",                                           () => new NavigationPageBackButtonPage()),
            ("Features", "CommandBar",          "Add, remove and position CommandBar items inside the navigation bar or as a bottom bar.",  () => new NavigationPageToolbarPage()),
            ("Features", "Transitions",         "Configure page transitions: PageSlide, Parallax Slide, CrossFade, Fade Through, and more.", () => new NavigationPageTransitionsPage()),
            ("Features", "Modal Transitions",   "Configure modal transition: PageSlide from bottom, CrossFade, or None.",                   () => new NavigationPageModalTransitionsPage()),
            ("Features", "Stack Management",    "Remove or insert pages anywhere in the navigation stack at runtime.",                      () => new NavigationPageStackPage()),
            ("Features", "Interactive Header",  "Build a header with a title and live search box that filters page content in real time.",  () => new NavigationPageInteractiveHeaderPage()),
            ("Features", "Back Swipe Gesture",  "Swipe from the left edge to interactively pop the current page.",                         () => new NavigationPageGesturePage()),
            ("Features", "Scroll-Aware Bar",    "Hide the navigation bar on downward scroll and reveal it on upward scroll.",               () => new NavigationPageScrollAwarePage()),

            // Performance
            ("Performance", "Performance Monitor", "Track stack depth, live page instances, and managed heap size. Observe how memory is reclaimed after popping pages.", () => new NavigationPagePerformancePage()),

        };

        public NavigationDemoPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await SampleNav.PushAsync(CreateHomePage(), null);
        }

        private ContentPage CreateHomePage()
        {
            var stack = new StackPanel
            {
                Margin = new Avalonia.Thickness(12),
                Spacing = 16
            };

            // Build groups
            var groups = new Dictionary<string, WrapPanel>();
            var groupOrder = new List<string>();

            foreach (var (group, title, description, factory) in Demos)
            {
                if (!groups.ContainsKey(group))
                {
                    groups[group] = new WrapPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    groupOrder.Add(group);
                }

                var demoFactory = factory;
                var demoTitle = title;

                var card = new Button
                {
                    Width = 170,
                    MinHeight = 80,
                    Margin = new Avalonia.Thickness(0, 0, 8, 8),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    Padding = new Avalonia.Thickness(12, 8),
                    Content = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = title,
                                FontSize = 13,
                                FontWeight = FontWeight.SemiBold,
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = description,
                                FontSize = 11,
                                Opacity = 0.6,
                                TextWrapping = TextWrapping.Wrap
                            }
                        }
                    }
                };

                card.Click += async (s, e) =>
                {
                    var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
                    headerGrid.Children.Add(new TextBlock
                    {
                        Text = demoTitle,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    var closeBtn = new Button
                    {
                        Content = new PathIcon
                        {
                            Data = Geometry.Parse("M4.397 4.397a1 1 0 0 1 1.414 0L12 10.585l6.19-6.188a1 1 0 0 1 1.414 1.414L13.413 12l6.19 6.189a1 1 0 0 1-1.414 1.414L12 13.413l-6.189 6.19a1 1 0 0 1-1.414-1.414L10.585 12 4.397 5.811a1 1 0 0 1 0-1.414z")
                        },
                        Background = Brushes.Transparent,
                        BorderThickness = new Avalonia.Thickness(0),
                        Padding = new Avalonia.Thickness(8, 4),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(closeBtn, 1);
                    headerGrid.Children.Add(closeBtn);
                    closeBtn.Click += async (_, _) => await SampleNav.PopAsync(null);

                    var page = new ContentPage
                    {
                        Header = headerGrid,
                        Content = demoFactory(),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch
                    };
                    NavigationPage.SetHasBackButton(page, false);
                    await SampleNav.PushAsync(page, null);
                };

                groups[group].Children.Add(card);
            }

            foreach (var groupName in groupOrder)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = groupName,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Avalonia.Thickness(0, 0, 0, 4),
                    Opacity = 0.6
                });
                stack.Children.Add(groups[groupName]);
            }

            var homePage = new ContentPage
            {
                Content = new ScrollViewer { Content = stack },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            // Hide the navigation bar on the gallery root — the catalog header already shows the control name
            NavigationPage.SetHasNavigationBar(homePage, false);

            return homePage;
        }
    }
}
