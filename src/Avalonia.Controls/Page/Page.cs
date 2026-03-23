using System;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
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
            AvaloniaProperty.Register<Page, Thickness>(nameof(SafeAreaPadding), validate: PaddingProperty.ValidateValue);

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
        /// Defines the <see cref="IconTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty =
            AvaloniaProperty.Register<Page, IDataTemplate?>(nameof(IconTemplate));

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
        /// Gets or sets the data template used to display the icon.
        /// </summary>
        public IDataTemplate? IconTemplate
        {
            get => GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
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
        /// Occurs when the page has been navigated to.
        /// </summary>
        public event EventHandler<NavigatedToEventArgs>? NavigatedTo;

        /// <summary>
        /// Occurs when the page is about to be navigated from.
        /// </summary>
        /// <remarks>
        /// Each subscriber is awaited in turn. Set <see cref="NavigatingFromEventArgs.Cancel"/> to
        /// <see langword="true"/> to abort the navigation; remaining subscribers are not invoked once
        /// cancellation is requested. If a subscriber throws an exception, the exception propagates
        /// to the calling navigation method (such as <see cref="NavigationPage.PushAsync(Page)"/>)
        /// and the navigation is aborted. Subscribers should use try/catch internally if they need
        /// guaranteed cancellation semantics regardless of errors.
        /// </remarks>
        public event Func<NavigatingFromEventArgs, Task>? Navigating;

        /// <summary>
        /// Occurs when the page has been navigated from.
        /// </summary>
        public event EventHandler<NavigatedFromEventArgs>? NavigatedFrom;

        /// <summary>
        /// Called when the page has been navigated to.
        /// </summary>
        protected virtual void OnNavigatedTo(NavigatedToEventArgs args) => NavigatedTo?.Invoke(this, args);

        /// <summary>
        /// Called when the page is about to be navigated from.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="NavigatingFromEventArgs.Cancel"/> to <see langword="true"/> here
        /// prevents the <see cref="Navigating"/> async handlers from running and aborts the
        /// navigation. This method is called before the <see cref="Navigating"/> event.
        /// </remarks>
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

        internal void SendNavigatedTo(NavigatedToEventArgs args) => OnNavigatedTo(args);

        internal async Task SendNavigatingAsync(NavigatingFromEventArgs args)
        {
            OnNavigatingFrom(args);

            if (args.Cancel)
                return;

            var navigating = Navigating;
            if (navigating != null)
            {
                foreach (Func<NavigatingFromEventArgs, Task> handler in navigating.GetInvocationList())
                {
                    await handler(args);
                    if (args.Cancel)
                        return;
                }
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
                AutomationProperties.SetName(this, change.GetNewValue<object?>() as string ?? string.Empty);
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
