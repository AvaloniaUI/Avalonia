using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaCarouselPage = Avalonia.Controls.CarouselPage;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageDataTemplatePage : UserControl
    {
        private static readonly (string Name, string Color, string Description)[] InitialData =
        {
            ("Tokyo",     "#1565C0", "The neon-lit capital of Japan, where ancient temples meet futuristic skylines."),
            ("Amsterdam", "#2E7D32", "A city of canals, bicycles, and world-class museums."),
            ("New York",  "#6A1B9A", "The city that never sleeps — a cultural and financial powerhouse."),
            ("Sydney",    "#B71C1C", "Iconic harbour, golden beaches and the world-famous Opera House."),
        };

        private static readonly (string Name, string Color, string Description)[] AddData =
        {
            ("Paris",     "#E65100", "The city of light, love, and the Eiffel Tower."),
            ("Barcelona", "#00695C", "Art, architecture, and vibrant street life on the Mediterranean coast."),
            ("Kyoto",     "#880E4F", "Japan's ancient capital, a living museum of traditional culture."),
        };

        private readonly ObservableCollection<Page> _pages = new();
        private int _addCounter;
        private bool _isCardStyle = true;
        private AvaCarouselPage? _carouselPage;

        public CarouselPageDataTemplatePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            foreach (var (name, color, desc) in InitialData)
                _pages.Add(CreatePage(name, color, desc, _isCardStyle));

            _addCounter = InitialData.Length;

            _carouselPage = new AvaCarouselPage { Pages = _pages };
            _carouselPage.SelectionChanged += OnSelectionChanged;

            CarouselHost.Children.Add(_carouselPage);
            UpdateStatus();
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            if (_carouselPage != null)
                _carouselPage.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void OnAddPage(object? sender, RoutedEventArgs e)
        {
            var idx = _addCounter % AddData.Length;
            var (name, color, desc) = AddData[idx];
            var suffix = _addCounter >= AddData.Length ? $" {_addCounter / AddData.Length + 1}" : "";
            _pages.Add(CreatePage(name + suffix, color, desc, _isCardStyle));
            _addCounter++;
            UpdateStatus();
        }

        private void OnSwitchTemplate(object? sender, RoutedEventArgs e)
        {
            _isCardStyle = !_isCardStyle;
            foreach (var page in _pages.OfType<ContentPage>())
            {
                var (color, desc) = ((string, string))page.Tag!;
                page.Content = _isCardStyle
                    ? BuildCardContent(page.Header?.ToString()!, color, desc)
                    : BuildBoldContent(page.Header?.ToString()!, color);
            }
            SwitchTemplateButton.Content = "Switch Template";
        }

        private void OnRemovePage(object? sender, RoutedEventArgs e)
        {
            if (_pages.Count > 0)
            {
                _pages.RemoveAt(_pages.Count - 1);
                UpdateStatus();
            }
        }

        private void OnPrevious(object? sender, RoutedEventArgs e)
        {
            if (_carouselPage == null)
                return;
            if (_carouselPage.SelectedIndex > 0)
                _carouselPage.SelectedIndex--;
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            if (_carouselPage == null)
                return;
            if (_carouselPage.SelectedIndex < _pages.Count - 1)
                _carouselPage.SelectedIndex++;
        }

        private void UpdateStatus()
        {
            var count = _pages.Count;
            var index = _carouselPage?.SelectedIndex ?? -1;
            StatusText.Text = count == 0
                ? "No pages"
                : $"Page {index + 1} of {count} (index {index})";
        }

        private static ContentPage CreatePage(string name, string color, string description, bool cardStyle) => new()
        {
            Header = name,
            Tag = (color, description),
            Content = cardStyle
                ? BuildCardContent(name, color, description)
                : BuildBoldContent(name, color)
        };

        private static Control BuildCardContent(string name, string color, string description) =>
            new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = name,
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse(color)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = description,
                        FontSize = 13,
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 280
                    }
                }
            };

        private static Control BuildBoldContent(string name, string color) =>
            new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.Parse(color)),
                Child = new TextBlock
                {
                    Text = name,
                    FontSize = 40,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
    }
}
