using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
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
        private bool _ignoringDisabledSelection;
        private readonly Dictionary<TabItem, Page> _containerPageMap = new();
        private readonly Dictionary<Page, TabItem> _pageContainerMap = new();
        private readonly HashSet<Page> _templateCreatedPages = new(ReferenceEqualityComparer.Instance);
        private int _lastSwipeGestureId;
        private readonly SwipeGestureRecognizer _swipeRecognizer = new SwipeGestureRecognizer
        {
            IsEnabled = false
        };

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

        private static readonly bool s_isMobilePlatform = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        static TabbedPage()
        {
            FocusableProperty.OverrideDefaultValue<TabbedPage>(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabbedPage"/> class.
        /// </summary>
        public TabbedPage()
        {
            SetCurrentValue(PagesProperty, new AvaloniaList<Page>());
            GestureRecognizers.Add(_swipeRecognizer);
            UpdateSwipeRecognizerAxes();
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
        /// <remarks>
        /// Defaults to <see langword="false"/> because tab strips do not respond to swipe gestures
        /// on most platforms (iOS, desktop). Enable this only when the host platform and the
        /// content inside each tab page do not conflict with horizontal swipe input.
        /// </remarks>
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

        protected override Type StyleKeyOverride => typeof(TabbedPage);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AddHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            RemoveHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
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

            var requestedIndex = SelectedIndex;

            if (_tabControl != null)
            {
                _tabControl.SelectionChanged -= TabControl_SelectionChanged;
                _tabControl.ContainerPrepared -= OnContainerPrepared;
                _tabControl.ContainerClearing -= OnContainerClearing;
            }

            ClearContainerPages();

            _tabControl = e.NameScope.Find<TabControl>("PART_TabControl");

            if (_tabControl != null)
            {
                _tabControl.ContainerPrepared += OnContainerPrepared;
                _tabControl.ContainerClearing += OnContainerClearing;
                _tabControl.ItemsSource = (IEnumerable?)ItemsSource ?? Pages;

                if (requestedIndex >= 0)
                    _tabControl.SelectedIndex = requestedIndex;

                _tabControl.SelectionChanged += TabControl_SelectionChanged;

                if (PageTransition != null)
                    _tabControl.PageTransition = PageTransition;

                ApplyTabPlacement();
                ApplyIndicatorTemplate();
                UpdateActivePage();

                var capturedTabControl = _tabControl;
                Dispatcher.UIThread.Post(
                    () => SyncAllTabEnabledStates(capturedTabControl),
                    DispatcherPriority.Loaded);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TabPlacementProperty)
            {
                ApplyTabPlacement();
                UpdateSwipeRecognizerAxes();
            }
            else if (change.Property == PageTransitionProperty && _tabControl != null)
                _tabControl.PageTransition = change.GetNewValue<IPageTransition?>();
            else if (change.Property == IndicatorTemplateProperty)
                ApplyIndicatorTemplate();
            else if (change.Property == IsGestureEnabledProperty)
                _swipeRecognizer.IsEnabled = change.GetNewValue<bool>();
            else if (change.Property == ItemsSourceProperty && _tabControl != null)
                _tabControl.ItemsSource = change.GetNewValue<IEnumerable?>() ?? Pages;
            else if (change.Property == PagesProperty && ItemsSource == null && _tabControl != null)
                _tabControl.ItemsSource = change.GetNewValue<IEnumerable<Page>?>();
            else if (change.Property == PageTemplateProperty && ItemsSource != null && _tabControl != null)
                RebuildTemplateCreatedPages();
        }

        private TabPlacement ResolveTabPlacement()
        {
            if (TabPlacement != TabPlacement.Auto)
                return TabPlacement;

            return s_isMobilePlatform ? TabPlacement.Bottom : TabPlacement.Top;
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

        private void UpdateSwipeRecognizerAxes()
        {
            var placement = ResolveTabPlacement();
            var isHorizontal = placement == TabPlacement.Top || placement == TabPlacement.Bottom;
            _swipeRecognizer.CanHorizontallySwipe = isHorizontal;
            _swipeRecognizer.CanVerticallySwipe = !isHorizontal;
        }

        private void ApplyIndicatorTemplate()
        {
            if (_tabControl == null)
                return;

            _tabControl.IndicatorTemplate = IndicatorTemplate;
        }

        private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_ignoringDisabledSelection)
                return;

            int newIndex = _tabControl!.SelectedIndex;
            var newPage = ResolvePageAtIndex(newIndex);

            if (newPage != null && !GetIsTabEnabled(newPage))
            {
                int target = FindNearestEnabledTab(newIndex);
                if (target < 0)
                {
                    _ignoringDisabledSelection = true;
                    try { _tabControl.SelectedIndex = SelectedIndex; }
                    finally { _ignoringDisabledSelection = false; }
                    return;
                }

                _ignoringDisabledSelection = true;
                try { _tabControl.SelectedIndex = target; }
                finally { _ignoringDisabledSelection = false; }

                if (target == SelectedIndex)
                    return;

                CommitSelectionIfResolved(target, NavigationType.Replace);
                UpdateContentSafeAreaPadding();
                return;
            }

            CommitSelectionIfResolved(newIndex, NavigationType.Replace);
            UpdateContentSafeAreaPadding();
        }

        private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            if (e.Container is not TabItem tabItem)
                return;

            var page = PreparePageForContainer(tabItem, e.Index);
            if (page == null)
                return;

            tabItem.IsEnabled = GetIsTabEnabled(page);
            tabItem.Header = page.Header;
            tabItem.Icon = page.Icon;
            tabItem.IconTemplate = page.IconTemplate;

            if (e.Index == (_tabControl?.SelectedIndex ?? -1))
                UpdateActivePage();
        }

        private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
        {
            if (e.Container is TabItem tabItem)
                ClearContainerPage(tabItem);
        }

        private void RebuildTemplateCreatedPages()
        {
            if (_tabControl == null || ItemsSource == null)
                return;

            for (int i = 0; i < _tabControl.ItemCount; i++)
            {
                if (_tabControl.ContainerFromIndex(i) is not TabItem tabItem)
                    continue;

                var page = PreparePageForContainer(tabItem, i);
                if (page == null)
                    continue;

                tabItem.IsEnabled = GetIsTabEnabled(page);
                tabItem.Header = page.Header;
                tabItem.Icon = page.Icon;
                tabItem.IconTemplate = page.IconTemplate;
            }

            UpdateActivePage();
        }

        private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is not Page page || _tabControl == null)
                return;

            if (e.Property == Page.IconProperty)
            {
                if (_pageContainerMap.TryGetValue(page, out var tabItem))
                    tabItem.Icon = page.Icon;
            }
            else if (e.Property == Page.IconTemplateProperty)
            {
                if (_pageContainerMap.TryGetValue(page, out var tabItem))
                    tabItem.IconTemplate = page.IconTemplate;
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

        private int FindNearestEnabledTab(int disabledIndex)
        {
            int count = GetTabCount();
            for (int dist = 1; dist < count; dist++)
            {
                int p = disabledIndex - dist;
                if (p >= 0 && IsTabEnabledAtIndex(p))
                    return p;
                int n = disabledIndex + dist;
                if (n < count && IsTabEnabledAtIndex(n))
                    return n;
            }

            return -1;
        }

        private protected int FindNextEnabledTab(int start, int direction)
        {
            int count = GetTabCount();
            int i = start;
            while (i >= 0 && i < count)
            {
                if (IsTabEnabledAtIndex(i))
                    return i;
                i += direction;
            }

            return -1;
        }

        internal int GetTabCount()
        {
            if (_tabControl != null)
                return _tabControl.ItemCount;

            var source = (IEnumerable?)ItemsSource ?? Pages;
            if (source is ICollection collection)
                return collection.Count;

            if (source == null)
                return 0;

            int count = 0;
            foreach (var _ in source)
                count++;

            return count;
        }

        private bool IsTabEnabledAtIndex(int index)
        {
            if (_tabControl?.ContainerFromIndex(index) is TabItem ti && _containerPageMap.TryGetValue(ti, out var p))
                return GetIsTabEnabled(p);
            if (ResolvePageAtIndex(index) is Page page)
                return GetIsTabEnabled(page);
            return true;
        }

        private new Page? ResolvePageAtIndex(int index)
        {
            if (_tabControl?.ContainerFromIndex(index) is TabItem ti && _containerPageMap.TryGetValue(ti, out var p))
                return p;

            if (_tabControl != null)
            {
                var itemsView = _tabControl.ItemsView;
                if ((uint)index < (uint)itemsView.Count)
                    return itemsView[index] as Page;
            }

            if (_tabControl?.ContainerFromIndex(index) is TabItem { Content: Page page })
                return page;

            return base.ResolvePageAtIndex(index);
        }

        private void SyncTabEnabledState(Page page)
        {
            if (_tabControl == null || !_pageContainerMap.TryGetValue(page, out var tabItem))
                return;

            tabItem.IsEnabled = GetIsTabEnabled(page);

            if (!GetIsTabEnabled(page) && ReferenceEquals(tabItem, _tabControl.ContainerFromIndex(_tabControl.SelectedIndex)))
            {
                int i = _tabControl.SelectedIndex;
                int target = FindNearestEnabledTab(i);
                if (target >= 0 && target != i)
                    _tabControl.SelectedIndex = target;
            }
        }

        private void SyncAllTabEnabledStates(TabControl tabControl)
        {
            for (int i = 0; i < tabControl.ItemCount; i++)
            {
                if (tabControl.ContainerFromIndex(i) is TabItem tabItem &&
                    _containerPageMap.TryGetValue(tabItem, out var page))
                {
                    tabItem.IsEnabled = GetIsTabEnabled(page);
                }
            }
        }

        protected override void UpdateActivePage(NavigationType navigationType)
        {
            if (_tabControl != null)
            {
                int index = _tabControl.SelectedIndex;
                CommitSelectionIfResolved(index, navigationType);
                UpdateContentSafeAreaPadding();
            }
        }

        private Page? PreparePageForContainer(TabItem tabItem, int index)
        {
            ClearContainerPage(tabItem);

            Page? page;

            if (ItemsSource != null)
            {
                if (!TryGetItemAtIndex(index, out var item))
                    return null;

                page = PageTemplate?.Build(item) as Page;
                tabItem.Content = page;

                if (page == null)
                    return null;

                _templateCreatedPages.Add(page);
                LogicalChildren.Add(page);
            }
            else
            {
                if (!TryGetItemAtIndex(index, out var item))
                    return null;

                page = item as Page;
                if (page == null)
                    return null;

                tabItem.Content = page;
            }

            _containerPageMap[tabItem] = page;
            _pageContainerMap[page] = tabItem;
            page.PropertyChanged += OnPagePropertyChanged;

            return page;
        }

        private void ClearContainerPages()
        {
            foreach (var page in _containerPageMap.Values)
                page.PropertyChanged -= OnPagePropertyChanged;

            foreach (var page in _templateCreatedPages)
                LogicalChildren.Remove(page);

            _containerPageMap.Clear();
            _pageContainerMap.Clear();
            _templateCreatedPages.Clear();
        }

        private void ClearContainerPage(TabItem tabItem)
        {
            if (!_containerPageMap.Remove(tabItem, out var page))
                return;

            _pageContainerMap.Remove(page);
            page.PropertyChanged -= OnPagePropertyChanged;

            if (_templateCreatedPages.Remove(page))
                LogicalChildren.Remove(page);
        }

        private bool TryGetItemAtIndex(int index, out object? item)
        {
            if (_tabControl != null)
            {
                var itemsView = _tabControl.ItemsView;
                if ((uint)index < (uint)itemsView.Count)
                {
                    item = itemsView[index];
                    return true;
                }
            }

            var source = (IEnumerable?)ItemsSource ?? Pages;
            if (source != null)
            {
                int currentIndex = 0;
                foreach (var candidate in source)
                {
                    if (currentIndex == index)
                    {
                        item = candidate;
                        return true;
                    }

                    currentIndex++;
                }
            }

            item = null;
            return false;
        }

        private void CommitSelectionIfResolved(int index, NavigationType navigationType)
        {
            var page = ResolvePageAtIndex(index);

            if (page == null &&
                ItemsSource != null &&
                index >= 0 &&
                _tabControl?.ContainerFromIndex(index) == null)
            {
                StoreSelectedIndex(index);
                return;
            }

            CommitSelection(index, page, navigationType);
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
            if (!IsGestureEnabled || _tabControl == null || e.Id == _lastSwipeGestureId)
                return;

            var placement = ResolveTabPlacement();
            bool isHorizontal = placement == TabPlacement.Top || placement == TabPlacement.Bottom;
            bool isRtl = FlowDirection == Media.FlowDirection.RightToLeft;

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

            if (delta == 0)
                return;

            int next = FindNextEnabledTab(_tabControl.SelectedIndex + delta, delta);
            if (next >= 0)
            {
                _tabControl.SelectedIndex = next;
                e.Handled = true;
                _lastSwipeGestureId = e.Id;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsKeyboardNavigationEnabled || _tabControl == null)
                return;

            var resolved = ResolveTabPlacement();
            bool isHorizontal = resolved == TabPlacement.Top || resolved == TabPlacement.Bottom;
            bool isRtl = FlowDirection == Media.FlowDirection.RightToLeft;

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

        protected override AutomationPeer OnCreateAutomationPeer() => new TabbedPageAutomationPeer(this);

        private void FocusSelectedTabItem()
        {
            if (_tabControl == null)
                return;

            (_tabControl.ContainerFromIndex(_tabControl.SelectedIndex) as InputElement)
                ?.Focus(NavigationMethod.Directional);
        }
    }
}
