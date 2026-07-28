using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaCarouselPage = Avalonia.Controls.CarouselPage;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageDataTemplatePage : UserControl
    {
        private sealed class CityViewModel
        {
            public string Name { get; }
            public string Color { get; }
            public string Description { get; }

            public CityViewModel(string name, string color, string description)
            {
                Name = name;
                Color = color;
                Description = description;
            }
        }

        private static readonly CityViewModel[] InitialData =
        {
            new("Tokyo", "#1565C0",
                "The neon-lit capital of Japan, where ancient temples meet futuristic skylines."),
            new("Amsterdam", "#2E7D32", "A city of canals, bicycles, and world-class museums."),
            new("New York", "#6A1B9A", "The city that never sleeps — a cultural and financial powerhouse."),
            new("Sydney", "#B71C1C", "Iconic harbour, golden beaches and the world-famous Opera House."),
        };

        private static readonly CityViewModel[] AddData =
        {
            new("Paris", "#E65100", "The city of light, love, and the Eiffel Tower."),
            new("Barcelona", "#00695C", "Art, architecture, and vibrant street life on the Mediterranean coast."),
            new("Kyoto", "#880E4F", "Japan's ancient capital, a living museum of traditional culture."),
        };

        private readonly ObservableCollection<CityViewModel> _items = new();
        private int _addCounter;
        private bool _useCardTemplate = true;
        private AvaCarouselPage? _carouselPage;

        public CarouselPageDataTemplatePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_carouselPage != null)
                return;

            foreach (var vm in InitialData)
                _items.Add(vm);
            _addCounter = InitialData.Length;
            _useCardTemplate = true;

            _carouselPage = new AvaCarouselPage { ItemsSource = _items, PageTemplate = CreatePageTemplate() };

            _carouselPage.SelectionChanged += OnSelectionChanged;
            CarouselHost.Children.Add(_carouselPage);
            UpdateStatus();
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e) => UpdateStatus();

        private void OnAddPage(object? sender, RoutedEventArgs e)
        {
            var idx = _addCounter % AddData.Length;
            var vm = AddData[idx];
            var suffix = _addCounter >= AddData.Length ? $" {_addCounter / AddData.Length + 1}" : "";
            _items.Add(new CityViewModel(vm.Name + suffix, vm.Color, vm.Description));
            _addCounter++;
            UpdateStatus();
        }

        private void OnRemovePage(object? sender, RoutedEventArgs e)
        {
            if (_items.Count > 0)
            {
                _items.RemoveAt(_items.Count - 1);
                UpdateStatus();
            }
        }

        private void OnSwitchTemplate(object? sender, RoutedEventArgs e)
        {
            if (_carouselPage == null)
                return;

            _useCardTemplate = !_useCardTemplate;
            _carouselPage.PageTemplate = CreatePageTemplate();
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
            if (_carouselPage.SelectedIndex < _items.Count - 1)
                _carouselPage.SelectedIndex++;
        }

        private void UpdateStatus()
        {
            var count = _items.Count;
            var index = _carouselPage?.SelectedIndex ?? -1;
            StatusText.Text = count == 0 ? "No pages" : $"Page {index + 1} of {count} (index {index})";
        }

        private IDataTemplate CreatePageTemplate()
        {
            return new FuncDataTemplate<CityViewModel>((vm, _) => CreatePage(vm, _useCardTemplate));
        }

        private static ContentPage CreatePage(CityViewModel? vm, bool useCardTemplate)
        {
            if (vm is null)
                return new ContentPage();

            return new ContentPage
            {
                Header = vm.Name, Content = useCardTemplate ? CreateCardContent(vm) : CreateFeatureContent(vm)
            };
        }

        private static Control CreateCardContent(CityViewModel vm)
        {
            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = vm.Name,
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse(vm.Color)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = vm.Description,
                        FontSize = 13,
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 280
                    }
                }
            };
        }

        private static Control CreateFeatureContent(CityViewModel vm)
        {
            var accent = Color.Parse(vm.Color);

            return new Border
            {
                Background = new SolidColorBrush(accent),
                Padding = new Thickness(32),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = vm.Name.ToUpperInvariant(),
                            FontSize = 34,
                            FontWeight = FontWeight.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = vm.Description,
                            FontSize = 15,
                            Foreground = Brushes.White,
                            Opacity = 0.88,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 320
                        }
                    }
                }
            };
        }
    }
}
