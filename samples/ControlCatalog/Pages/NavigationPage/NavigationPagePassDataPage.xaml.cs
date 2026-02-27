using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPagePassDataPage : UserControl
    {
        private record Contact(string Name, string Occupation, string Country, Color Color);

        private static readonly Contact[] Contacts =
        {
            new("Alice Johnson", "Software Engineer", "United States", Color.Parse("#4CAF50")),
            new("Bob Smith", "Product Designer", "Canada", Color.Parse("#2196F3")),
            new("Carol White", "Data Scientist", "United Kingdom", Color.Parse("#9C27B0")),
            new("David Lee", "DevOps Engineer", "Australia", Color.Parse("#FF9800")),
            new("Emma Brown", "UX Researcher", "Germany", Color.Parse("#F44336")),
        };

        public NavigationPagePassDataPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(CreateContactListPage(), null);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private ContentPage CreateContactListPage()
        {
            var list = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(16) };

            foreach (var contact in Contacts)
            {
                var card = CreateContactCard(contact);
                list.Children.Add(card);
            }

            return new ContentPage
            {
                Header = "Contacts",
                Content = new ScrollViewer { Content = list },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
        }

        private Button CreateContactCard(Contact contact)
        {
            var initials = string.Concat(contact.Name.Split(' ')[0][0], contact.Name.Split(' ')[1][0]).ToString();

            var card = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Avalonia.Thickness(12, 8),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children =
                    {
                        new Border
                        {
                            Width = 40, Height = 40,
                            CornerRadius = new Avalonia.CornerRadius(20),
                            Background = new SolidColorBrush(contact.Color),
                            Child = new TextBlock
                            {
                                Text = initials,
                                Foreground = Brushes.White,
                                FontWeight = FontWeight.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        },
                        new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                new TextBlock { Text = contact.Name, FontWeight = FontWeight.SemiBold },
                                new TextBlock { Text = contact.Occupation, FontSize = 12, Opacity = 0.7 }
                            }
                        }
                    }
                }
            };

            card.Click += (s, e) => NavigateToDetail(contact);
            return card;
        }

        private void NavigateToDetail(Contact contact)
        {
            ContentPage detailPage;

            var pageBg = new SolidColorBrush(Color.FromArgb(30, contact.Color.R, contact.Color.G, contact.Color.B));
            if (MethodCombo.SelectedIndex == 1)
            {
                // Via DataContext
                detailPage = new ContentPage
                {
                    Header = contact.Name,
                    Background = pageBg,
                    DataContext = contact,
                    Content = CreateDetailContent(contact)
                };
            }
            else
            {
                // Via Constructor argument
                detailPage = new ContentPage
                {
                    Header = contact.Name,
                    Background = pageBg,
                    Content = CreateDetailContent(contact)
                };
            }

            detailPage.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            detailPage.VerticalContentAlignment = VerticalAlignment.Stretch;
            DemoNav.Push(detailPage);
        }

        private static Panel CreateDetailContent(Contact contact) =>
            new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new Border
                    {
                        Width = 72, Height = 72,
                        CornerRadius = new Avalonia.CornerRadius(36),
                        Background = new SolidColorBrush(contact.Color),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = string.Concat(contact.Name.Split(' ')[0][0], contact.Name.Split(' ')[1][0]).ToString(),
                            Foreground = Brushes.White,
                            FontSize = 28,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock
                    {
                        Text = contact.Name,
                        FontSize = 22,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = contact.Occupation,
                        FontSize = 14,
                        Opacity = 0.7,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = contact.Country,
                        FontSize = 13,
                        Opacity = 0.5,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            };
    }
}
