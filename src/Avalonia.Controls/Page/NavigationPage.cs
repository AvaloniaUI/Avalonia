using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls.Metadata;
using Avalonia.Logging;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
        private const double GestureThreshold = 30;
        private const double EdgeGestureWidth = 20;
        private const double VerticalGestureCancelThreshold = 8;

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
        private bool _isDragging;
        private Point _dragStart;
        private bool _isNavigating;
        private bool _canGoBack;
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
        /// Defines the computed back-button visibility.
        /// </summary>
        public static readonly StyledProperty<bool?> BackButtonVisibleEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, bool?>(nameof(BackButtonVisibleEffective), true);

        /// <summary>
        /// Defines the computed effective nav bar visibility based on the current page's
        /// <see cref="HasNavigationBarProperty"/>. Drives the template binding.
        /// </summary>
        private static readonly StyledProperty<bool> NavBarVisibleEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(NavBarVisibleEffective), true);

        /// <summary>
        /// Defines the per-page attached property for <see cref="BarLayoutBehavior"/>.
        /// Null means inherit the NavigationPage default (<see cref="BarLayoutBehavior.Inset"/>).
        /// </summary>
        public static readonly AttachedProperty<BarLayoutBehavior?> PageBarLayoutBehaviorProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, BarLayoutBehavior?>("BarLayoutBehavior");

        /// <summary>
        /// Defines the computed effective <see cref="BarLayoutBehavior"/> based on the current page's
        /// <see cref="PageBarLayoutBehaviorProperty"/>. Drives the template.
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
        /// Defines the per-page attached property that overrides the navigation bar height.
        /// Null means inherit the global <see cref="BarHeight"/> value.
        /// </summary>
        public static readonly AttachedProperty<double?> PageBarHeightProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, double?>("PageBarHeight");

        /// <summary>
        /// Defines the computed effective bar height for the current page.
        /// Set to the page's <see cref="PageBarHeightProperty"/> when non-null, otherwise falls
        /// back to the global <see cref="BarHeight"/>. Drives the template binding.
        /// </summary>
        public static readonly StyledProperty<double> BarHeightEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, double>(nameof(BarHeightEffective), 48.0);

        /// <summary>
        /// Defines the attached property that sets custom content for the back button on a specific <see cref="Page"/>.
        /// Accepts any object (string, icon, Control).
        /// </summary>
        public static readonly AttachedProperty<object?> BackButtonContentProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, object?>("BackButtonContent");

        /// <summary>
        /// Defines the attached property controlling back button visibility for a specific <see cref="Page"/>.
        /// </summary>
        public static readonly AttachedProperty<bool> PageIsBackButtonVisibleEffectiveProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, bool>("PageIsBackButtonVisible", true);

        /// <summary>
        /// Defines the <see cref="IsBackButtonVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsBackButtonVisibleEffectiveProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(IsBackButtonVisible), true);

        /// <summary>
        /// Defines the attached property that sets a command bar displayed at the top of the navigation bar for a specific <see cref="Page"/>.
        /// </summary>
        public static readonly AttachedProperty<Control?> TopCommandBarProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, Control?>("TopCommandBar");

        /// <summary>
        /// Defines the attached property that sets a command bar displayed below the page content for a specific <see cref="Page"/>.
        /// </summary>
        public static readonly AttachedProperty<Control?> BottomCommandBarProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, Control?>("BottomCommandBar");

        /// <summary>
        /// Defines the attached property that controls whether the navigation bar is visible for a specific <see cref="Page"/>.
        /// </summary>
        public static readonly AttachedProperty<bool> HasNavigationBarProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, bool>("HasNavigationBar", true);

        /// <summary>
        /// Defines the <see cref="IsGestureEnabled"/> property.
        /// When true, an edge-swipe gesture can be used to navigate back.
        /// </summary>
        public static readonly StyledProperty<bool> IsGestureEnabledProperty =
            AvaloniaProperty.Register<NavigationPage, bool>(nameof(IsGestureEnabled), true);

        /// <summary>
        /// Defines the <see cref="CanGoBack"/> property.
        /// </summary>
        public static readonly DirectProperty<NavigationPage, bool> CanGoBackProperty =
            AvaloniaProperty.RegisterDirect<NavigationPage, bool>(nameof(CanGoBack), o => o.CanGoBack);

        /// <summary>
        /// Defines the attached property controlling whether the back button is enabled for a specific <see cref="Page"/>.
        /// When false the back button is visible but grayed out and non-interactive.
        /// </summary>
        public static readonly AttachedProperty<bool> PageIsBackButtonEnabledEffectiveProperty =
            AvaloniaProperty.RegisterAttached<NavigationPage, Page, bool>("IsBackButtonEnabled", true);

        /// <summary>
        /// Defines the computed back-button enabled state that drives the template binding.
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

            NavBarVisibleEffectiveProperty.Changed.AddClassHandler<NavigationPage>((x, _) =>
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
        /// Gets or sets the foreground brush of the navigation bar (title text, back button, etc.).
        /// </summary>
        public IBrush? BarForeground
        {
            get => GetValue(BarForegroundProperty);
            set => SetValue(BarForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the transition used when pushing or popping pages.
        /// Set to <see langword="null"/> to disable transitions.
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
        /// Gets or sets the computed back-button visibility.
        /// </summary>
        public bool? BackButtonVisibleEffective
        {
            get => GetValue(BackButtonVisibleEffectiveProperty);
            set => SetValue(BackButtonVisibleEffectiveProperty, value);
        }

        /// <summary>
        /// Gets or sets the root page of the navigation stack.
        /// Setting this in XAML (or in code before the first <see cref="Push"/> call) pushes the
        /// value as the initial entry when the stack is empty. After the stack is populated this
        /// property mirrors the currently displayed page; use <see cref="PushAsync(object)"/> and
        /// <see cref="PopAsync()"/> to navigate.
        /// </summary>
        [Content]
        [DependsOn(nameof(PageTemplate))]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the effective (computed) navigation bar visibility.
        /// Determined by the current page's <see cref="HasNavigationBarProperty"/>.
        /// </summary>
        private bool NavBarVisibleEffective
        {
            get => GetValue(NavBarVisibleEffectiveProperty);
            set => SetValue(NavBarVisibleEffectiveProperty, value);
        }

        /// <summary>
        /// Gets or sets the computed effective bar layout behavior for the current page.
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
        /// Gets or sets the effective navigation bar height for the current page
        /// (per-page override if set, otherwise the global <see cref="BarHeight"/>).
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
        /// Gets whether the navigation stack has more than one entry (popping is possible).
        /// Suitable for binding to custom back-button <c>IsEnabled</c> or <c>IsVisible</c>.
        /// </summary>
        public bool CanGoBack => _canGoBack;

        private bool BackButtonEnabledEffective
        {
            get => GetValue(BackButtonEnabledEffectiveProperty);
            set => SetValue(BackButtonEnabledEffectiveProperty, value);
        }

        /// <summary>
        /// Gets the current navigation stack as a read-only list (root at index 0, current page at last index).
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
        /// Gets the current modal stack. The top (most recently pushed) modal is enumerated first.
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
        /// Accepts any object (string label, icon, Control).
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
        /// Accepts any object: a string renders as plain text, a Control renders directly.
        /// </summary>
        public static void SetHeader(Page page, object? header) =>
            page.SetValue(Page.HeaderProperty, header);

        /// <summary>
        /// Gets the top command bar assigned to the specified page.
        /// </summary>
        public static Control? GetTopCommandBar(Page page) =>
            page.GetValue(TopCommandBarProperty);

        /// <summary>
        /// Sets a top command bar for the specified page, displayed inside the navigation bar area.
        /// </summary>
        public static void SetTopCommandBar(Page page, Control? commandBar) =>
            page.SetValue(TopCommandBarProperty, commandBar);

        /// <summary>
        /// Gets the bottom command bar assigned to the specified page.
        /// </summary>
        public static Control? GetBottomCommandBar(Page page) =>
            page.GetValue(BottomCommandBarProperty);

        /// <summary>
        /// Sets a bottom command bar for the specified page, displayed below the page content.
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
        /// Returns null when not explicitly set (NavigationPage defaults to <see cref="BarLayoutBehavior.Inset"/>).
        /// </summary>
        public static BarLayoutBehavior? GetBarLayoutBehavior(Page page) =>
            page.GetValue(PageBarLayoutBehaviorProperty);

        /// <summary>
        /// Sets the bar layout behavior for the specified page.
        /// Use <see cref="BarLayoutBehavior.Overlay"/> to make page content extend behind the navigation bar.
        /// </summary>
        public static void SetBarLayoutBehavior(Page page, BarLayoutBehavior? value) =>
            page.SetValue(PageBarLayoutBehaviorProperty, value);

        /// <summary>
        /// Gets the per-page navigation bar height override for the specified page.
        /// Returns null when not explicitly set (NavigationPage uses its global <see cref="BarHeight"/>).
        /// </summary>
        public static double? GetBarHeight(Page page) =>
            page.GetValue(PageBarHeightProperty);

        /// <summary>
        /// Sets a per-page navigation bar height override.
        /// Pass null to revert to the global <see cref="BarHeight"/>.
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
        /// When false the button is visible but grayed out and non-interactive.
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
            await PopAsync();
        }

        /// <summary>
        /// Returns the page that would become active after a Pop, without modifying the stack.
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
        /// Performs the actual push after the Navigating event has been fired and awaited.
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
        /// Performs the actual pop after the Navigating event has been fired and awaited.
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
        /// Pushes <paramref name="page"/> onto the navigation stack asynchronously using
        /// <paramref name="transition"/>. Pass <see langword="null"/> to push without animation.
        /// </summary>
        public async Task PushAsync(object page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PushAsync(page); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
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
        /// Pass <see langword="null"/> to pop without animation.
        /// </summary>
        public async Task<Page?> PopAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { return await PopAsync(); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
        }

        /// <summary>
        /// Pops all pages to the root page using <see cref="PageTransition"/>.
        /// </summary>
        public Task PopToRootAsync()
        {
            if (StackDepth <= 1)
                return Task.CompletedTask;
            if (_isNavigating)
                return Task.CompletedTask;

            _isNavigating = true;
            try
            {
                var navigationStack = NavigationStack;
                var rootPage = navigationStack.Count > 0 ? navigationStack[0] : null;

                var currentPage = CurrentPage;
                if (currentPage != null)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(rootPage, NavigationType.PopToRoot);
                    currentPage.SendNavigatingFrom(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return Task.CompletedTask;
                }

                if (Pages is Stack<object> stack)
                {
                    while (stack.Count > 1)
                    {
                        var popped = stack.Pop();
                        _pageSet.Remove(popped);
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
                    CurrentPage.SendNavigatedTo(new NavigatedToEventArgs(null, NavigationType.PopToRoot));
                    CurrentPage.SendAppearing();
                    PoppedToRoot?.Invoke(this, new NavigationEventArgs(CurrentPage, NavigationType.PopToRoot));
                }

                return Task.CompletedTask;
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops all pages to the root page using <paramref name="transition"/>.
        /// Pass <see langword="null"/> to pop without animation.
        /// </summary>
        public async Task PopToRootAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PopToRootAsync(); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
        }

        /// <summary>
        /// Pops to a specific page in the stack using <see cref="PageTransition"/>.
        /// </summary>
        public Task PopToPageAsync(Page page)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (!_pageSet.Contains(page))
                throw new ArgumentException("Page is not in the navigation stack.", nameof(page));

            if (_isNavigating)
                return Task.CompletedTask;

            _isNavigating = true;
            try
            {
                var currentPage = CurrentPage;
                if (currentPage != null)
                {
                    var navigatingArgs = new NavigatingFromEventArgs(page, NavigationType.Pop);
                    currentPage.SendNavigatingFrom(navigatingArgs);
                    if (navigatingArgs.Cancel)
                        return Task.CompletedTask;
                }

                if (Pages is Stack<object> stack)
                {
                    while (stack.Count > 1 && stack.Peek() != page)
                    {
                        var popped = stack.Pop();
                        _pageSet.Remove(popped);
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
                    CurrentPage.SendNavigatedTo(new NavigatedToEventArgs(null, NavigationType.Pop));
                    CurrentPage.SendAppearing();
                }

                return Task.CompletedTask;
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Pops all pages above <paramref name="page"/> using <paramref name="transition"/>.
        /// Pass <see langword="null"/> to pop without animation.
        /// </summary>
        public async Task PopToPageAsync(Page page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PopToPageAsync(page); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
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
        /// Pass <see langword="null"/> to push without animation.
        /// </summary>
        public async Task PushModalAsync(object page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PushModalAsync(page); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
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
                            // Sync logical state regardless of whether the transition completed or was cancelled.
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
                            // Sync visual state regardless of whether the transition completed or was cancelled.
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
        /// Pass <see langword="null"/> to pop without animation.
        /// </summary>
        public async Task<Page?> PopModalAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { return await PopModalAsync(); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
        }

        /// <summary>
        /// Pops all modal pages using <see cref="ModalTransition"/>, animating the topmost one out.
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
        /// Pass <see langword="null"/> to dismiss without animation.
        /// </summary>
        public async Task PopAllModalsAsync(IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await PopAllModalsAsync(); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
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
        /// Replaces the top page with <paramref name="page"/> without creating a back entry,
        /// using <see cref="PageTransition"/>.
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
        /// Replaces the top page with <paramref name="page"/> without creating a back entry,
        /// using <paramref name="transition"/>. Pass <see langword="null"/> to replace without animation.
        /// </summary>
        public async Task ReplaceAsync(object page, IPageTransition? transition)
        {
            _overrideTransition = transition;
            _hasOverrideTransition = true;
            try { await ReplaceAsync(page); }
            catch { _hasOverrideTransition = false; _overrideTransition = null; throw; }
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
                    .Subscribe(new AnonymousObserver<bool>(_ => UpdateNavBarVisibleEffective()));

                _isBackButtonEnabledSub = newPage.GetObservable(PageIsBackButtonEnabledEffectiveProperty)
                    .Subscribe(new AnonymousObserver<bool>(_ => UpdateBackButtonEnabledEffective()));

                _barLayoutBehaviorSub = newPage.GetObservable(PageBarLayoutBehaviorProperty)
                    .Subscribe(new AnonymousObserver<BarLayoutBehavior?>(_ => UpdateBarLayoutBehaviorEffective()));

                _barHeightSub = newPage.GetObservable(PageBarHeightProperty)
                    .Subscribe(new AnonymousObserver<double?>(_ => UpdateBarHeightEffective()));
            }

            UpdateNavBarVisibleEffective();
            UpdateBarLayoutBehaviorEffective();
            UpdateBarHeightEffective();

            _cachedNavigationStack = null;
            UpdateContentSafeAreaPadding();
            UpdateBackButtonVisibleEffective();
            UpdateBackButtonEnabledEffective();
        }

        /// <summary>
        /// Runs the page transition and performs cleanup once the animation completes.
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
        /// Atomically swaps the top of the navigation stack with <paramref name="page"/>,
        /// treating the operation as a push-direction transition.
        /// </summary>
        private void ExecuteReplaceCore(object page, Page? replacedPage)
        {
            if (Pages is Stack<object> pagesStack && pagesStack.Count > 0)
                _pageSet.Remove(pagesStack.Pop());
            else if (Pages is IList pagesList && pagesList.Count > 0)
            {
                var top = pagesList[pagesList.Count - 1];
                if (top != null)
                    _pageSet.Remove(top);
                pagesList.RemoveAt(pagesList.Count - 1);
            }

            if (Pages is Stack<object> pushStack)
                pushStack.Push(page);
            else if (Pages is IList pushList)
                pushList.Add(page);

            _pageSet.Add(page);
            _cachedNavigationStack = null;

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
        /// Swaps front and back modal presenters by exchanging field references and ZIndex.
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
            SetCurrentValue(BackButtonVisibleEffectiveProperty, (bool?)(IsBackButtonVisible
                && depth > 1
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

            AutomationProperties.SetName(_backButton, "Go back");
            ToolTip.SetTip(_backButton, "Go back");
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!IsGestureEnabled || StackDepth <= 1 || _isNavigating || _modalStack.Count > 0)
                return;

            var pos = e.GetPosition(this);
            var width = Bounds.Width;

            bool hitEdge = IsRtl
                ? pos.X >= width - EdgeGestureWidth
                : pos.X <= EdgeGestureWidth;

            if (hitEdge)
            {
                _isDragging = true;
                _dragStart = pos;
                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!_isDragging)
                return;

            var pos = e.GetPosition(this);
            var deltaX = pos.X - _dragStart.X;
            var deltaY = pos.Y - _dragStart.Y;

            if (Math.Abs(deltaY) > Math.Abs(deltaX) && Math.Abs(deltaY) > VerticalGestureCancelThreshold)
            {
                _isDragging = false;
                e.Pointer.Capture(null);
                return;
            }

            bool rightDirection = IsRtl ? deltaX < -GestureThreshold : deltaX > GestureThreshold;
            if (!rightDirection)
                return;

            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            _ = PopAsync();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_isDragging)
            {
                e.Pointer.Capture(null);
                _isDragging = false;
            }
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            _isDragging = false;
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

        private void UpdateNavBarVisibleEffective()
        {
            SetCurrentValue(NavBarVisibleEffectiveProperty, CurrentPage == null || GetHasNavigationBar(CurrentPage));
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
                NavBarVisibleEffective && BarLayoutBehaviorEffective == BarLayoutBehavior.Inset);
        }

        private void UpdateBarHeightEffective()
        {
            SetCurrentValue(BarHeightEffectiveProperty, (CurrentPage != null ? GetBarHeight(CurrentPage) : null) ?? BarHeight);
            PseudoClasses.Set(":nav-bar-compact", BarHeightEffective < 40);
        }

        private void ApplyNavBarVisibility()
        {
            if (_navBar != null)
                _navBar.IsVisible = NavBarVisibleEffective;
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
            _navBarShadow.IsVisible = HasShadow && NavBarVisibleEffective;
        }
    }
}
