using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class CarouselCardItem
    {
        public string Number { get; set; } = "";
        public string Title { get; set; } = "";
        public IBrush Background { get; set; } = Brushes.Gray;
        public IBrush Accent { get; set; } = Brushes.White;
    }

    public partial class CarouselDataBindingPage : UserControl
    {
        private static readonly (string Title, string Color, string Accent)[] Palette =
        {
            ("Neon Pulse", "#3525CD", "#C3C0FF"), ("Ephemeral Blue", "#0891B2", "#BAF0FA"),
            ("Forest Forms", "#059669", "#A7F3D0"), ("Golden Hour", "#D97706", "#FDE68A"),
            ("Crimson Wave", "#BE185D", "#FBCFE8"), ("Stone Age", "#57534E", "#D6D3D1"),
        };

        private readonly ObservableCollection<CarouselCardItem> _items = new();
        private int _addCounter;

        public CarouselDataBindingPage()
        {
            InitializeComponent();
            DemoCarousel.ItemsSource = _items;
            DemoCarousel.SelectionChanged += OnSelectionChanged;

            for (var i = 0; i < 4; i++)
                AppendItem();

            PreviousButton.Click += (_, _) => DemoCarousel.Previous();
            NextButton.Click += (_, _) => DemoCarousel.Next();
            AddButton.Click += OnAddItem;
            RemoveButton.Click += OnRemoveCurrent;
            ShuffleButton.Click += OnShuffle;
            UpdateStatus();
        }

        private void AppendItem()
        {
            var (title, color, accent) = Palette[_addCounter % Palette.Length];
            _items.Add(new CarouselCardItem
            {
                Number = $"{_items.Count + 1:D2}",
                Title = title,
                Background = new SolidColorBrush(Color.Parse(color)),
                Accent = new SolidColorBrush(Color.Parse(accent)),
            });
            _addCounter++;
        }

        private void OnAddItem(object? sender, RoutedEventArgs e)
        {
            AppendItem();
            UpdateStatus();
        }

        private void OnRemoveCurrent(object? sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
                return;
            var idx = Math.Clamp(DemoCarousel.SelectedIndex, 0, _items.Count - 1);
            _items.RemoveAt(idx);
            UpdateStatus();
        }

        private void OnShuffle(object? sender, RoutedEventArgs e)
        {
            var rng = new Random();
            var shuffled = _items.OrderBy(_ => rng.Next()).ToList();
            _items.Clear();
            foreach (var item in shuffled)
                _items.Add(item);
            UpdateStatus();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Item: {DemoCarousel.SelectedIndex + 1} / {_items.Count}";
        }
    }
}
