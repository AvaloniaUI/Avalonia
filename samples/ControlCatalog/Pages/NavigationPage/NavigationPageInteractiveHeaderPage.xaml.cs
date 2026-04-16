using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public record ContactItem(string Name, string Role);

    public partial class NavigationPageInteractiveHeaderPage : UserControl
    {
        private static readonly ContactItem[] AllContacts =
        [
            new("Alice Martin",   "Engineering Lead"),
            new("Bob Chen",       "Product Designer"),
            new("Carol White",    "Frontend Developer"),
            new("David Kim",      "Backend Developer"),
            new("Eva Müller",     "UX Researcher"),
            new("Frank Lopez",    "QA Engineer"),
            new("Grace Zhang",    "Data Scientist"),
            new("Henry Brown",    "DevOps Engineer"),
            new("Iris Patel",     "Security Analyst"),
            new("Jack Robinson",  "Mobile Developer"),
            new("Karen Lee",      "Project Manager"),
            new("Liam Thompson",  "Full-Stack Developer"),
            new("Maya Singh",     "Backend Developer"),
            new("Noah Garcia",    "iOS Developer"),
            new("Olivia Davis",   "Android Developer"),
            new("Paul Wilson",    "Systems Architect"),
            new("Quinn Adams",    "Technical Writer"),
            new("Rachel Turner",  "Data Engineer"),
            new("Samuel Hall",    "Cloud Engineer"),
            new("Tina Scott",     "UI Designer"),
        ];

        private readonly ObservableCollection<ContactItem> _filteredItems = new(AllContacts);
        private bool _initialized;
        private string _searchText = "";

        public NavigationPageInteractiveHeaderPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;
            var headerGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, Auto"),
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            var titleBlock = new TextBlock
            {
                Text = "Contacts",
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(titleBlock, 0);

            var searchBox = new TextBox
            {
                PlaceholderText = "Search...",
                Width = 140,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(searchBox, 1);

            searchBox.TextChanged += (_, _) =>
            {
                _searchText = searchBox.Text ?? "";
                ApplyFilter();
            };

            headerGrid.Children.Add(titleBlock);
            headerGrid.Children.Add(searchBox);

            var resultLabel = new TextBlock
            {
                Text = $"{AllContacts.Length} contacts",
                FontSize = 12,
                Opacity = 0.6,
                Margin = new Avalonia.Thickness(16, 8),
            };

            var listBox = new ListBox
            {
                ItemsSource = _filteredItems,
                ItemTemplate = new FuncDataTemplate<ContactItem>((item, _) =>
                {
                    if (item == null)
                        return new TextBlock();
                    var panel = new StackPanel { Margin = new Avalonia.Thickness(4, 2) };
                    panel.Children.Add(new TextBlock
                    {
                        Text = item.Name,
                        FontSize = 14,
                        FontWeight = FontWeight.SemiBold,
                    });
                    panel.Children.Add(new TextBlock
                    {
                        Text = item.Role,
                        FontSize = 12,
                        Opacity = 0.6,
                    });
                    return panel;
                }),
            };

            _filteredItems.CollectionChanged += (_, _) =>
            {
                resultLabel.Text = string.IsNullOrWhiteSpace(_searchText)
                    ? $"{AllContacts.Length} contacts"
                    : $"{_filteredItems.Count} of {AllContacts.Length} contacts";
            };

            var content = new DockPanel();
            DockPanel.SetDock(resultLabel, Dock.Top);
            content.Children.Add(resultLabel);
            content.Children.Add(listBox);

            await DemoNav.PushAsync(new ContentPage
            {
                Header = headerGrid,
                Content = content,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            }, null);
        }

        private void ApplyFilter()
        {
            _filteredItems.Clear();
            foreach (var c in AllContacts)
            {
                if (string.IsNullOrEmpty(_searchText) ||
                    c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Role.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                {
                    _filteredItems.Add(c);
                }
            }
        }
    }
}
