using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageDataTemplatePage : UserControl
    {
        private readonly ObservableCollection<CategoryItem> _categories = new();
        private int _counter;

        public TabbedPageDataTemplatePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _categories.Add(new CategoryItem("Electronics", "#1565C0"));
            _categories.Add(new CategoryItem("Books", "#2E7D32"));
            _categories.Add(new CategoryItem("Clothing", "#6A1B9A"));
            _counter = 3;

            var tabbedPage = new TabbedPage
            {
                TabPlacement = TabPlacement.Top,
                PageTemplate = new FuncDataTemplate<CategoryItem>((item, _) =>
                {
                    if (item == null) return null;

                    return new ContentPage
                    {
                        Header = item.Name,
                        Content = new StackPanel
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Spacing = 8,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = item.Name,
                                    FontSize = 24,
                                    FontWeight = FontWeight.SemiBold,
                                    Foreground = new SolidColorBrush(Color.Parse(item.Color)),
                                    HorizontalAlignment = HorizontalAlignment.Center
                                },
                                new TextBlock
                                {
                                    Text = $"Data-bound tab generated from a {item.GetType().Name} object.",
                                    FontSize = 13,
                                    Opacity = 0.7,
                                    TextWrapping = TextWrapping.Wrap,
                                    TextAlignment = TextAlignment.Center,
                                    MaxWidth = 280
                                }
                            }
                        }
                    };
                }, true),
                Pages = _categories
            };

            TabbedPageHost.Children.Add(tabbedPage);
            UpdateStatus();
        }

        private void OnAddCategory(object? sender, RoutedEventArgs e)
        {
            var colors = new[] { "#E53935", "#F57C00", "#00796B", "#E91E63", "#3F51B5" };
            var names = new[] { "Sports", "Music", "Garden", "Toys", "Food" };
            var idx = _counter % names.Length;
            _categories.Add(new CategoryItem(names[idx] + (_counter >= names.Length ? $" {_counter / names.Length + 1}" : ""), colors[idx]));
            _counter++;
            UpdateStatus();
        }

        private void OnRemoveCategory(object? sender, RoutedEventArgs e)
        {
            if (_categories.Count > 0)
            {
                _categories.RemoveAt(_categories.Count - 1);
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"{_categories.Count} categor{(_categories.Count == 1 ? "y" : "ies")}";
        }

        public record CategoryItem(string Name, string Color);
    }
}
