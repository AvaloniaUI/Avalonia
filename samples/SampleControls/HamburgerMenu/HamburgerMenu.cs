using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ControlSamples
{
    public class HamburgerMenu : TabControl
    {
        private SplitView? _splitView;
        private TextBox? _searchBox;
        private bool _initialized;

        public HamburgerMenu()
        {
            Loaded += OnControlLoaded;
        }

        public static readonly StyledProperty<IBrush?> PaneBackgroundProperty =
            SplitView.PaneBackgroundProperty.AddOwner<HamburgerMenu>();

        public IBrush? PaneBackground
        {
            get => GetValue(PaneBackgroundProperty);
            set => SetValue(PaneBackgroundProperty, value);
        }

        public static readonly StyledProperty<IBrush?> ContentBackgroundProperty =
            AvaloniaProperty.Register<HamburgerMenu, IBrush?>(nameof(ContentBackground));

        public IBrush? ContentBackground
        {
            get => GetValue(ContentBackgroundProperty);
            set => SetValue(ContentBackgroundProperty, value);
        }

        public static readonly StyledProperty<int> ExpandedModeThresholdWidthProperty =
            AvaloniaProperty.Register<HamburgerMenu, int>(nameof(ExpandedModeThresholdWidth), 1008);

        public int ExpandedModeThresholdWidth
        {
            get => GetValue(ExpandedModeThresholdWidthProperty);
            set => SetValue(ExpandedModeThresholdWidthProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _splitView = e.NameScope.Find<SplitView>("PART_NavigationPane");
            _searchBox = e.NameScope.Find<TextBox>("PART_SearchBox");

            if (_searchBox is not null)
            {
                _searchBox.TextChanged += OnSearchTextChanged;
            }
        }

        private void OnControlLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_initialized)
            {
                _initialized = true;
                SortItems();
            }
        }

        private void SortItems()
        {
            var items = Items.OfType<TabItem>().ToList();
            var sorted = items.OrderBy(t => t.Header?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ToList();

            // Only reorder if needed
            bool needsSort = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (!ReferenceEquals(items[i], sorted[i]))
                {
                    needsSort = true;
                    break;
                }
            }

            if (needsSort)
            {
                Items.Clear();
                foreach (var item in sorted)
                {
                    Items.Add(item);
                }
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchText = _searchBox?.Text;
            var hasFilter = !string.IsNullOrWhiteSpace(searchText);

            foreach (var item in Items.OfType<TabItem>())
            {
                if (hasFilter)
                {
                    var header = item.Header?.ToString() ?? "";
                    item.IsVisible = header.Contains(searchText!, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    item.IsVisible = true;
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty && _splitView is not null)
            {
                var (oldBounds, newBounds) = change.GetOldAndNewValue<Rect>();
                EnsureSplitViewMode(oldBounds, newBounds);
            }

            if (change.Property == SelectedItemProperty)
            {
                if (_splitView is not null && _splitView.DisplayMode == SplitViewDisplayMode.Overlay)
                {
                    _splitView.SetCurrentValue(SplitView.IsPaneOpenProperty, false);
                }
            }
        }

        private void EnsureSplitViewMode(Rect oldBounds, Rect newBounds)
        {
            if (_splitView is not null)
            {
                var threshold = ExpandedModeThresholdWidth;

                if (newBounds.Width >= threshold)
                {
                    _splitView.DisplayMode = SplitViewDisplayMode.Inline;
                    _splitView.IsPaneOpen = true;
                }
                else if (newBounds.Width < threshold)
                {
                    _splitView.DisplayMode = SplitViewDisplayMode.Overlay;
                    _splitView.IsPaneOpen = false;
                }
            }
        }
    }
}
