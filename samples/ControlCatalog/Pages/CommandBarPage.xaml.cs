using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class CommandBarPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            // Overview
            ("Overview", "First Look",     "A CommandBar with primary commands, secondary overflow menu, and custom content area.", () => new CommandBarFirstLookPage()),
            ("Overview", "Toggle Buttons", "CommandBarToggleButton for stateful actions like Bold, Italic, and Favorite.",              () => new CommandBarTogglePage()),

            // Appearance
            ("Appearance", "Label Positions", "Configure label position: Bottom (default), Right, or Collapsed (icon only).",       () => new CommandBarLabelPositionPage()),
            ("Appearance", "Customization",   "Background, Foreground, BorderBrush, BorderThickness, and CornerRadius.",            () => new CommandBarCustomizationPage()),

            // Features
            ("Features", "Overflow Menu",    "Secondary commands appear in an overflow popup. Configure visibility and sticky behavior.", () => new CommandBarOverflowPage()),
            ("Features", "Dynamic Overflow", "IsDynamicOverflowEnabled moves primary commands to overflow as space shrinks.",             () => new CommandBarDynamicOverflowPage()),
            ("Features", "Events & State",  "Observe Opening, Opened, Closing, and Closed while tracking IsOpen, HasSecondaryCommands, and IsOverflowButtonVisible.", () => new CommandBarEventsPage()),
        };

        public CommandBarPage()
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

            NavigationPage.SetHasNavigationBar(homePage, false);

            return homePage;
        }
    }
}
