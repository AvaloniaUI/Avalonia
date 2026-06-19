using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageDataTemplatePage : UserControl
    {
        private sealed class CategoryViewModel
        {
            public string Name { get; }
            public string Color { get; }

            public CategoryViewModel(string name, string color)
            {
                Name = name;
                Color = color;
            }
        }

        private static readonly CategoryViewModel[] InitialData =
        {
            new("Electronics", "#1565C0"), new("Books", "#2E7D32"), new("Clothing", "#6A1B9A"),
        };

        private static readonly CategoryViewModel[] AddData =
        {
            new("Sports", "#E53935"), new("Music", "#F57C00"), new("Garden", "#00796B"), new("Toys", "#E91E63"),
            new("Food", "#3F51B5")
        };

        private readonly ObservableCollection<CategoryViewModel> _items = new();
        private int _addCounter;
        private bool _useDetailTemplate = true;
        private TabbedPage? _tabbedPage;

        public TabbedPageDataTemplatePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_tabbedPage != null)
                return;

            foreach (var vm in InitialData)
                _items.Add(vm);
            _addCounter = InitialData.Length;
            _useDetailTemplate = true;

            _tabbedPage = new TabbedPage
            {
                TabPlacement = TabPlacement.Top, ItemsSource = _items, PageTemplate = CreatePageTemplate()
            };

            _tabbedPage.SelectionChanged += OnSelectionChanged;
            TabbedPageHost.Children.Add(_tabbedPage);
            UpdateStatus();
        }

        private void OnAddCategory(object? sender, RoutedEventArgs e)
        {
            var idx = _addCounter % AddData.Length;
            var vm = AddData[idx];
            var suffix = _addCounter >= AddData.Length ? $" {_addCounter / AddData.Length + 1}" : "";
            _items.Add(new CategoryViewModel(vm.Name + suffix, vm.Color));
            _addCounter++;
            UpdateStatus();
        }

        private void OnRemoveCategory(object? sender, RoutedEventArgs e)
        {
            if (_items.Count > 0)
            {
                _items.RemoveAt(_items.Count - 1);
                UpdateStatus();
            }
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e) => UpdateStatus();

        private void OnSwitchTemplate(object? sender, RoutedEventArgs e)
        {
            if (_tabbedPage == null)
                return;

            _useDetailTemplate = !_useDetailTemplate;
            _tabbedPage.PageTemplate = CreatePageTemplate();
            UpdateStatus();
        }

        private void OnPrevious(object? sender, RoutedEventArgs e)
        {
            if (_tabbedPage == null)
                return;

            if (_tabbedPage.SelectedIndex > 0)
                _tabbedPage.SelectedIndex--;
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            if (_tabbedPage == null)
                return;

            if (_tabbedPage.SelectedIndex < _items.Count - 1)
                _tabbedPage.SelectedIndex++;
        }

        private void UpdateStatus()
        {
            var count = _items.Count;
            var index = _tabbedPage?.SelectedIndex ?? -1;
            StatusText.Text = count == 0 ? "No tabs" : $"Tab {index + 1} of {count} (index {index})";
        }

        private IDataTemplate CreatePageTemplate()
        {
            return new FuncDataTemplate<CategoryViewModel>((vm, _) => CreatePage(vm, _useDetailTemplate));
        }

        private static ContentPage CreatePage(CategoryViewModel? vm, bool useDetailTemplate)
        {
            if (vm is null)
                return new ContentPage();

            return new ContentPage
            {
                Header = vm.Name, Content = useDetailTemplate ? CreateDetailContent(vm) : CreateShowcaseContent(vm)
            };
        }

        private static Control CreateDetailContent(CategoryViewModel vm)
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
                        FontSize = 24,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse(vm.Color)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"Tab for category: {vm.Name}",
                        FontSize = 13,
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 280
                    }
                }
            };
        }

        private static Control CreateShowcaseContent(CategoryViewModel vm)
        {
            var accent = Color.Parse(vm.Color);

            return new Border
            {
                Margin = new Thickness(24),
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(accent),
                Padding = new Thickness(28),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = vm.Name,
                            FontSize = 28,
                            FontWeight = FontWeight.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "Template switched at runtime",
                            FontSize = 14,
                            Foreground = Brushes.White,
                            Opacity = 0.9,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
        }
    }
}
