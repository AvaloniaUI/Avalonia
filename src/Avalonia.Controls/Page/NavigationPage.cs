using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls.Metadata;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A navigation page that supports simple stack-based navigation.
    /// </summary>
    [TemplatePart("PART_NavigationBar", typeof(Border))]
    [TemplatePart("PART_BackButton", typeof(Button))]
    [TemplatePart("PART_ContentHost", typeof(Panel))]
    [TemplatePart("PART_PagePresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_PageBackPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_TopCommandBar", typeof(ContentPresenter))]
    [TemplatePart("PART_BottomCommandBar", typeof(ContentPresenter))]
    [TemplatePart("PART_ModalBackPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_ModalPresenter", typeof(ContentPresenter))]
    public class NavigationPage : MultiPage, INavigation
    {
        private const double EdgeGestureWidth = 20;

        private Button? _backButton;
        private Panel? _contentHost;
        private ContentPresenter? _pagePresenter;
        private ContentPresenter? _pageBackPresenter;
        private CancellationTokenSource? _currentTransition;
        private CancellationTokenSource? _currentModalTransition;
        private Border? _navBar;
        private Border? _navBarShadow;
        private bool _isPop;
        private bool _hasHadFirstPage;
        private readonly Stack<Page> _modalStack = new();
        private IReadOnlyList<Page>? _cachedNavigationStack;
        private ContentPresenter? _modalBackPresenter;
        private ContentPresenter? _modalPresenter;
        private ContentPresenter? _topCommandBarPresenter;
        private IDisposable? _hasNavigationBarSub;
        private IDisposable? _isBackButtonEnabledSub;
        private IDisposable? _barLayoutBehaviorSub;
        private IDisposable? _barHeightSub;
        private bool _isNavigating;
        private bool _canGoBack;
        private DrawerPage? _drawerPage;
        private IPageTransition? _overrideTransition;
        private bool _hasOverrideTransition;
        private readonly HashSet<object> _pageSet = new(ReferenceEqualityComparer.Instance);

        private bool IsRtl => FlowDirection == FlowDirection.RightToLeft;

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            AvaloniaProperty.Register<NavigationPage, object?>(nameof(Content));

        internal static readonly StyledProperty<object?> ModalContentProperty =
            AvaloniaProperty.Register<NavigationPage, object?>(nameof(ModalContent));

        internal static readonly StyledProperty<bool> IsModalVisibleProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(IsModalVisible));

        /// <summary>
        /// Defines the <see cref="BarBackground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BarBackgroundProperty =
            AvaloniaProperty.Register<NavigationPage, IBrush?>(nameof(BarBackground));

        /// <summary>
        /// Defines the <see cref="BarForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BarForegroundProperty =
            AvaloniaProperty.Register<NavigationPage, IBrush?>(nameof(BarForeground));

        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<NavigationPage, IPageTransition?>(nameof(PageTransition));

        /// <summary>
        /// Defines the <see cref="ModalTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> ModalTransitionProperty =
            AvaloniaProperty.Register<NavigationPage, IPageTransition?>(nameof(ModalTransition));

        /// <summary>
        /// Defines the <see cref="BackButtonVisibleEffective"/> property.
        /// </summary>
        public static readonly StyledProperty<bool?> BackButtonVisibleEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, bool?>(nameof(BackButtonVisibleEffective), true);

        /// <summary>
        /// Defines the <see cref="NavBarEffectivelyVisible"/> property.
        /// </summary>
        internal static readonly StyledProperty<bool> NavBarEffectivelyVisibleProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(NavBarEffectivelyVisible), true);

        /// <summary>
        /// Defines the <see cref="PageBarLayoutBehaviorProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<BarLayoutBehavior?> PageBarLayoutBehaviorProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, BarLayoutBehavior?>("BarLayoutBehavior");

        /// <summary>
        /// Defines the <see cref="BarLayoutBehaviorEffective"/> property.
        /// </summary>
        public static readonly StyledProperty<BarLayoutBehavior> BarLayoutBehaviorEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, BarLayoutBehavior>(nameof(BarLayoutBehaviorEffective), BarLayoutBehavior.Inset);

        /// <summary>
        /// Defines the <see cref="HasShadow"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> HasShadowProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(HasShadow), false);

        /// <summary>
        /// Defines the <see cref="BarHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> BarHeightProperty =
            AvaloniaProperty.Register<NavigationPage, double>(nameof(BarHeight), 48.0);

        /// <summary>
        /// Defines the <see cref="PageBarHeightProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<double?> PageBarHeightProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, double?>("PageBarHeight");

        /// <summary>
        /// Defines the <see cref="BarHeightEffective"/> property.
        /// </summary>
        public static readonly StyledProperty<double> BarHeightEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, double>(nameof(BarHeightEffective), 48.0);

        /// <summary>
        /// Defines the <see cref="BackButtonContentProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<object?> BackButtonContentProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, object?>("BackButtonContent");

        /// <summary>
        /// Defines the <see cref="PageIsBackButtonVisibleEffectiveProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> PageIsBackButtonVisibleEffectiveProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, bool>("PageIsBackButtonVisible", true);

        /// <summary>
        /// Defines the <see cref="IsBackButtonVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsBackButtonVisibleEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(IsBackButtonVisible), true);

        /// <summary>
        /// Defines the <see cref="TopCommandBarProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<Control?> TopCommandBarProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, Control?>("TopCommandBar");

        /// <summary>
        /// Defines the <see cref="BottomCommandBarProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<Control?> BottomCommandBarProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, Control?>("BottomCommandBar");

        /// <summary>
        /// Defines the <see cref="HasNavigationBarProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> HasNavigationBarProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, bool>("HasNavigationBar", true);

        /// <summary>
        /// Defines the <see cref="IsGestureEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsGestureEnabledProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(IsGestureEnabled), true);

        /// <summary>
        /// Defines the <see cref="CanGoBack"/> property.
        /// </summary>
        public static readonly DirectProperty<NavigationPage, bool> CanGoBackProperty =
            AvaloniaProperty.RegisterDirect<NavigationPage, bool>(nameof(CanGoBack), o => o.CanGoBack);

        /// <summary>
        /// Defines the <see cref="PageIsBackButtonEnabledEffectiveProperty"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> PageIsBackButtonEnabledEffectiveProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, bool>("IsBackButtonEnabled", true);

        /// <summary>
        /// Defines the <see cref="BackButtonEnabledEffective"/> property.
        /// </summary>
        private static readonly StyledProperty<bool> BackButtonEnabledEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(BackButtonEnabledEffective), true);

        static NavigationPage()
        {
            PageNavigationSystemBackButtonPressedEvent.AddClassHandler<NavigationPage>((sender, eventArgs) =>
            {
                if (sender.StackDepth > 1)
                {
                    eventArgs.Handled = true;
                    _ = sender.PopAsync();
                }
            });

            ModalContentProperty.Changed.AddClassHandler<NavigationPage>((x, e) =>
            {
                if (x._modalPresenter != null)
                    x._modalPresenter.Content = e.NewValue;
            });

            IsModalVisibleProperty.Changed.AddClassHandler<NavigationPage>((x, e) =>
            {
                if (x._modalPresenter != null)
                    x._modalPresenter.IsVisible = e.GetNewValue<bool>();
            });

            IsBackButtonVisibleEffectiveProperty.Changed.AddClassHandler<NavigationPage>((x, _) =>
                x.UpdateBackButtonVisibleEffective());

            BarHeightProperty.Changed.AddClassHandler<NavigationPage>((x, _) =>
                x.UpdateBarHeightEffective());

            HasShadowProperty.Changed.AddClassHandler<NavigationPage>((x, _) =>
                x.ApplyHasShadow());

            BarHeightEffectiveProperty.Changed.AddClassHandler<NavigationPage>((x, _) =>
                x.ApplyHasShadow());

            NavBarEffectivelyVisibleProperty.Changed.AddClassHandler<NavigationPage>((x, _) =>
            {
                x.ApplyNavBarVisibility();
                x.ApplyHasShadow();
            });

            BackButtonEnabledEffectiveProperty.Changed.AddClassHandler<NavigationPage>((x, e) =>
                x.ApplyBackButtonEnabled(e.GetNewValue<bool>()));

            ContentProperty.Changed.AddClassHandler<NavigationPage>((x, e) =>
            {
                var newValue = e.NewValue;
                if (newValue == null || x.StackDepth > 0)
                    return;
                x.Push(newValue);
            });
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NavigationPage"/>.
        /// </summary>
        public NavigationPage()
        {
            Pages = new Stack<object>();
            GestureRecognizers.Add(new SwipeGestureRecognizer { EdgeSize = EdgeGestureWidth });
            AddHandler(Gestures.SwipeGestureEvent, OnSwipeGesture);
        }

        /// <summary>
        /// Gets or sets the background brush of the navigation bar.
        /// </summary>
        public IBrush? BarBackground
        {
            get => GetValue(BarBackgroundProperty);
            set => SetValue(BarBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the navigation bar.
        /// </summary>
        public IBrush? BarForeground
        {
            get => GetValue(BarForegroundProperty);
            set => SetValue(BarForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the transition used when pushing or popping pages.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets the transition used when presenting or dismissing modal pages.
        /// </summary>
        public IPageTransition? ModalTransition
        {
            get => GetValue(ModalTransitionProperty);
            set => SetValue(ModalTransitionProperty, value);
        }

        internal object? ModalContent
        {
            get => GetValue(ModalContentProperty);
            set => SetValue(ModalContentProperty, value);
        }

        internal bool IsModalVisible
        {
            get => GetValue(IsModalVisibleProperty);
            set => SetValue(IsModalVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the effective back-button visibility.
        /// </summary>
        public bool? BackButtonVisibleEffective
        {
            get => GetValue(BackButtonVisibleEffectiveProperty);
            set => SetValue(BackButtonVisibleEffectiveProperty, value);
        }

        /// <summary>
        /// Gets or sets the root page of the navigation stack.
        /// </summary>
        [Content]
        [DependsOn(nameof(PageTemplate))]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets the effective navigation bar visibility.
        /// </summary>
        internal bool NavBarEffectivelyVisible
        {
            get => GetValue(NavBarEffectivelyVisibleProperty);
        }

        /// <summary>
        /// Gets or sets the effective bar layout behavior.
        /// </summary>
        public BarLayoutBehavior BarLayoutBehaviorEffective
        {
            get => GetValue(BarLayoutBehaviorEffectiveProperty);
            set => SetValue(BarLayoutBehaviorEffectiveProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the navigation bar has a shadow.
        /// </summary>
        public bool HasShadow
        {
            get => GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the navigation bar.
        /// </summary>
        public double BarHeight
        {
            get => GetValue(BarHeightProperty);
            set => SetValue(BarHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the effective navigation bar height.
        /// </summary>
        public double BarHeightEffective
        {
            get => GetValue(BarHeightEffectiveProperty);
            set => SetValue(BarHeightEffectiveProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the back button is globally visible for this NavigationPage.
        /// </summary>
        public bool IsBackButtonVisible
        {
            get => GetValue(IsBackButtonVisibleEffectiveProperty);
            set => SetValue(IsBackButtonVisibleEffectiveProperty, value);
        }

        /// <summary>
        /// Gets or sets whether edge-swipe gestures can be used to navigate back.
        /// </summary>
        public bool IsGestureEnabled
        {
            get => GetValue(IsGestureEnabledProperty);
            set => SetValue(IsGestureEnabledProperty, value);
        }

        /// <summary>
        /// Gets whether the navigation stack has more than one entry.
        /// </summary>
        public bool CanGoBack => _canGoBack;

        private bool BackButtonEnabledEffective
        {
            get => GetValue(BackButtonEnabledEffectiveProperty);
            set => SetValue(BackButtonEnabledEffectiveProperty, value);
        }

        /// <summary>
        /// Gets the current navigation stack as a read-only list.
        /// </summary>
        public IReadOnlyList<Page> NavigationStack
        {
            get
            {
                if (_cachedNavigationStack != null)
                    return _cachedNavigationStack;

                if (Pages is Stack<object> stack)
                {
                    int total = stack.Count;
                    var result = new Page[total];
                    int writeAt = total;
                    foreach (var item in stack)
                        if (item is Page p)
                            result[--writeAt] = p;
                    _cachedNavigationStack = writeAt == 0 ? result : result[writeAt..];
                }
                else if (Pages is IList list)
                {
                    int total = list.Count;
                    var result = new Page[total];
                    int count = 0;
                    for (int i = 0; i < total; i++)
                        if (list[i] is Page p)
                            result[count++] = p;
                    _cachedNavigationStack = count == total ? result : result[..count];
                }
                else
                    _cachedNavigationStack = Array.Empty<Page>();

                return _cachedNavigationStack;
            }
        }

        /// <summary>
        /// Gets the current modal stack.
        /// </summary>
        public IReadOnlyCollection<Page> ModalStack => _modalStack;

        /// <summary>
        /// Gets the number of pages in the navigation stack.
        /// </summary>
        public int StackDepth
        {
            get
            {
                if (Pages is Stack<object> stack)
                    return stack.Count;
                if (Pages is IList list)
                    return list.Count;
                return 0;
            }
        }

        /// <summary>
        /// Gets the custom back-button content for the specified page.
        /// </summary>
        public static object? GetBackButtonContent(Page page) =>
            page.GetValue(BackButtonContentProperty);

        /// <summary>
        /// Sets custom content for the back button on the specified page.
        /// </summary>
        public static void SetBackButtonContent(Page page, object? content) =>
            page.SetValue(BackButtonContentProperty, content);

        /// <summary>
        /// Gets whether the back button is visible for the specified page.
        /// </summary>
        public static bool GetHasBackButton(Page page) =>
            page.GetValue(PageIsBackButtonVisibleEffectiveProperty);

        /// <summary>
        /// Sets whether the back button is visible for the specified page.
        /// </summary>
        public static void SetHasBackButton(Page page, bool value) =>
            page.SetValue(PageIsBackButtonVisibleEffectiveProperty, value);

        /// <summary>
        /// Gets the header for the specified page.
        /// </summary>
        public static object? GetHeader(Page page) =>
            page.GetValue(Page.HeaderProperty);

        /// <summary>
        /// Sets the header for the specified page.
        /// </summary>
        public static void SetHeader(Page page, object? header) =>
            page.SetValue(Page.HeaderProperty, header);

        /// <summary>
        /// Gets the top command bar assigned to the specified page.
        /// </summary>
        public static Control? GetTopCommandBar(Page page) =>
            page.GetValue(TopCommandBarProperty);

        /// <summary>
        /// Sets a top command bar for the specified page.
        /// </summary>
        public static void SetTopCommandBar(Page page, Control? commandBar) =>
            page.SetValue(TopCommandBarProperty, commandBar);

        /// <summary>
        /// Gets the bottom command bar assigned to the specified page.
        /// </summary>
        public static Control? GetBottomCommandBar(Page page) =>
            page.GetValue(BottomCommandBarProperty);

        /// <summary>
        /// Sets a bottom command bar for the specified page.
        /// </summary>
        public static void SetBottomCommandBar(Page page, Control? commandBar) =>
            page.SetValue(BottomCommandBarProperty, commandBar);

        /// <summary>
        /// Gets whether the navigation bar is visible for the specified page.
        /// </summary>
        public static bool GetHasNavigationBar(Page page) =>
            page.GetValue(HasNavigationBarProperty);

        /// <summary>
        /// Sets whether the navigation bar is visible for the specified page.
        /// </summary>
        public static void SetHasNavigationBar(Page page, bool value) =>
            page.SetValue(HasNavigationBarProperty, value);

        /// <summary>
        /// Gets the bar layout behavior for the specified page.
        /// </summary>
        public static BarLayoutBehavior? GetBarLayoutBehavior(Page page) =>
            page.GetValue(PageBarLayoutBehaviorProperty);

        /// <summary>
        /// Sets the bar layout behavior for the specified page.
        /// </summary>
        public static void SetBarLayoutBehavior(Page page, BarLayoutBehavior? value) =>
            page.SetValue(PageBarLayoutBehaviorProperty, value);

        /// <summary>
        /// Gets the per-page navigation bar height override for the specified page.
        /// </summary>
        public static double? GetBarHeight(Page page) =>
            page.GetValue(PageBarHeightProperty);

        /// <summary>
        /// Sets the per-page navigation bar height override.
        /// </summary>
        public static void SetBarHeight(Page page, double? value) =>
            page.SetValue(PageBarHeightProperty, value);

        /// <summary>
        /// Gets whether the back button is enabled for the specified page.
        /// </summary>
        public static bool GetIsBackButtonEnabled(Page page) =>
            page.GetValue(PageIsBackButtonEnabledEffectiveProperty);

        /// <summary>
        /// Sets whether the back button is enabled for the specified page.
        /// </summary>
        public static void SetIsBackButtonEnabled(Page page, bool value) =>
            page.SetValue(PageIsBackButtonEnabledEffectiveProperty, value);

        /// <summary>
        /// Occurs when a page is pushed onto the navigation stack.
        /// </summary>
        public event EventHandler<NavigationEventArgs>? Pushed;

        /// <summary>
        /// Occurs when a page is popped from the navigation stack.
        /// </summary>
        public event EventHandler<NavigationEventArgs>? Popped;

        /// <summary>
        /// Occurs when the stack is popped to root.
        /// </summary>
        public event EventHandler<NavigationEventArgs>? PoppedToRoot;

        /// <summary>
        /// Occurs when a page has been inserted into the navigation stack.
        /// </summary>
        public event EventHandler<PageInsertedEventArgs>? PageInserted;

        /// <summary>
        /// Occurs when a page has been removed from the navigation stack.
        /// </summary>
        public event EventHandler<PageRemovedEventArgs>? PageRemoved;

        /// <summary>
        /// Occurs when a modal page is pushed.
        /// </summary>
        public event EventHandler<ModalPushedEventArgs>? ModalPushed;

        /// <summary>
        /// Occurs when a modal page is popped.
        /// </summary>
        public event EventHandler<ModalPoppedEventArgs>? ModalPopped;

        private Button? BackButton
        {
            get { return _backButton; }
            set
            {
                if (_backButton != null)
                    _backButton.Click -= BackButton_Clicked;
                _backButton = value;
                if (_backButton != null)
                    _backButton.Click += BackButton_Clicked;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            BackButton = e.NameScope.Get<Button>("PART_BackButton");

            _contentHost = e.NameScope.Find<Panel>("PART_ContentHost");
            _pagePresenter = e.NameScope.Find<ContentPresenter>("PART_PagePresenter");
            _pageBackPresenter = e.NameScope.Find<ContentPresenter>("PART_PageBackPresenter");
            if (_navBar != null)
                _navBar.SizeChanged -= OnNavBarSizeChanged;
            _navBar = e.NameScope.Get<Border>("PART_NavigationBar");
            _navBar.SizeChanged += OnNavBarSizeChanged;
            _navBarShadow = e.NameScope.Find<Border>("PART_NavBarShadow");
            _topCommandBarPresenter = e.NameScope.Find<ContentPresenter>("PART_TopCommandBar");
            UpdateTopCommandBarMaxWidth();

            _modalBackPresenter = e.NameScope.Find<ContentPresenter>("PART_ModalBackPresenter");

            _modalPresenter = e.NameScope.Find<ContentPresenter>("PART_ModalPresenter");
            if (_modalPresenter != null)
            {
                _modalPresenter.Content = ModalContent;
                _modalPresenter.IsVisible = IsModalVisible;
            }

            foreach (var p in NavigationStack)
            {
                p.Navigation = this;
                p.SetInNavigationPage(true);
            }

            ApplyNavBarVisibility();
            ApplyBackButtonEnabled(BackButtonEnabledEffective);
            ApplyHasShadow();
            UpdateActivePage();
        }

        private void OnNavBarSizeChanged(object? sender, SizeChangedEventArgs e) =>
            UpdateTopCommandBarMaxWidth();

        private void UpdateTopCommandBarMaxWidth()
        {
            if (_topCommandBarPresenter == null || _navBar == null)
                return;
            var navBarWidth = _navBar.Bounds.Width;
            if (navBarWidth <= 0)
                return;
            _topCommandBarPresenter.MaxWidth = Math.Floor(navBarWidth * 0.5);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _currentTransition?.Cancel();
            _currentTransition?.Dispose();
            _currentTransition = null;

            _currentModalTransition?.Cancel();
            _currentModalTransition?.Dispose();
            _currentModalTransition = null;

            _hasNavigationBarSub?.Dispose();
            _hasNavigationBarSub = null;
            _isBackButtonEnabledSub?.Dispose();
            _isBackButtonEnabledSub = null;
            _barLayoutBehaviorSub?.Dispose();
            _barLayoutBehaviorSub = null;
            _barHeightSub?.Dispose();
            _barHeightSub = null;

            while (_modalStack.Count > 0)
            {
                var modal = _modalStack.Pop();
                modal.Navigation = null;
                modal.SetInNavigationPage(false);
            }

            foreach (var p in NavigationStack)
            {
                p.Navigation = null;
                p.SetInNavigationPage(false);
            }
            _cachedNavigationStack = null;
        }

        protected override void UpdateContentSafeAreaPadding()
        {
            if (_contentHost != null && _navBar != null)
            {
                _navBar.Padding = new Thickness(SafeAreaPadding.Left, SafeAreaPadding.Top, SafeAreaPadding.Right, 0);

                if (_pagePresenter != null)
                    _pagePresenter.Padding = Padding;
                if (_pageBackPresenter != null)
                    _pageBackPresenter.Padding = Padding;

                if (CurrentPage != null)
                {
                    var remainingSafeArea = Padding.GetRemainingSafeAreaPadding(SafeAreaPadding);
                    CurrentPage.SafeAreaPadding = new Thickness(remainingSafeArea.Left, 0, remainingSafeArea.Right, remainingSafeArea.Bottom);
                }

                foreach (var modal in _modalStack)
                    modal.SafeAreaPadding = SafeAreaPadding;
            }
        }

        private async void BackButton_Clicked(object? sender, RoutedEventArgs eventArgs)
        {
            if (StackDepth <= 1 && _drawerPage != null
                && _drawerPage.DrawerBehavior != DrawerBehavior.Locked
                && _drawerPage.DrawerBehavior != DrawerBehavior.Disabled)
            {
                _drawerPage.IsOpen = !_drawerPage.IsOpen;
                return;
            }
            await PopAsync();
        }

        /// <summary>
        /// Returns the page that would become active after a pop.
        /// </summary>
        private Page? PeekDestinationPage()
        {
            if (Pages is Stack<object> stack)
            {
                if (stack.Count < 2)
                    return null;
                using var enumerator = stack.GetEnumerator();
                enumerator.MoveNext();
                enumerator.MoveNext();
                return enumerator.Current as Page;
            }
            if (Pages is IList list)
                return list.Count >= 2 ? list[list.Count - 2] as Page : null;
            return null;
        }

        /// <summary>
        /// Performs the push after the Navigating event has been processed.
        /// </summary>
        private void ExecutePushCore(object page, Page? previousPage)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (_pageSet.Contains(page))
                throw new InvalidOperationException("The page is already in the navigation stack.");

            if (Pages is Stack<object> pages)
                pages.Push(page);
            else if (Pages is IList list)
                list.Add(page);

            _pageSet.Add(page);
            _cachedNavigationStack = null;

            if (page is ILogical logical && Pages is not INotifyCollectionChanged)
                LogicalChildren.Add(logical);

            var typedPage = page as Page;

            if (typedPage != null)
            {
                typedPage.Navigation = this;
                typedPage.SetInNavigationPage(true);
            }

            UpdateActivePage();

            if (typedPage != null)
            {
                previousPage?.SendDisappearing();
                previousPage?.SendNavigatedFrom(new NavigatedFromEventArgs(typedPage, NavigationType.Push));
                typedPage.SendNavigatedTo(new NavigatedToEventArgs(previousPage, NavigationType.Push));
                typedPage.SendAppearing();
                Pushed?.Invoke(this, new NavigationEventArgs(typedPage, NavigationType.Push));
            }
        }

        /// <summary>
        /// Performs the pop after the Navigating event has been processed.
        /// </summary>
        private object? ExecutePopCore()
        {
            object? old = null;

            if (Pages is Stack<object> pages)
            {
                old = pages.Pop();
            }
            else if (Pages is IList list)
            {
                if (list.Count > 0)
                {
                    old = list[list.Count - 1];
                    list.Remove(old);
                }
            }

            if (old != null)
                _pageSet.Remove(old);

            if (old is ILogical oldLogical && Pages is not INotifyCollectionChanged)
                LogicalChildren.Remove(oldLogical);

            _cachedNavigationStack = null;
            _isPop = true;
            UpdateActivePage();

            if (old is Page p)
            {
                p.Navigation = null;
                p.SetInNavigationPage(false);

                p.SendDisappearing();
                p.SendNavigatedFrom(new NavigatedFromEventArgs(CurrentPage, NavigationType.Pop));
                CurrentPage?.SendNavigatedTo(new NavigatedToEventArgs(p, NavigationType.Pop));
                CurrentPage?.SendAppearing();
                Popped?.Invoke(this, new NavigationEventArgs(p, NavigationType.Pop));
            }

            return old;
        }

        /// <summary>
        /// Pushes a page onto the navigation stack synchronously.
        /// </summary>
        public void Push(object page)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (_isNavigating)
                return;

            _isNavigating = true;
            try
            {
                var previousPage = CurrentPage;

                if (previousPage != null && page is Page destPage)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(destPage, NavigationType.Push);
                    previousPage.SendNavigatingFrom(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return;
                }

                ExecutePushCore(page, previousPage);
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pushes <paramref name="page"/> onto the navigation stack asynchronously using <see cref="PageTransition"/>.
        /// </summary>
        public async Task PushAsync(object page)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (_isNavigating)
                return;

            _isNavigating = true;
            try
            {
                var previousPage = CurrentPage;

                if (previousPage != null && page is Page destPage)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(destPage, NavigationType.Push);
                    await previousPage.SendNavigatingAsync(navigatingArgs);

                    if (navigatingArgs.Cancel)
                        return;
                }

                ExecutePushCore(page, previousPage);
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pushes <paramref name="page"/> onto the navigation stack asynchronously using <paramref name="transition"/>.
        /// </summary>
        public async Task PushAsync(object page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PushAsync(page); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Pops the top page from the navigation stack synchronously.
        /// </summary>
        public object? Pop()
        {
            if (StackDepth <= 1)
                return null;
            if (_isNavigating)
                return null;

            _isNavigating = true;
            try
            {
                var currentPage = CurrentPage;
                if (currentPage != null)
                {
                    var destination = PeekDestinationPage();
                    var navigatingArgs = new NavigatingFromEventArgs(destination, NavigationType.Pop);
                    currentPage.SendNavigatingFrom(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return null;
                }

                return ExecutePopCore();
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops the top page from the navigation stack asynchronously using <see cref="PageTransition"/>.
        /// </summary>
        public async Task<Page?> PopAsync()
        {
            if (StackDepth <= 1)
                return null;
            if (_isNavigating)
                return null;

            _isNavigating = true;
            try
            {
                var currentPage = CurrentPage;
                if (currentPage != null)
                {
                    var destination = PeekDestinationPage();
                    var navigatingArgs = new NavigatingFromEventArgs(destination, NavigationType.Pop);
                    await currentPage.SendNavigatingAsync(navigatingArgs);

                    if (navigatingArgs.Cancel)
                        return null;
                }

                return ExecutePopCore() as Page;
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops the top page from the navigation stack asynchronously using <paramref name="transition"/>.
        /// </summary>
        public async Task<Page?> PopAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { return await PopAsync(); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Pops all pages to the root page using <see cref="PageTransition"/>.
        /// </summary>
        public async Task PopToRootAsync()
        {
            if (StackDepth <= 1)
                return;
            if (_isNavigating)
                return;

            _isNavigating = true;
            try
            {
                var navigationStack = NavigationStack;
                var rootPage = navigationStack.Count > 0 ? navigationStack[0] : null;

                var currentPage = CurrentPage;
                if (currentPage != null)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(rootPage, NavigationType.PopToRoot);
                    await currentPage.SendNavigatingAsync(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return;
                }

                bool isIncc = Pages is INotifyCollectionChanged;

                if (Pages is Stack<object> stack)
                {
                    while (stack.Count > 1)
                    {
                        var popped = stack.Pop();
                        _pageSet.Remove(popped);
                        if (!isIncc && popped is ILogical poppedLogical)
                            LogicalChildren.Remove(poppedLogical);
                        if (popped is Page p)
                        {
                            p.SendDisappearing();
                            p.Navigation = null;
                            p.SetInNavigationPage(false);
                            p.SendNavigatedFrom(new NavigatedFromEventArgs(rootPage, NavigationType.PopToRoot));
                        }
                    }
                }
                else if (Pages is IList list)
                {
                    while (list.Count > 1)
                    {
                        var last = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                        if (last != null)
                            _pageSet.Remove(last);
                        if (!isIncc && last is ILogical lastLogical)
                            LogicalChildren.Remove(lastLogical);
                        if (last is Page p)
                        {
                            p.SendDisappearing();
                            p.Navigation = null;
                            p.SetInNavigationPage(false);
                            p.SendNavigatedFrom(new NavigatedFromEventArgs(rootPage, NavigationType.PopToRoot));
                        }
                    }
                }

                _cachedNavigationStack = null;
                _isPop = true;
                UpdateActivePage();

                if (CurrentPage != null)
                {
                    CurrentPage.SendNavigatedTo(new NavigatedToEventArgs(currentPage, NavigationType.PopToRoot));
                    CurrentPage.SendAppearing();
                    PoppedToRoot?.Invoke(this, new NavigationEventArgs(CurrentPage, NavigationType.PopToRoot));
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops all pages to the root page using <paramref name="transition"/>.
        /// </summary>
        public async Task PopToRootAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PopToRootAsync(); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Pops to a specific page in the stack using <see cref="PageTransition"/>.
        /// </summary>
        public async Task PopToPageAsync(Page page)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (!_pageSet.Contains(page))
                throw new ArgumentException("Page is not in the navigation stack.", nameof(page));

            if (_isNavigating)
                return;

            _isNavigating = true;
            try
            {
                var currentPage = CurrentPage;
                if (currentPage != null)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(page, NavigationType.Pop);
                    await currentPage.SendNavigatingAsync(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return;
                }

                bool isIncc = Pages is INotifyCollectionChanged;

                if (Pages is Stack<object> stack)
                {
                    while (stack.Count > 1 && stack.Peek() != page)
                    {
                        var popped = stack.Pop();
                        _pageSet.Remove(popped);
                        if (!isIncc && popped is ILogical poppedLogical)
                            LogicalChildren.Remove(poppedLogical);
                        if (popped is Page p)
                        {
                            p.SendDisappearing();
                            p.Navigation = null;
                            p.SetInNavigationPage(false);
                            p.SendNavigatedFrom(new NavigatedFromEventArgs(page, NavigationType.Pop));
                            Popped?.Invoke(this, new NavigationEventArgs(p, NavigationType.Pop));
                        }
                    }
                }
                else if (Pages is IList list)
                {
                    while (list.Count > 1 && list[list.Count - 1] != page)
                    {
                        var last = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                        if (last != null)
                            _pageSet.Remove(last);
                        if (!isIncc && last is ILogical lastLogical)
                            LogicalChildren.Remove(lastLogical);
                        if (last is Page p)
                        {
                            p.SendDisappearing();
                            p.Navigation = null;
                            p.SetInNavigationPage(false);
                            p.SendNavigatedFrom(new NavigatedFromEventArgs(page, NavigationType.Pop));
                            Popped?.Invoke(this, new NavigationEventArgs(p, NavigationType.Pop));
                        }
                    }
                }

                _cachedNavigationStack = null;
                _isPop = true;
                UpdateActivePage();

                if (CurrentPage != null)
                {
                    CurrentPage.SendNavigatedTo(new NavigatedToEventArgs(currentPage, NavigationType.Pop));
                    CurrentPage.SendAppearing();
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops all pages above <paramref name="page"/> using <paramref name="transition"/>.
        /// </summary>
        public async Task PopToPageAsync(Page page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PopToPageAsync(page); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Pushes a modal page using <see cref="ModalTransition"/>.
        /// </summary>
        public async Task PushModalAsync(object page)
        {
            if (_isNavigating)
                return;

            if (page is Page modalPage)
            {
                _isNavigating = true;
                try
                {
                    var previousModal = _modalStack.Count > 0 ? (Page?)_modalStack.Peek() : null;

                    var coveredPage = previousModal ?? CurrentPage;
                    coveredPage?.SendDisappearing();
                    coveredPage?.SendNavigatedFrom(new NavigatedFromEventArgs(modalPage, NavigationType.Push));

                    _modalStack.Push(modalPage);
                    modalPage.Navigation = this;
                    modalPage.SetInNavigationPage(true);
                    modalPage.SafeAreaPadding = SafeAreaPadding;

                    var effectiveModalTransition = _hasOverrideTransition ? _overrideTransition : ModalTransition;
                    _hasOverrideTransition = false;
                    _overrideTransition = null;

                    if (_modalPresenter != null && effectiveModalTransition != null)
                    {
                        if (previousModal != null && _modalBackPresenter != null)
                        {
                            _modalPresenter.IsVisible = false;

                            SetCurrentValue(ModalContentProperty, (object?)modalPage);

                            _modalBackPresenter.Content = previousModal;
                            _modalBackPresenter.IsVisible = true;
                        }
                        else
                        {
                            SetCurrentValue(ModalContentProperty, (object?)modalPage);
                        }
                        _currentModalTransition?.Cancel();
                        _currentModalTransition?.Dispose();
                        _currentModalTransition = new CancellationTokenSource();
                        await effectiveModalTransition.Start(null, _modalPresenter, forward: true, _currentModalTransition.Token);

                        if (_modalBackPresenter != null)
                        {
                            _modalBackPresenter.IsVisible = false;
                            _modalBackPresenter.Content = null;
                        }
                    }
                    else
                    {
                        SetCurrentValue(ModalContentProperty, (object?)modalPage);
                    }

                    SetCurrentValue(IsModalVisibleProperty, true);

                    modalPage.SendNavigatedTo(new NavigatedToEventArgs(coveredPage, NavigationType.Push));
                    modalPage.SendAppearing();
                    ModalPushed?.Invoke(this, new ModalPushedEventArgs(modalPage));
                }
                finally
                {
                    _isNavigating = false;
                }
            }
        }

        /// <summary>
        /// Pushes <paramref name="page"/> as a modal page using <paramref name="transition"/>.
        /// </summary>
        public async Task PushModalAsync(object page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PushModalAsync(page); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Pops the top modal page using <see cref="ModalTransition"/>.
        /// </summary>
        public async Task<Page?> PopModalAsync()
        {
            if (_modalStack.Count == 0)
                return null;
            if (_isNavigating)
                return null;

            _isNavigating = true;
            try
            {
                var modal = _modalStack.Pop();
                modal.SendDisappearing();

                var revealedPageForNav = _modalStack.Count > 0 ? (Page?)_modalStack.Peek() : CurrentPage;
                modal.SendNavigatedFrom(new NavigatedFromEventArgs(revealedPageForNav, NavigationType.Pop));

                modal.Navigation = null;
                modal.SetInNavigationPage(false);

                var effectiveModalTransition = _hasOverrideTransition ? _overrideTransition : ModalTransition;
                _hasOverrideTransition = false;
                _overrideTransition = null;

                if (_modalStack.Count > 0)
                {
                    var next = _modalStack.Peek();
                    if (_modalPresenter != null && effectiveModalTransition != null)
                    {
                        if (_modalBackPresenter != null)
                        {
                            _modalBackPresenter.Content = next;
                            _modalBackPresenter.IsVisible = true;
                        }

                        _currentModalTransition?.Cancel();
                        _currentModalTransition?.Dispose();
                        _currentModalTransition = new CancellationTokenSource();
                        try
                        {
                            await effectiveModalTransition.Start(_modalPresenter, null, forward: false, _currentModalTransition.Token);
                            SwapModalPresenters();
                            if (_modalBackPresenter != null)
                                _modalBackPresenter.Content = null;
                        }
                        finally
                        {
                            SetCurrentValue(ModalContentProperty, (object?)next);
                        }
                    }
                    else
                    {
                        SetCurrentValue(ModalContentProperty, (object?)next);
                    }
                }
                else
                {
                    if (_modalPresenter != null && effectiveModalTransition != null)
                    {
                        _currentModalTransition?.Cancel();
                        _currentModalTransition?.Dispose();
                        _currentModalTransition = new CancellationTokenSource();
                        try
                        {
                            await effectiveModalTransition.Start(_modalPresenter, null, forward: false, _currentModalTransition.Token);
                        }
                        finally
                        {
                            SetCurrentValue(IsModalVisibleProperty, false);
                            SetCurrentValue(ModalContentProperty, (object?)null);
                        }
                    }
                    else
                    {
                        SetCurrentValue(IsModalVisibleProperty, false);
                        SetCurrentValue(ModalContentProperty, (object?)null);
                    }
                }

                var revealedPage = _modalStack.Count > 0 ? (Page?)_modalStack.Peek() : CurrentPage;
                revealedPage?.SendNavigatedTo(new NavigatedToEventArgs(modal, NavigationType.Pop));
                revealedPage?.SendAppearing();

                ModalPopped?.Invoke(this, new ModalPoppedEventArgs(modal));
                return modal;
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops the top modal page using <paramref name="transition"/>.
        /// </summary>
        public async Task<Page?> PopModalAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { return await PopModalAsync(); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Pops all modal pages using <see cref="ModalTransition"/>.
        /// </summary>
        public async Task PopAllModalsAsync()
        {
            if (_modalStack.Count == 0)
                return;
            if (_isNavigating)
                return;

            _isNavigating = true;
            try
            {
                var effectiveModalTransition = _hasOverrideTransition ? _overrideTransition : ModalTransition;
                _hasOverrideTransition = false;
                _overrideTransition = null;

                if (_modalPresenter != null && effectiveModalTransition != null)
                {
                    _currentModalTransition?.Cancel();
                    _currentModalTransition?.Dispose();
                    _currentModalTransition = new CancellationTokenSource();
                    await effectiveModalTransition.Start(_modalPresenter, null, forward: false, _currentModalTransition.Token);
                }

                SetCurrentValue(ModalContentProperty, (object?)null);
                SetCurrentValue(IsModalVisibleProperty, false);

                while (_modalStack.Count > 0)
                {
                    var modal = _modalStack.Pop();
                    modal.SendDisappearing();
                    var nextPage = _modalStack.Count > 0 ? (Page?)_modalStack.Peek() : CurrentPage;
                    modal.SendNavigatedFrom(new NavigatedFromEventArgs(nextPage, NavigationType.Pop));
                    modal.Navigation = null;
                    modal.SetInNavigationPage(false);
                    ModalPopped?.Invoke(this, new ModalPoppedEventArgs(modal));
                }

                CurrentPage?.SendNavigatedTo(new NavigatedToEventArgs(null, NavigationType.Pop));
                CurrentPage?.SendAppearing();
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops all modal pages using <paramref name="transition"/>.
        /// </summary>
        public async Task PopAllModalsAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PopAllModalsAsync(); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        /// <summary>
        /// Removes a page from the navigation stack without animation.
        /// </summary>
        public void RemovePage(Page page)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (_isNavigating)
                return;

            if (Pages is Stack<object> stack)
            {
                if (stack.Count > 0 && ReferenceEquals(stack.Peek(), page))
                {
                    ExecutePopCore();
                    PageRemoved?.Invoke(this, new PageRemovedEventArgs(page));
                    return;
                }

                var arr = stack.ToArray();
                bool found = false;
                stack.Clear();
                for (int i = arr.Length - 1; i >= 0; i--)
                {
                    if (!found && ReferenceEquals(arr[i], page))
                    {
                        found = true;
                        continue;
                    }
                    stack.Push(arr[i]);
                }
                if (!found)
                    return;
            }
            else if (Pages is System.Collections.IList list)
            {
                int idx = -1;
                for (int i = 0; i < list.Count; i++)
                    if (ReferenceEquals(list[i], page)) { idx = i; break; }
                if (idx < 0)
                    return;

                if (idx == list.Count - 1)
                {
                    ExecutePopCore();
                    PageRemoved?.Invoke(this, new PageRemovedEventArgs(page));
                    return;
                }

                list.RemoveAt(idx);
            }
            else return;

            _pageSet.Remove(page);

            page.SendDisappearing();
            page.SendNavigatedFrom(new NavigatedFromEventArgs(null, NavigationType.Remove));

            page.Navigation = null;
            page.SetInNavigationPage(false);

            if (Pages is not System.Collections.Specialized.INotifyCollectionChanged)
                LogicalChildren.Remove(page);

            _cachedNavigationStack = null;
            UpdateBackButtonVisibleEffective();

            PageRemoved?.Invoke(this, new PageRemovedEventArgs(page));
        }

        /// <summary>
        /// Inserts a page into the stack before the specified page.
        /// </summary>
        public void InsertPage(Page page, Page before)
        {
            ArgumentNullException.ThrowIfNull(page);
            ArgumentNullException.ThrowIfNull(before);
            if (_isNavigating)
                return;

            if (_pageSet.Contains(page))
                throw new InvalidOperationException("The page is already in the navigation stack.");

            bool inserted = false;

            if (Pages is Stack<object> stack)
            {
                var arr = stack.ToArray();
                int beforeIdx = -1;
                for (int i = 0; i < arr.Length; i++)
                    if (ReferenceEquals(arr[i], before)) { beforeIdx = i; break; }
                if (beforeIdx < 0)
                    return;

                stack.Clear();
                for (int i = arr.Length - 1; i >= 0; i--)
                {
                    if (i == beforeIdx)
                        stack.Push(page);
                    stack.Push(arr[i]);
                }

                inserted = true;
            }
            else if (Pages is System.Collections.IList list)
            {
                int beforeIdx = -1;
                for (int i = 0; i < list.Count; i++)
                    if (ReferenceEquals(list[i], before)) { beforeIdx = i; break; }
                if (beforeIdx < 0)
                    return;

                list.Insert(beforeIdx, page);
                inserted = true;
            }

            if (!inserted)
                return;

            _pageSet.Add(page);
            page.Navigation = this;
            page.SetInNavigationPage(true);

            if (Pages is not System.Collections.Specialized.INotifyCollectionChanged)
                LogicalChildren.Add(page);

            _cachedNavigationStack = null;
            UpdateBackButtonVisibleEffective();

            PageInserted?.Invoke(this, new PageInsertedEventArgs(page, before));
        }

        /// <summary>
        /// Replaces the top page with <paramref name="page"/> using <see cref="PageTransition"/>.
        /// </summary>
        public async Task ReplaceAsync(object page)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (StackDepth == 0) { await PushAsync(page); return; }
            if (_isNavigating)
                return;

            _isNavigating = true;
            try
            {
                var previousPage = CurrentPage;

                if (previousPage != null && page is Page destPage)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(destPage, NavigationType.Replace);
                    await previousPage.SendNavigatingAsync(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return;
                }

                ExecuteReplaceCore(page, previousPage);
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Replaces the top page with <paramref name="page"/> using <paramref name="transition"/>.
        /// </summary>
        public async Task ReplaceAsync(object page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await ReplaceAsync(page); }
            finally { _hasOverrideTransition = false; _overrideTransition = null; }
        }

        protected override void UpdateActivePage()
        {
            bool isPop = _isPop;
            _isPop = false;

            bool hasOverride = _hasOverrideTransition;
            IPageTransition? overrideTransition = _overrideTransition;
            _hasOverrideTransition = false;
            _overrideTransition = null;

            object? page = null;
            if (Pages is Stack<object> pages)
            {
                pages.TryPeek(out page);
            }
            else if (Pages is IList list)
            {
                if (list.Count > 0)
                    page = list[list.Count - 1];
            }

            var newPage = page as Page;

            if (_contentHost != null && _pagePresenter != null && _pageBackPresenter != null)
            {
                IPageTransition? resolvedTransition;

                if (!_hasHadFirstPage)
                {
                    resolvedTransition = null;
                }
                else if (hasOverride)
                {
                    resolvedTransition = overrideTransition;
                }
                else
                {
                    resolvedTransition = PageTransition;
                }

                _currentTransition?.Cancel();
                _currentTransition?.Dispose();
                _currentTransition = null;

                _pageBackPresenter.IsVisible = false;
                _pageBackPresenter.Content = null;

                if (page is Control pCtrl && pCtrl.Parent is ContentPresenter strayCp)
                {
                    if (!ReferenceEquals(strayCp, _pagePresenter) && !ReferenceEquals(strayCp, _pageBackPresenter))
                        strayCp.Content = null;
                }

                if (resolvedTransition != null)
                {
                    var cancel = new CancellationTokenSource();
                    _currentTransition = cancel;

                    var oldPresenter = _pagePresenter;
                    var newPresenter = _pageBackPresenter;

                    newPresenter.Content = page;
                    newPresenter.IsVisible = true;

                    if (isPop)
                    {
                        oldPresenter.ZIndex = 1;
                        newPresenter.ZIndex = 0;
                    }
                    else
                    {
                        newPresenter.ZIndex = 1;
                        oldPresenter.ZIndex = 0;
                    }

                    _ = RunPageTransitionAsync(resolvedTransition, oldPresenter, newPresenter, !isPop, cancel.Token);

                    (_pagePresenter, _pageBackPresenter) = (newPresenter, oldPresenter);
                }
                else
                {
                    _pagePresenter.Content = page;
                    _pagePresenter.IsVisible = true;
                    _pagePresenter.ZIndex = 0;

                    _pageBackPresenter.Content = null;
                    _pageBackPresenter.IsVisible = false;
                    _pageBackPresenter.ZIndex = 0;
                }
            }

            SetCurrentValue(ContentProperty, page);

            SetCurrentValue(CurrentPageProperty, newPage);
            if (page != null)
                _hasHadFirstPage = true;

            _hasNavigationBarSub?.Dispose();
            _hasNavigationBarSub = null;

            _isBackButtonEnabledSub?.Dispose();
            _isBackButtonEnabledSub = null;

            _barLayoutBehaviorSub?.Dispose();
            _barLayoutBehaviorSub = null;

            _barHeightSub?.Dispose();
            _barHeightSub = null;

            if (newPage != null)
            {
                _hasNavigationBarSub = newPage.GetObservable(HasNavigationBarProperty)
                    .Subscribe(new AnonymousObserver<bool>(_ => UpdateNavBarEffectivelyVisible()));

                _isBackButtonEnabledSub = newPage.GetObservable(PageIsBackButtonEnabledEffectiveProperty)
                    .Subscribe(new AnonymousObserver<bool>(_ => UpdateBackButtonEnabledEffective()));

                _barLayoutBehaviorSub = newPage.GetObservable(PageBarLayoutBehaviorProperty)
                    .Subscribe(new AnonymousObserver<BarLayoutBehavior?>(_ => UpdateBarLayoutBehaviorEffective()));

                _barHeightSub = newPage.GetObservable(PageBarHeightProperty)
                    .Subscribe(new AnonymousObserver<double?>(_ => UpdateBarHeightEffective()));
            }

            UpdateNavBarEffectivelyVisible();
            UpdateBarLayoutBehaviorEffective();
            UpdateBarHeightEffective();

            _cachedNavigationStack = null;
            UpdateContentSafeAreaPadding();
            UpdateBackButtonVisibleEffective();
            UpdateBackButtonEnabledEffective();
            UpdateDrawerToggleIcon();
        }

        /// <summary>
        /// Runs the page transition and cleans up presenters on completion.
        /// </summary>
        private async Task RunPageTransitionAsync(
            IPageTransition transition,
            ContentPresenter from,
            ContentPresenter to,
            bool forward,
            CancellationToken ct)
        {
            try
            {
                await transition.Start(from, to, forward, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                    ?.Log(this, "Page transition threw an unhandled exception: {Exception}", e);
            }

            if (ct.IsCancellationRequested)
                return;

            from.IsVisible = false;
            from.Content = null;
            from.RenderTransform = null;
            from.Opacity = 1;
        }

        /// <summary>
        /// Swaps the top of the navigation stack with <paramref name="page"/>.
        /// </summary>
        private void ExecuteReplaceCore(object page, Page? replacedPage)
        {
            object? removed = null;

            if (Pages is Stack<object> pagesStack && pagesStack.Count > 0)
            {
                removed = pagesStack.Pop();
                _pageSet.Remove(removed);
            }
            else if (Pages is IList pagesList && pagesList.Count > 0)
            {
                removed = pagesList[pagesList.Count - 1];
                if (removed != null)
                    _pageSet.Remove(removed);
                pagesList.RemoveAt(pagesList.Count - 1);
            }

            if (Pages is Stack<object> pushStack)
                pushStack.Push(page);
            else if (Pages is IList pushList)
                pushList.Add(page);

            _pageSet.Add(page);
            _cachedNavigationStack = null;

            if (Pages is not INotifyCollectionChanged)
            {
                if (removed is ILogical removedLogical)
                    LogicalChildren.Remove(removedLogical);
                if (page is ILogical addedLogical)
                    LogicalChildren.Add(addedLogical);
            }

            var typedPage = page as Page;
            if (typedPage != null)
            {
                typedPage.Navigation = this;
                typedPage.SetInNavigationPage(true);
            }

            UpdateActivePage();

            if (replacedPage != null)
            {
                replacedPage.Navigation = null;
                replacedPage.SetInNavigationPage(false);
                replacedPage.SendDisappearing();
                replacedPage.SendNavigatedFrom(new NavigatedFromEventArgs(typedPage, NavigationType.Replace));
            }

            if (typedPage != null)
            {
                typedPage.SendNavigatedTo(new NavigatedToEventArgs(replacedPage, NavigationType.Replace));
                typedPage.SendAppearing();
            }
        }

        /// <summary>
        /// Swaps front and back modal presenters.
        /// </summary>
        private void SwapModalPresenters()
        {
            if (_modalPresenter == null || _modalBackPresenter == null)
                return;

            (_modalPresenter.ZIndex, _modalBackPresenter.ZIndex) =
                (_modalBackPresenter.ZIndex, _modalPresenter.ZIndex);

            (_modalPresenter, _modalBackPresenter) = (_modalBackPresenter, _modalPresenter);
        }

        internal void UpdateBackButtonVisibleEffective()
        {
            var depth = StackDepth;

            bool showDrawerToggle = _drawerPage != null
                && _drawerPage.DrawerBehavior != DrawerBehavior.Locked
                && _drawerPage.DrawerBehavior != DrawerBehavior.Disabled;

            SetCurrentValue(BackButtonVisibleEffectiveProperty, (bool?)(IsBackButtonVisible
                && (depth > 1 || showDrawerToggle)
                && (CurrentPage == null || GetHasBackButton(CurrentPage))));

            SetAndRaise(CanGoBackProperty, ref _canGoBack, depth > 1);

            UpdateBackButtonAccessibility();
        }

        private void UpdateBackButtonEnabledEffective()
        {
            SetCurrentValue(BackButtonEnabledEffectiveProperty, CurrentPage == null || GetIsBackButtonEnabled(CurrentPage));
        }

        private void UpdateBackButtonAccessibility()
        {
            if (_backButton == null)
                return;

            bool isDrawerToggle = _drawerPage != null
                && StackDepth <= 1
                && _drawerPage.DrawerBehavior != DrawerBehavior.Locked
                && _drawerPage.DrawerBehavior != DrawerBehavior.Disabled;

            var label = isDrawerToggle ? "Toggle navigation drawer" : "Go back";

            AutomationProperties.SetName(_backButton, label);
            ToolTip.SetTip(_backButton, label);
        }

        internal void SetDrawerPage(DrawerPage? drawerPage)
        {
            _drawerPage = drawerPage;
            UpdateBackButtonVisibleEffective();
            UpdateDrawerToggleIcon();
        }

        private static StreamGeometry? s_hamburgerIcon;
        private static StreamGeometry HamburgerIcon => s_hamburgerIcon ??=
            StreamGeometry.Parse("M3 17h18a1 1 0 0 1 .117 1.993L21 19H3a1 1 0 0 1-.117-1.993L3 17h18H3Zm0-6 18-.002a1 1 0 0 1 .117 1.993l-.117.007L3 13a1 1 0 0 1-.117-1.993L3 11l18-.002L3 11Zm0-6h18a1 1 0 0 1 .117 1.993L21 7H3a1 1 0 0 1-.117-1.993L3 5h18H3Z");

        private void UpdateDrawerToggleIcon()
        {
            if (_drawerPage == null || CurrentPage == null)
                return;

            bool showToggle = _drawerPage.DrawerBehavior != DrawerBehavior.Locked
                           && _drawerPage.DrawerBehavior != DrawerBehavior.Disabled;

            if (StackDepth <= 1 && showToggle)
            {
                if (GetBackButtonContent(CurrentPage) is not PathIcon)
                    SetBackButtonContent(CurrentPage, new PathIcon { Data = HamburgerIcon });
            }
            else
            {
                SetBackButtonContent(CurrentPage, null);
            }

            UpdateBackButtonAccessibility();
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            if (!IsGestureEnabled || StackDepth <= 1 || _isNavigating || _modalStack.Count > 0)
                return;
            bool shouldPop = IsRtl
                ? e.SwipeDirection == SwipeDirection.Left
                : e.SwipeDirection == SwipeDirection.Right;
            if (shouldPop)
            {
                e.Handled = true;
                _ = PopAsync();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Alt && StackDepth > 1)
            {
                _ = PopAsync();
                e.Handled = true;
            }
        }

        private void UpdateNavBarEffectivelyVisible()
        {
            SetCurrentValue(NavBarEffectivelyVisibleProperty, CurrentPage == null || GetHasNavigationBar(CurrentPage));
            UpdateNavBarSpacer();
        }

        private void UpdateBarLayoutBehaviorEffective()
        {
            SetCurrentValue(BarLayoutBehaviorEffectiveProperty, (CurrentPage != null
                ? GetBarLayoutBehavior(CurrentPage)
                : null) ?? BarLayoutBehavior.Inset);
            UpdateNavBarSpacer();
        }

        private void UpdateNavBarSpacer()
        {
            PseudoClasses.Set(":nav-bar-inset",
                NavBarEffectivelyVisible && BarLayoutBehaviorEffective == BarLayoutBehavior.Inset);
        }

        private void UpdateBarHeightEffective()
        {
            SetCurrentValue(BarHeightEffectiveProperty, (CurrentPage != null ? GetBarHeight(CurrentPage) : null) ?? BarHeight);
            PseudoClasses.Set(":nav-bar-compact", BarHeightEffective < 40);
        }

        private void ApplyNavBarVisibility()
        {
            if (_navBar != null)
                _navBar.IsVisible = NavBarEffectivelyVisible;
        }

        private void ApplyBackButtonEnabled(bool enabled)
        {
            if (_backButton != null)
                _backButton.IsEnabled = enabled;
        }

        private void ApplyHasShadow()
        {
            if (_navBarShadow == null)
                return;
            _navBarShadow.Margin = new Thickness(0, BarHeightEffective, 0, 0);
            _navBarShadow.IsVisible = HasShadow && NavBarEffectivelyVisible;
        }
    }
}
