using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// A page that displays its child pages through a tab strip.
    /// </summary>
    [TemplatePart("PART_TabControl", typeof(TabControl))]
    public class TabbedPage : SelectingMultiPage
    {
        private TabControl? _tabControl;
        private readonly Dictionary<TabItem, Page> _containerPageMap = new();
        private readonly Dictionary<Page, TabItem> _pageContainerMap = new();
        private readonly SwipeGestureRecognizer _swipeRecognizer = new SwipeGestureRecognizer
        {
            IsEnabled = false
        };

        /// <summary>
        /// Defines the <see cref="BarBackground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BarBackgroundProperty =
            AvaloniaProperty.Register<TabbedPage, IBrush?>(nameof(BarBackground));

        /// <summary>
        /// Defines the <see cref="BarForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BarForegroundProperty =
            AvaloniaProperty.Register<TabbedPage, IBrush?>(nameof(BarForeground));

        /// <summary>
        /// Defines the <see cref="SelectedTabBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> SelectedTabBrushProperty =
            AvaloniaProperty.Register<TabbedPage, IBrush?>(nameof(SelectedTabBrush));

        /// <summary>
        /// Defines the <see cref="UnselectedTabBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> UnselectedTabBrushProperty =
            AvaloniaProperty.Register<TabbedPage, IBrush?>(nameof(UnselectedTabBrush));

        /// <summary>
        /// Defines the <see cref="TabPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<TabPlacement> TabPlacementProperty =
            AvaloniaProperty.Register<TabbedPage, TabPlacement>(nameof(TabPlacement), TabPlacement.Auto);

        /// <summary>
        /// Defines the <see cref="IsKeyboardNavigationEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsKeyboardNavigationEnabledProperty =
            AvaloniaProperty.Register<TabbedPage, bool>(nameof(IsKeyboardNavigationEnabled), true);

        /// <summary>
        /// Defines the <see cref="IsGestureEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsGestureEnabledProperty =
            AvaloniaProperty.Register<TabbedPage, bool>(nameof(IsGestureEnabled), defaultValue: false);

        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<TabbedPage, IPageTransition?>(nameof(PageTransition));

        /// <summary>
        /// Defines the <see cref="IndicatorTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> IndicatorTemplateProperty =
            AvaloniaProperty.Register<TabbedPage, IDataTemplate?>(nameof(IndicatorTemplate));

        /// <summary>
        /// Defines the <see cref="IsTabEnabledProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsTabEnabledProperty =
            AvaloniaProperty.RegisterAttached<TabbedPage, Page, bool>("IsTabEnabled", defaultValue: true);

        /// <summary>
        /// Gets the value of the <see cref="IsTabEnabledProperty"/> attached property for a page.
        /// </summary>
        /// <param name="page">The page to query.</param>
        /// <returns><see langword="true"/> if the tab is enabled; otherwise <see langword="false"/>.</returns>
        public static bool GetIsTabEnabled(Page page) => page.GetValue(IsTabEnabledProperty);

        /// <summary>
        /// Sets the value of the <see cref="IsTabEnabledProperty"/> attached property for a page.
        /// Disabled tabs are skipped during keyboard and swipe navigation.
        /// </summary>
        /// <param name="page">The page whose tab state to update.</param>
        /// <param name="value"><see langword="true"/> to enable the tab; <see langword="false"/> to disable it.</param>
        public static void SetIsTabEnabled(Page page, bool value) =>
            page.SetValue(IsTabEnabledProperty, value);

        /// <summary>
        /// Initializes a new instance of the <see cref="TabbedPage"/> class.
        /// </summary>
        public TabbedPage()
        {
            Pages = new AvaloniaList<object>();
            Focusable = true;
            GestureRecognizers.Add(_swipeRecognizer);
            AddHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
        }

        /// <summary>
        /// Gets or sets the background brush of the tab bar.
        /// </summary>
        public IBrush? BarBackground
        {
            get => GetValue(BarBackgroundProperty);
            set => SetValue(BarBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the tab bar.
        /// </summary>
        public IBrush? BarForeground
        {
            get => GetValue(BarForegroundProperty);
            set => SetValue(BarForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush for the selected tab.
        /// </summary>
        public IBrush? SelectedTabBrush
        {
            get => GetValue(SelectedTabBrushProperty);
            set => SetValue(SelectedTabBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush for unselected tabs.
        /// </summary>
        public IBrush? UnselectedTabBrush
        {
            get => GetValue(UnselectedTabBrushProperty);
            set => SetValue(UnselectedTabBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the tab bar placement.
        /// </summary>
        public TabPlacement TabPlacement
        {
            get => GetValue(TabPlacementProperty);
            set => SetValue(TabPlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets whether keyboard navigation can switch tabs.
        /// </summary>
        public bool IsKeyboardNavigationEnabled
        {
            get => GetValue(IsKeyboardNavigationEnabledProperty);
            set => SetValue(IsKeyboardNavigationEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether swipe gestures can be used to navigate between tabs.
        /// </summary>
        public bool IsGestureEnabled
        {
            get => GetValue(IsGestureEnabledProperty);
            set => SetValue(IsGestureEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the page transition to use when switching tabs.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to render the selection indicator on each tab.
        /// </summary>
        public IDataTemplate? IndicatorTemplate
        {
            get => GetValue(IndicatorTemplateProperty);
            set => SetValue(IndicatorTemplateProperty, value);
        }

        protected override void ApplySelectedIndex(int index)
        {
            if (_tabControl != null)
            {
                _tabControl.SelectedIndex = index;
            }
            else
            {
                StoreSelectedIndex(index);
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_tabControl != null)
            {
                _tabControl.SelectionChanged -= TabControl_SelectionChanged;
                _tabControl.ContainerPrepared -= OnContainerPrepared;
                _tabControl.ContainerClearing -= OnContainerClearing;

                foreach (var page in _containerPageMap.Values)
                    page.PropertyChanged -= OnPagePropertyChanged;
                _containerPageMap.Clear();
                _pageContainerMap.Clear();
            }

            _tabControl = e.NameScope.Find<TabControl>("PART_TabControl");

            if (_tabControl != null)
            {
                _tabControl.SelectionChanged += TabControl_SelectionChanged;
                _tabControl.ContainerPrepared += OnContainerPrepared;
                _tabControl.ContainerClearing += OnContainerClearing;

                if (SelectedIndex >= 0)
                    _tabControl.SelectedIndex = SelectedIndex;

                if (PageTransition != null)
                    _tabControl.PageTransition = PageTransition;

                ApplyTabPlacement();
                ApplyBarBackground();
                ApplyForegroundResources();
                ApplyIndicatorTemplate();
                UpdateActivePage();

                Dispatcher.UIThread.Post(SyncAllTabEnabledStates, DispatcherPriority.Loaded);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TabPlacementProperty)
                ApplyTabPlacement();
            else if (change.Property == BarBackgroundProperty)
                ApplyBarBackground();
            else if (change.Property == BarForegroundProperty
                     || change.Property == SelectedTabBrushProperty
                     || change.Property == UnselectedTabBrushProperty)
                ApplyForegroundResources();
            else if (change.Property == PageTransitionProperty && _tabControl != null)
                _tabControl.PageTransition = change.GetNewValue<IPageTransition?>();
            else if (change.Property == IndicatorTemplateProperty)
                ApplyIndicatorTemplate();
            else if (change.Property == IsGestureEnabledProperty)
                _swipeRecognizer.IsEnabled = change.GetNewValue<bool>();
        }

        private TabPlacement ResolveTabPlacement()
        {
            if (TabPlacement != TabPlacement.Auto)
                return TabPlacement;

            return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
                ? TabPlacement.Bottom
                : TabPlacement.Top;
        }

        private void ApplyTabPlacement()
        {
            if (_tabControl == null)
                return;

            _tabControl.TabStripPlacement = ResolveTabPlacement() switch
            {
                TabPlacement.Bottom => Dock.Bottom,
                TabPlacement.Left => Dock.Left,
                TabPlacement.Right => Dock.Right,
                _ => Dock.Top
            };
        }

        private void ApplyBarBackground()
        {
            if (_tabControl == null)
                return;

            _tabControl.Background = BarBackground;
        }

        private void ApplyForegroundResources()
        {
            if (_tabControl == null)
                return;

            _tabControl.Foreground = BarForeground;

            var selectedFg = SelectedTabBrush ?? BarForeground;
            if (selectedFg != null)
            {
                _tabControl.Resources["TabbedPageTabItemHeaderSelectedPipeFill"] = selectedFg;
                _tabControl.Resources["TabbedPageTabItemHeaderForegroundSelected"] = selectedFg;
                _tabControl.Resources["TabbedPageTabItemHeaderForegroundSelectedPointerOver"] = selectedFg;
                _tabControl.Resources["TabbedPageTabItemHeaderForegroundSelectedPressed"] = selectedFg;
            }
            else
            {
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderSelectedPipeFill");
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderForegroundSelected");
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderForegroundSelectedPointerOver");
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderForegroundSelectedPressed");
            }

            var unselectedFg = UnselectedTabBrush ?? BarForeground;
            if (unselectedFg != null)
            {
                _tabControl.Resources["TabbedPageTabItemHeaderForegroundUnselected"] = unselectedFg;
                _tabControl.Resources["TabbedPageTabItemHeaderForegroundUnselectedPointerOver"] = unselectedFg;
                _tabControl.Resources["TabbedPageTabItemHeaderForegroundUnselectedPressed"] = unselectedFg;
            }
            else
            {
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderForegroundUnselected");
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderForegroundUnselectedPointerOver");
                _tabControl.Resources.Remove("TabbedPageTabItemHeaderForegroundUnselectedPressed");
            }
        }

        private void ApplyIndicatorTemplate()
        {
            if (_tabControl == null)
                return;

            _tabControl.IndicatorTemplate = IndicatorTemplate;
        }

        private bool _ignoringDisabledSelection;

        private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_ignoringDisabledSelection)
                return;

            int newIndex = _tabControl!.SelectedIndex;
            var newPage = _tabControl.SelectedItem as Page ?? ResolvePageAtIndex(newIndex);

            if (newPage != null && !GetIsTabEnabled(newPage))
            {
                int target = FindNearestEnabledTab(newIndex);
                if (target < 0)
                    return;

                _ignoringDisabledSelection = true;
                try { _tabControl.SelectedIndex = target; }
                finally { _ignoringDisabledSelection = false; }

                if (target == SelectedIndex)
                    return;

                CommitSelection(target, ResolvePageAtIndex(target));
                UpdateContentSafeAreaPadding();
                return;
            }

            CommitSelection(newIndex, newPage);
            UpdateContentSafeAreaPadding();
        }

        private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            if (e.Container is not TabItem tabItem) return;
            if (Pages is not IList pages || e.Index >= pages.Count) return;

            var item = pages[e.Index];
            Page? page;

            if (item is Page directPage)
            {
                page = directPage;
            }
            else
            {
                // Data-template mode: build the page and use it directly as the tab's content.
                page = PageTemplate?.Build(item) as Page;
                if (page == null) return;
                tabItem.Content = page;
            }

            tabItem.IsEnabled = GetIsTabEnabled(page);
            tabItem.Header = page.Header;
            tabItem.Icon = CreateIconControl(page.Icon);

            _containerPageMap[tabItem] = page;
            _pageContainerMap[page] = tabItem;
            page.PropertyChanged += OnPagePropertyChanged;
        }

        private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
        {
            if (e.Container is TabItem tabItem && _containerPageMap.Remove(tabItem, out var page))
            {
                _pageContainerMap.Remove(page);
                page.PropertyChanged -= OnPagePropertyChanged;
            }
        }

        private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is not Page page || _tabControl == null)
                return;

            if (e.Property == Page.IconProperty)
            {
                if (_pageContainerMap.TryGetValue(page, out var tabItem))
                    tabItem.Icon = CreateIconControl(page.Icon);
            }
            else if (e.Property == Page.HeaderProperty)
            {
                if (_pageContainerMap.TryGetValue(page, out var tabItem))
                    tabItem.Header = page.Header;
            }
            else if (e.Property == IsTabEnabledProperty)
            {
                SyncTabEnabledState(page);
            }
        }

        /// <summary>
        /// Creates a visual control from a page icon value.
        /// </summary>
        internal static Control? CreateIconControl(object? icon)
        {
            Geometry? geometry = icon switch
            {
                Geometry g => g,
                PathIcon pi => pi.Data,
                DrawingImage { Drawing: GeometryDrawing { Geometry: { } gd } } => gd,
                string s when !string.IsNullOrEmpty(s) => Geometry.Parse(s),
                _ => null
            };

            if (geometry != null)
            {
                var path = new Path
                {
                    Data = geometry,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                path.Bind(
                    Path.FillProperty,
                    path.GetObservable(Documents.TextElement.ForegroundProperty));

                return path;
            }

            if (icon is IImage image)
            {
                return new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Source = image,
                };
            }

            return null;
        }

        private int FindNearestEnabledTab(int disabledIndex)
        {
            if (Pages is not IList pages) return -1;
            int count = pages.Count;
            for (int dist = 1; dist < count; dist++)
            {
                int p = disabledIndex - dist;
                if (p >= 0 && pages[p] is Page pp && GetIsTabEnabled(pp)) return p;
                int n = disabledIndex + dist;
                if (n < count && pages[n] is Page np && GetIsTabEnabled(np)) return n;
            }

            return -1;
        }

        protected internal int FindNextEnabledTab(int start, int direction)
        {
            if (Pages is not IList pages) return -1;
            int count = pages.Count;
            int i = start;
            while (i >= 0 && i < count)
            {
                if (pages[i] is not Page page || GetIsTabEnabled(page)) return i;
                i += direction;
            }

            return -1;
        }

        private Page? ResolvePageAtIndex(int index)
        {
            if (_tabControl?.ContainerFromIndex(index) is TabItem ti && _containerPageMap.TryGetValue(ti, out var p))
                return p;
            if (Pages is IList pages && (uint)index < (uint)pages.Count)
                return pages[index] as Page;
            return null;
        }

        private void SyncTabEnabledState(Page page)
        {
            if (_tabControl == null || Pages is not IList pages)
                return;

            for (int i = 0; i < pages.Count; i++)
            {
                if (!ReferenceEquals(pages[i], page))
                    continue;

                if (_tabControl.ContainerFromIndex(i) is TabItem tabItem)
                    tabItem.IsEnabled = GetIsTabEnabled(page);

                if (!GetIsTabEnabled(page) && i == _tabControl.SelectedIndex)
                {
                    int target = FindNearestEnabledTab(i);
                    if (target >= 0 && target != i)
                        _tabControl.SelectedIndex = target;
                }

                return;
            }
        }

        private void SyncAllTabEnabledStates()
        {
            if (_tabControl == null || Pages is not IList pages)
                return;

            for (int i = 0; i < pages.Count; i++)
            {
                if (_tabControl.ContainerFromIndex(i) is TabItem tabItem &&
                    _containerPageMap.TryGetValue(tabItem, out var page))
                {
                    tabItem.IsEnabled = GetIsTabEnabled(page);
                }
            }
        }

        protected override void UpdateActivePage()
        {
            if (_tabControl != null)
            {
                int index = _tabControl.SelectedIndex;
                CommitSelection(index, _tabControl.SelectedItem as Page ?? ResolvePageAtIndex(index));
                UpdateContentSafeAreaPadding();
            }
        }

        protected override void UpdateContentSafeAreaPadding()
        {
            base.UpdateContentSafeAreaPadding();

            if (_tabControl != null)
            {
                var sa = SafeAreaPadding;

                Thickness barMargin;
                Thickness contentSafeArea;

                switch (ResolveTabPlacement())
                {
                    case TabPlacement.Bottom:
                        barMargin = new Thickness(sa.Left, 0, sa.Right, sa.Bottom);
                        contentSafeArea = new Thickness(sa.Left, sa.Top, sa.Right, 0);
                        break;
                    case TabPlacement.Left:
                        barMargin = new Thickness(sa.Left, sa.Top, 0, sa.Bottom);
                        contentSafeArea = new Thickness(0, sa.Top, sa.Right, sa.Bottom);
                        break;
                    case TabPlacement.Right:
                        barMargin = new Thickness(0, sa.Top, sa.Right, sa.Bottom);
                        contentSafeArea = new Thickness(sa.Left, sa.Top, 0, sa.Bottom);
                        break;
                    default:
                        barMargin = new Thickness(sa.Left, sa.Top, sa.Right, 0);
                        contentSafeArea = new Thickness(sa.Left, 0, sa.Right, sa.Bottom);
                        break;
                }

                _tabControl.Margin = barMargin;

                if (CurrentPage != null)
                    CurrentPage.SafeAreaPadding = contentSafeArea;
            }
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            if (!IsGestureEnabled || _tabControl == null) return;

            var placement = ResolveTabPlacement();
            bool isHorizontal = placement == TabPlacement.Top || placement == TabPlacement.Bottom;
            bool isRtl = FlowDirection == FlowDirection.RightToLeft;

            int delta = (e.SwipeDirection, isHorizontal, isRtl) switch
            {
                (SwipeDirection.Left,  true,  false) => +1,
                (SwipeDirection.Right, true,  false) => -1,
                (SwipeDirection.Left,  true,  true)  => -1,
                (SwipeDirection.Right, true,  true)  => +1,
                (SwipeDirection.Up,    false, _)     => -1,
                (SwipeDirection.Down,  false, _)     => +1,
                _ => 0
            };

            if (delta == 0) return;

            int next = FindNextEnabledTab(_tabControl.SelectedIndex + delta, delta);
            if (next >= 0)
            {
                _tabControl.SelectedIndex = next;
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsKeyboardNavigationEnabled || _tabControl == null)
                return;

            var resolved = ResolveTabPlacement();
            bool isHorizontal = resolved == TabPlacement.Top || resolved == TabPlacement.Bottom;
            bool isRtl = FlowDirection == FlowDirection.RightToLeft;

            bool next = isHorizontal ? (isRtl ? e.Key == Key.Left : e.Key == Key.Right) : e.Key == Key.Down;
            bool prev = isHorizontal ? (isRtl ? e.Key == Key.Right : e.Key == Key.Left) : e.Key == Key.Up;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Tab)
            {
                next = !e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                prev = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            }

            if (next)
            {
                int target = FindNextEnabledTab(_tabControl.SelectedIndex + 1, 1);
                if (target >= 0)
                {
                    _tabControl.SelectedIndex = target;
                    FocusSelectedTabItem();
                    e.Handled = true;
                }
            }
            else if (prev)
            {
                int target = FindNextEnabledTab(_tabControl.SelectedIndex - 1, -1);
                if (target >= 0)
                {
                    _tabControl.SelectedIndex = target;
                    FocusSelectedTabItem();
                    e.Handled = true;
                }
            }
        }

        private void FocusSelectedTabItem()
        {
            if (_tabControl == null)
                return;

            (_tabControl.ContainerFromIndex(_tabControl.SelectedIndex) as InputElement)
                ?.Focus(NavigationMethod.Directional);
        }
    }
}
