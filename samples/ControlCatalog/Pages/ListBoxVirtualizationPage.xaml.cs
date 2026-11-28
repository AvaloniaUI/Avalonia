using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public enum ListBoxVirtualizationScenario
    {
        VariableSizes,
        Wrapping,
        OwnContainers,
    }

    public partial class ListBoxVirtualizationPage : ContentPage
    {
        public static readonly StyledProperty<ListBoxVirtualizationScenario> ScenarioProperty =
            AvaloniaProperty.Register<ListBoxVirtualizationPage, ListBoxVirtualizationScenario>(nameof(Scenario));

        private readonly ObservableCollection<object> _items = new();
        private VirtualizingStackPanel? _panel;
        private bool _ready;
        private int _nextId;

        public ListBoxVirtualizationPage()
        {
            InitializeComponent();
            ItemsList.ItemsSource = _items;
            ItemsList.ItemsPanel = new FuncTemplate<Panel?>(() => _panel = new VirtualizingStackPanel
            {
                Orientation = HorizontalCheck.IsChecked == true ? Orientation.Horizontal : Orientation.Vertical,
                CacheLength = CacheSize.Value,
            });
            ItemsList.LayoutUpdated += OnLayoutUpdated;
            _ready = true;
            ConfigureScenario();
        }

        public ListBoxVirtualizationScenario Scenario
        {
            get => GetValue(ScenarioProperty);
            set => SetValue(ScenarioProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (_ready && change.Property == ScenarioProperty)
                ConfigureScenario();
        }

        private static ListBoxVirtualizationItem Model(object item) =>
            item is ListBoxItem container ? (ListBoxVirtualizationItem)container.Content! : (ListBoxVirtualizationItem)item;

        private void ConfigureScenario()
        {
            var wraps = Scenario == ListBoxVirtualizationScenario.Wrapping;
            DescriptionText.Text = wraps ?
                "Resize the cross axis while scrolled. The visible item should stay anchored, even with fixed headers. Horizontal mode rotates the wrapping text." :
                Scenario == ListBoxVirtualizationScenario.OwnContainers ?
                "The items are ListBoxItem controls. Resize, hide, or edit the prefix while scrolled, then return to the start to check reattachment." :
                "Select a row and resize or hide it while scrolled. Try uniform, variable, and viewport-sized rows in both orientations.";
            SizePreset.IsEnabled = SingleItemButton.IsEnabled = !wraps;
            HeaderCount.Value = wraps ? 2 : 0;
            ResetItems();
            UpdateViewport();
        }

        private double InitialSize(int id) => SizePreset.SelectedIndex switch
        {
            1 => 50,
            2 => 500,
            3 => id % 2 == 0 ? 60 : 40,
            _ => 40 + id * 37 % 80,
        };

        private object CreateItem()
        {
            var id = _nextId++;
            var model = new ListBoxVirtualizationItem(id, InitialSize(id), Scenario == ListBoxVirtualizationScenario.Wrapping)
            {
                Horizontal = HorizontalCheck.IsChecked == true,
            };
            return Scenario == ListBoxVirtualizationScenario.OwnContainers ?
                new ListBoxItem { Content = model, DataContext = model, ContentTemplate = ItemsList.ItemTemplate } : model;
        }

        private void ResetItems()
        {
            _items.Clear();
            _nextId = 0;
            for (var i = 0; i < 1000; ++i)
                _items.Add(CreateItem());
            UpdateHeaders();
            Jump(0);
        }

        private void UpdateHeaders()
        {
            for (var i = 0; i < _items.Count; ++i)
            {
                var model = Model(_items[i]);
                model.IsHeader = i < (int)(HeaderCount.Value ?? 0);
                if (model.IsHeader)
                    model.Size = 50;
            }
        }

        private void UpdateViewport()
        {
            if (!_ready)
                return;
            var horizontal = HorizontalCheck.IsChecked == true;
            ItemsList.Width = horizontal ? ViewportSize.Value : CrossSize.Value;
            ItemsList.Height = horizontal ? CrossSize.Value : ViewportSize.Value;
            ScrollViewer.SetHorizontalScrollBarVisibility(ItemsList, horizontal ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(ItemsList, horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
        }

        private void OnOrientationChanged(object? sender, RoutedEventArgs e)
        {
            if (!_ready)
                return;
            var horizontal = HorizontalCheck.IsChecked == true;
            foreach (var item in _items)
                Model(item).Horizontal = horizontal;
            if (_panel is not null)
                _panel.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
            UpdateViewport();
            ItemsList.UpdateLayout();
            Jump(Math.Max(0, ItemsList.SelectedIndex));
        }

        private void OnViewportChanged(object? sender, RangeBaseValueChangedEventArgs e) => UpdateViewport();

        private void OnCacheChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (_panel is not null)
                _panel.CacheLength = CacheSize.Value;
        }

        private void OnHeadersChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (_ready)
                UpdateHeaders();
        }

        private void OnPresetChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_ready)
                return;
            foreach (var item in _items)
            {
                var model = Model(item);
                model.Size = InitialSize(model.Id);
            }
            UpdateHeaders();
        }

        private void Jump(int index)
        {
            if (_items.Count == 0)
                return;
            ItemsList.SelectedIndex = Math.Clamp(index, 0, _items.Count - 1);
            ItemsList.ScrollIntoView(ItemsList.SelectedIndex);
            if (index == 0 && ItemsList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is { } scroll)
                scroll.Offset = default;
        }

        private void OnJump(object? sender, RoutedEventArgs e) => Jump((sender as Button)?.Tag switch
        {
            "middle" => _items.Count / 2,
            "end" => _items.Count - 1,
            _ => 0,
        });

        private void OnSingleItem(object? sender, RoutedEventArgs e)
        {
            HeaderCount.Value = 0;
            SizePreset.SelectedIndex = 2;
            foreach (var item in _items)
                Model(item).Size = 500;
            ViewportSize.Value = 100;
            CacheSize.Value = 0;
            ItemsList.UpdateLayout();
            Jump(_items.Count / 2);
        }

        private void OnResize(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string action })
                return;
            var factor = action.StartsWith("grow", StringComparison.Ordinal) ? 2d : 0.5d;
            var items = action.EndsWith("all", StringComparison.Ordinal) ? _items.ToArray() :
                ItemsList.SelectedItem is { } selected ? new[] { selected } : Array.Empty<object>();
            foreach (var item in items)
            {
                var model = Model(item);
                if (model.IsHeader)
                    continue;
                if (model.Wraps)
                    model.FontSize = Math.Clamp(model.FontSize * factor, 6, 80);
                else
                    model.Size = Math.Clamp(model.Size * factor, 1, 4000);
            }
        }

        private void OnToggleSelected(object? sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem is { } selected)
                Model(selected).IsVisible = !Model(selected).IsVisible;
        }

        private void OnHideAlternating(object? sender, RoutedEventArgs e)
        {
            for (var i = 0; i < _items.Count; ++i)
                Model(_items[i]).IsVisible = i % 2 == 0;
        }

        private void OnHideAll(object? sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                Model(item).IsVisible = false;
        }

        private void OnHideViewport(object? sender, RoutedEventArgs e)
        {
            if (ItemsList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is not { } scroll)
                return;
            var viewport = new Rect((Point)scroll.Offset, scroll.Viewport);
            foreach (var container in ItemsList.GetRealizedContainers().Where(c => c.Bounds.Intersects(viewport)).ToArray())
            {
                if (container.DataContext is ListBoxVirtualizationItem item)
                    item.IsVisible = false;
            }
        }

        private void OnShowAll(object? sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                Model(item).IsVisible = true;
        }

        private void OnReplaceFirst(object? sender, RoutedEventArgs e)
        {
            if (_items.Count > 0)
                _items[0] = CreateItem();
            UpdateHeaders();
        }

        private void OnInsertFirst(object? sender, RoutedEventArgs e)
        {
            _items.Insert(0, CreateItem());
            UpdateHeaders();
        }

        private void OnRemoveFirst(object? sender, RoutedEventArgs e)
        {
            if (_items.Count > 0)
                _items.RemoveAt(0);
            UpdateHeaders();
        }

        private void OnMoveFirst(object? sender, RoutedEventArgs e)
        {
            if (_items.Count > 1)
                _items.Move(0, Math.Min(20, _items.Count - 1));
            UpdateHeaders();
        }

        private void OnReset(object? sender, RoutedEventArgs e) => ResetItems();

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (_panel is null || ItemsList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is not { } scroll)
                return;
            var mismatches = ItemsList.GetRealizedContainers().Count(container =>
                container.DataContext is ListBoxVirtualizationItem item && container.IsVisible != item.IsVisible);
            var status = $"Items: {_items.Count:N0} | Realized: {_panel.FirstRealizedIndex}–{_panel.LastRealizedIndex} | " +
                $"Offset: {scroll.Offset.X:0.#}, {scroll.Offset.Y:0.#} | Viewport: {scroll.Viewport.Width:0.#} × {scroll.Viewport.Height:0.#} | " +
                $"Extent: {scroll.Extent.Width:0.#} × {scroll.Extent.Height:0.#}";
            if (mismatches > 0)
                status += $" | Visibility mismatches: {mismatches}";
            if (StatusText.Text != status)
            {
                StatusText.Text = status;
                if (mismatches > 0)
                    StatusText.Foreground = Brushes.OrangeRed;
                else
                    StatusText.ClearValue(TextBlock.ForegroundProperty);
            }
        }
    }
}
