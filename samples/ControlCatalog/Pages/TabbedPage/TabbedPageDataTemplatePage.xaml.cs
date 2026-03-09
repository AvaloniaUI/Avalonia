using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageDataTemplatePage : UserControl
    {
        private static readonly (string Name, string Color)[] CategoryData =
        {
            ("Electronics", "#1565C0"),
            ("Books",       "#2E7D32"),
            ("Clothing",    "#6A1B9A"),
        };

        private static readonly string[] AddNames   = { "Sports", "Music", "Garden", "Toys", "Food" };
        private static readonly string[] AddColors  = { "#E53935", "#F57C00", "#00796B", "#E91E63", "#3F51B5" };

        private readonly ObservableCollection<Page> _pages = new();
        private int _addCounter;
        private TabbedPage? _tabbedPage;

        public TabbedPageDataTemplatePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            foreach (var (name, color) in CategoryData)
                _pages.Add(CreatePage(name, color));

            _addCounter = CategoryData.Length;

            _tabbedPage = new TabbedPage
            {
                TabPlacement = TabPlacement.Top,
                Pages = _pages
            };

            TabbedPageHost.Children.Add(_tabbedPage);
            UpdateStatus();
        }

        private void OnAddCategory(object? sender, RoutedEventArgs e)
        {
            var idx = _addCounter % AddNames.Length;
            var name = AddNames[idx] + (_addCounter >= AddNames.Length ? $" {_addCounter / AddNames.Length + 1}" : "");
            _pages.Add(CreatePage(name, AddColors[idx]));
            _addCounter++;
            UpdateStatus();
        }

        private void OnRemoveCategory(object? sender, RoutedEventArgs e)
        {
            if (_pages.Count > 0)
            {
                _pages.RemoveAt(_pages.Count - 1);
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"{_pages.Count} categor{(_pages.Count == 1 ? "y" : "ies")}";
        }

        private static ContentPage CreatePage(string name, string color) => new()
        {
            Header = name,
            Content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = name,
                        FontSize = 24,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse(color)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"Tab for category: {name}",
                        FontSize = 13,
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 280
                    }
                }
            }
        };
    }
}
