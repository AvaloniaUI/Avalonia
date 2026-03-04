using System.Threading.Tasks;
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

        private bool _isLoaded;

        public NavigationPagePassDataPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            DemoNav.Pushed += (s, ev) => AppendNavigationLog($"Pushed → {ev.Page?.Header}");
            DemoNav.Popped += (s, ev) => AppendNavigationLog($"Popped ← {ev.Page?.Header}");

            await DemoNav.PushAsync(CreateContactListPage(), null);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private void OnMethodChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            if (MethodCombo.SelectedIndex == 0)
            {
                MethodDescription.Text = "Data is passed as a constructor argument to the detail page. The page stores the contact and displays its properties directly.";
            }
            else
            {
                MethodDescription.Text = "Data is passed by setting the new page's DataContext. This enables data binding in XAML to display the data automatically.";
            }
        }

        private ContentPage CreateContactListPage()
        {
            var list = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(16) };

            var header = new TextBlock
            {
                Text = "Contacts",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(0, 0, 0, 4),
            };
            list.Children.Add(header);

            var subtitle = new TextBlock
            {
                Text = "Tap a contact to navigate and pass its data to the detail page.",
                FontSize = 13,
                Opacity = 0.6,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 0, 0, 8),
            };
            list.Children.Add(subtitle);

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
                            Width = 44, Height = 44,
                            CornerRadius = new Avalonia.CornerRadius(22),
                            Background = new SolidColorBrush(contact.Color),
                            Child = new TextBlock
                            {
                                Text = initials,
                                Foreground = Brushes.White,
                                FontSize = 16,
                                FontWeight = FontWeight.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        },
                        new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Spacing = 2,
                            Children =
                            {
                                new TextBlock { Text = contact.Name, FontSize = 15, FontWeight = FontWeight.SemiBold },
                                new TextBlock { Text = $"{contact.Occupation} · {contact.Country}", FontSize = 12, Opacity = 0.6 }
                            }
                        }
                    }
                }
            };

            card.Click += async (s, e) => await NavigateToDetail(contact);
            return card;
        }

        private async Task NavigateToDetail(Contact contact)
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
                    Content = CreateDetailContent(contact, "DataContext")
                };
            }
            else
            {
                // Via Constructor argument
                detailPage = new ContentPage
                {
                    Header = contact.Name,
                    Background = pageBg,
                    Content = CreateDetailContent(contact, "Constructor")
                };
            }

            detailPage.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            detailPage.VerticalContentAlignment = VerticalAlignment.Stretch;
            await DemoNav.PushAsync(detailPage);

            AppendNavigationLog($"Navigated to {contact.Name} via {(MethodCombo.SelectedIndex == 1 ? "DataContext" : "Constructor")}");
        }

        private static Panel CreateDetailContent(Contact contact, string method)
        {
            var initials = string.Concat(contact.Name.Split(' ')[0][0], contact.Name.Split(' ')[1][0]).ToString();

            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new Border
                    {
                        Width = 80, Height = 80,
                        CornerRadius = new Avalonia.CornerRadius(40),
                        Background = new SolidColorBrush(contact.Color),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = initials,
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
                        FontSize = 24,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new Border
                    {
                        Background = new SolidColorBrush(Color.Parse("#2196F3")),
                        CornerRadius = new Avalonia.CornerRadius(4),
                        Padding = new Avalonia.Thickness(8, 4),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = $"Passed via {method}",
                            FontSize = 11,
                            Foreground = Brushes.White,
                        }
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

        private void AppendNavigationLog(string message)
        {
            var current = NavigationLog.Text;
            NavigationLog.Text = string.IsNullOrEmpty(current)
                ? message
                : $"{current}\n{message}";
        }
    }
}
