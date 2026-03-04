using System;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Abstract base class for all page types.
    /// </summary>
    public abstract class Page : TemplatedControl, IHeadered
    {
        private INavigation? _navigation;

        /// <summary>
        /// Defines the <see cref="SafeAreaPadding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty =
            AvaloniaProperty.Register<Page, Thickness>(nameof(SafeAreaPadding));

        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> HeaderProperty =
            AvaloniaProperty.Register<Page, object?>(nameof(Header));

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<Page, object?>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="CurrentPage"/> property.
        /// </summary>
        public static readonly StyledProperty<Page?> CurrentPageProperty =
            AvaloniaProperty.Register<Page, Page?>(nameof(CurrentPage));

        /// <summary>
        /// Defines the <see cref="IsInNavigationPage"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsInNavigationPageProperty =
            AvaloniaProperty.Register<Page, bool>(nameof(IsInNavigationPage));

        /// <summary>
        /// Defines the routed event raised when the system back button is pressed.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PageNavigationSystemBackButtonPressedEvent =
            RoutedEvent.Register<Page, RoutedEventArgs>(
                nameof(PageNavigationSystemBackButtonPressed),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Navigation"/> property.
        /// </summary>
        public static readonly DirectProperty<Page, INavigation?> NavigationProperty =
            AvaloniaProperty.RegisterDirect<Page, INavigation?>(
                nameof(Navigation),
                o => o.Navigation,
                (o, v) => o.Navigation = v);

        static Page()
        {
            PageNavigationSystemBackButtonPressedEvent.AddClassHandler<Page>((page, args) =>
            {
                if (!args.Handled && page.OnSystemBackButtonPressed())
                {
                    args.Handled = true;
                    return;
                }

                page.CurrentPage?.RaiseEvent(args);
            });

            AffectsMeasure<Page>(SafeAreaPaddingProperty);
        }

        /// <summary>
        /// Gets or sets the header content displayed in the navigation bar or tab strip.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon displayed alongside the page header.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the safe-area padding applied to this page's content.
        /// </summary>
        public Thickness SafeAreaPadding
        {
            get => GetValue(SafeAreaPaddingProperty);
            set => SetValue(SafeAreaPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the currently active child page.
        /// </summary>
        public Page? CurrentPage
        {
            get => GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="INavigation"/> service provided by the parent NavigationPage.
        /// </summary>
        public INavigation? Navigation
        {
            get => _navigation;
            set => SetAndRaise(NavigationProperty, ref _navigation, value);
        }

        /// <summary>
        /// Gets or sets whether this page is currently hosted inside a NavigationPage.
        /// </summary>
        public bool IsInNavigationPage
        {
            get => GetValue(IsInNavigationPageProperty);
            set => SetValue(IsInNavigationPageProperty, value);
        }

        /// <summary>
        /// Raised when the system back button is pressed while this page is active.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? PageNavigationSystemBackButtonPressed
        {
            add => AddHandler(PageNavigationSystemBackButtonPressedEvent, value);
            remove => RemoveHandler(PageNavigationSystemBackButtonPressedEvent, value);
        }

        /// <summary>
        /// Occurs when the page becomes visible.
        /// </summary>
        public event EventHandler? Appearing;

        /// <summary>
        /// Occurs when the page is no longer visible.
        /// </summary>
        public event EventHandler? Disappearing;

        /// <summary>
        /// Occurs when the page has been navigated to.
        /// </summary>
        public event EventHandler<NavigatedToEventArgs>? NavigatedTo;

        /// <summary>
        /// Occurs when the page is about to be navigated from.
        /// </summary>
        public event Func<NavigatingFromEventArgs, Task>? Navigating;

        /// <summary>
        /// Occurs when the page has been navigated from.
        /// </summary>
        public event EventHandler<NavigatedFromEventArgs>? NavigatedFrom;

        /// <summary>
        /// Called when the page becomes visible.
        /// </summary>
        protected virtual void OnAppearing() => Appearing?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the page is no longer visible.
        /// </summary>
        protected virtual void OnDisappearing() => Disappearing?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Called when the page has been navigated to.
        /// </summary>
        protected virtual void OnNavigatedTo(NavigatedToEventArgs args) => NavigatedTo?.Invoke(this, args);

        /// <summary>
        /// Called when the page is about to be navigated from.
        /// </summary>
        protected virtual void OnNavigatingFrom(NavigatingFromEventArgs args) { }

        /// <summary>
        /// Called when the page has been navigated from.
        /// </summary>
        protected virtual void OnNavigatedFrom(NavigatedFromEventArgs args) => NavigatedFrom?.Invoke(this, args);

        /// <summary>
        /// Called when the system back button is pressed.
        /// </summary>
        /// <returns><see langword="true"/> if the back press was handled.</returns>
        protected virtual bool OnSystemBackButtonPressed() => false;

        internal void SendAppearing() => OnAppearing();

        internal void SendDisappearing() => OnDisappearing();

        internal void SendNavigatedTo(NavigatedToEventArgs args) => OnNavigatedTo(args);

        internal async Task SendNavigatingAsync(NavigatingFromEventArgs args)
        {
            OnNavigatingFrom(args);

            var navigating = Navigating;
            if (navigating != null)
            {
                foreach (Func<NavigatingFromEventArgs, Task> handler in navigating.GetInvocationList())
                    await handler(args);
            }
        }

        internal void SendNavigatedFrom(NavigatedFromEventArgs args) => OnNavigatedFrom(args);

        internal void SetInNavigationPage(bool value) => SetCurrentValue(IsInNavigationPageProperty, value);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SafeAreaPaddingProperty || change.Property == PaddingProperty)
                UpdateContentSafeAreaPadding();

            if (change.Property == HeaderProperty)
                AutomationProperties.SetName(this, change.NewValue as string ?? string.Empty);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            UpdateContentSafeAreaPadding();
        }

        /// <summary>
        /// Called when the safe-area padding changes.
        /// </summary>
        protected virtual void UpdateContentSafeAreaPadding() { }

        /// <summary>
        /// Called when the active child page changes.
        /// </summary>
        protected virtual void UpdateActivePage() { }
    }
}
