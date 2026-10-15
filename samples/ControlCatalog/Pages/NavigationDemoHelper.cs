using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Shared helpers for ControlCatalog demo pages.
    /// </summary>
    internal static class NavigationDemoHelper
    {
        /// <summary>
        /// Pastel background brushes cycled by page index.
        /// </summary>
        internal static readonly IBrush[] PageBrushes =
        {
            new SolidColorBrush(Color.Parse("#BBDEFB")),
            new SolidColorBrush(Color.Parse("#C8E6C9")),
            new SolidColorBrush(Color.Parse("#FFE0B2")),
            new SolidColorBrush(Color.Parse("#E1BEE7")),
            new SolidColorBrush(Color.Parse("#FFCDD2")),
            new SolidColorBrush(Color.Parse("#B2EBF2")),
        };

        internal static IBrush GetPageBrush(int index) =>
            PageBrushes[((index % PageBrushes.Length) + PageBrushes.Length) % PageBrushes.Length];

        /// <summary>
        /// Creates a simple demo ContentPage with a centered title and subtitle.
        /// </summary>
        internal static ContentPage MakePage(string header, string body, int colorIndex) =>
            new ContentPage
            {
                Header = header,
                Background = GetPageBrush(colorIndex),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = header,
                            FontSize = 20,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = body,
                            FontSize = 13,
                            Opacity = 0.7,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 260
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

        /// <summary>
        /// Creates a demo ContentPage with an icon, title, body, and hint text
        /// (used by DrawerPage detail pages).
        /// </summary>
        internal static ContentPage MakeSectionPage(
            string header, string iconData, string title, string body,
            int colorIndex, string? hint = null)
        {
            var panel = new StackPanel { Margin = new Thickness(24, 20), Spacing = 12 };

            panel.Children.Add(new PathIcon
            {
                Width = 48,
                Height = 48,
                Data = Geometry.Parse(iconData),
                Foreground = new SolidColorBrush(Color.Parse("#0078D4"))
            });
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 26,
                FontWeight = FontWeight.Bold
            });
            panel.Children.Add(new TextBlock
            {
                Text = body,
                FontSize = 14,
                Opacity = 0.8,
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new Separator { Margin = new Thickness(0, 8) });

            if (hint != null)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = hint,
                    FontSize = 12,
                    Opacity = 0.45,
                    FontStyle = FontStyle.Italic,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            return new ContentPage
            {
                Header = header,
                Background = GetPageBrush(colorIndex),
                Content = new ScrollViewer { Content = panel },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
        }

        private static readonly Geometry CloseIcon = Geometry.Parse(
            "M4.397 4.397a1 1 0 0 1 1.414 0L12 10.585l6.19-6.188a1 1 0 0 1 1.414 1.414L13.413 12l6.19 6.189a1 1 0 0 1-1.414 1.414L12 13.413l-6.189 6.19a1 1 0 0 1-1.414-1.414L10.585 12 4.397 5.811a1 1 0 0 1 0-1.414z");

        /// <summary>
        /// Builds the demo gallery home page for NavigationPage/TabbedPage/DrawerPage demo registries.
        /// </summary>
        internal static ContentPage CreateGalleryHomePage(
            NavigationPage nav,
            (string Group, string Title, string Description, Func<UserControl> Factory)[] demos)
        {
            var stack = new StackPanel { Margin = new Thickness(12), Spacing = 16 };

            var groups = new Dictionary<string, WrapPanel>();
            var groupOrder = new List<string>();

            foreach (var (group, title, description, factory) in demos)
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
                    Margin = new Thickness(0, 0, 8, 8),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    Padding = new Thickness(12, 8),
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

                card.Click += async (_, _) =>
                {
                    var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
                    headerGrid.Children.Add(new TextBlock
                    {
                        Text = demoTitle,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    var closeBtn = new Button
                    {
                        Content = new PathIcon { Data = CloseIcon },
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 4),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(closeBtn, 1);
                    headerGrid.Children.Add(closeBtn);
                    closeBtn.Click += async (_, _) => await nav.PopAsync(null);

                    var page = new ContentPage
                    {
                        Header = headerGrid,
                        Content = demoFactory(),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch
                    };
                    NavigationPage.SetHasBackButton(page, false);
                    await nav.PushAsync(page, null);
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
                    Margin = new Thickness(0, 0, 0, 4),
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
