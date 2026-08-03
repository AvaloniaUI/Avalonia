using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    internal sealed class SamplePageFactory : ISamplePageFactory
    {
        public ContentPage CreatePage(ViewModelBase viewModel) =>
            viewModel switch
            {
                WorkspaceViewModel workspace => CreateWorkspacePage(workspace),
                ProjectDetailViewModel detail => CreateProjectDetailPage(detail),
                ProjectActivityViewModel activity => CreateProjectActivityPage(activity),
                _ => throw new InvalidOperationException($"Unsupported view model: {viewModel.GetType().Name}")
            };

        private static ContentPage CreateWorkspacePage(WorkspaceViewModel viewModel)
        {
            var stack = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 14,
            };

            stack.Children.Add(new TextBlock
            {
                Text = viewModel.Title,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
            });
            stack.Children.Add(new TextBlock
            {
                Text = viewModel.Description,
                FontSize = 13,
                Opacity = 0.75,
                TextWrapping = TextWrapping.Wrap,
            });

            stack.Children.Add(new ItemsControl
            {
                ItemsSource = viewModel.Projects,
                ItemTemplate = new FuncDataTemplate<ProjectCardViewModel>((item, _) =>
                {
                    if (item == null)
                        return new TextBlock();

                    var accentBrush = new SolidColorBrush(item.AccentColor);
                    var statusBadge = new Border
                    {
                        Background = accentBrush,
                        CornerRadius = new CornerRadius(999),
                        Padding = new Thickness(10, 4),
                        Child = new TextBlock
                        {
                            Text = item.Status,
                            Foreground = Brushes.White,
                            FontSize = 11,
                            FontWeight = FontWeight.SemiBold,
                        }
                    };
                    DockPanel.SetDock(statusBadge, Dock.Right);

                    var header = new DockPanel();
                    header.Children.Add(statusBadge);
                    header.Children.Add(new TextBlock
                    {
                        Text = item.Name,
                        FontSize = 17,
                        FontWeight = FontWeight.SemiBold,
                    });

                    return new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(20, item.AccentColor.R, item.AccentColor.G, item.AccentColor.B)),
                        BorderBrush = accentBrush,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(14),
                        Margin = new Thickness(0, 0, 0, 8),
                        Child = new StackPanel
                        {
                            Spacing = 8,
                            Children =
                            {
                                header,
                                new TextBlock
                                {
                                    Text = item.Summary,
                                    FontSize = 13,
                                    Opacity = 0.72,
                                    TextWrapping = TextWrapping.Wrap,
                                },
                                new TextBlock
                                {
                                    Text = $"Owner: {item.Owner}  •  Next: {item.NextMilestone}",
                                    FontSize = 12,
                                    Opacity = 0.6,
                                    TextWrapping = TextWrapping.Wrap,
                                },
                                new Button
                                {
                                    Content = "Open Project",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Command = item.OpenCommand,
                                }
                            }
                        }
                    };
                })
            });

            var page = new ContentPage
            {
                Header = "Workspace",
                Content = new ScrollViewer { Content = stack },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };

            NavigationPage.SetHasBackButton(page, false);
            return page;
        }

        private static ContentPage CreateProjectDetailPage(ProjectDetailViewModel viewModel)
        {
            var accentBrush = new SolidColorBrush(viewModel.AccentColor);
            var panel = new StackPanel
            {
                Margin = new Thickness(24, 20),
                Spacing = 12,
            };

            panel.Children.Add(new Border
            {
                Background = accentBrush,
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(12, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = viewModel.Status,
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                }
            });
            panel.Children.Add(new TextBlock
            {
                Text = viewModel.Name,
                FontSize = 26,
                FontWeight = FontWeight.Bold,
            });
            panel.Children.Add(new TextBlock
            {
                Text = viewModel.Summary,
                FontSize = 14,
                Opacity = 0.78,
                TextWrapping = TextWrapping.Wrap,
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Owner: {viewModel.Owner}",
                FontSize = 13,
                Opacity = 0.68,
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Next milestone: {viewModel.NextMilestone}",
                FontSize = 13,
                Opacity = 0.68,
                TextWrapping = TextWrapping.Wrap,
            });
            panel.Children.Add(new Separator { Margin = new Thickness(0, 4) });
            panel.Children.Add(new TextBlock
            {
                Text = "This page is resolved by SamplePageFactory from a ProjectDetailViewModel. The view model only requests navigation through ISampleNavigationService.",
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
            });
            panel.Children.Add(new Button
            {
                Content = "Open Activity",
                HorizontalAlignment = HorizontalAlignment.Left,
                Command = viewModel.OpenActivityCommand,
            });

            return new ContentPage
            {
                Header = viewModel.Name,
                Background = new SolidColorBrush(Color.FromArgb(18, viewModel.AccentColor.R, viewModel.AccentColor.G, viewModel.AccentColor.B)),
                Content = new ScrollViewer { Content = panel },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
        }

        private static ContentPage CreateProjectActivityPage(ProjectActivityViewModel viewModel)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(24, 20),
                Spacing = 10,
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Activity Timeline",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Recent updates for {viewModel.Name}. This page was opened from a command on the detail view model.",
                FontSize = 13,
                Opacity = 0.74,
                TextWrapping = TextWrapping.Wrap,
            });

            foreach (var item in viewModel.Items)
            {
                panel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(14, viewModel.AccentColor.R, viewModel.AccentColor.G, viewModel.AccentColor.B)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, viewModel.AccentColor.R, viewModel.AccentColor.G, viewModel.AccentColor.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10),
                    Child = new TextBlock
                    {
                        Text = item,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap,
                    }
                });
            }

            return new ContentPage
            {
                Header = "Activity",
                Content = new ScrollViewer { Content = panel },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
        }
    }
}
