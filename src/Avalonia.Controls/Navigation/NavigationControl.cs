using System;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// A navigation controlt that supports simple stack-based navigation
    /// </summary>
    public class NavigationControl : ContentControl
    {
        private Button? _backButton;
        private INavigationRouter? _navigationRouter;

        /// <summary>
        /// Raised when the back is requested.
        /// </summary>
        public event EventHandler BackRequested
        {
            add => AddHandler(BackRequestedEvent, value);
            remove => RemoveHandler(BackRequestedEvent, value);
        }

        /// <summary>
        /// Defines the <see cref="CanGoBack"/> property.
        /// </summary>
        public static readonly DirectProperty<NavigationControl, bool?> CanGoBackProperty =
            AvaloniaProperty.RegisterDirect<NavigationControl, bool?>(nameof(CanGoBack),
                o => o.CanGoBack, (o, v) => o.CanGoBack = v);

        /// <summary>
        /// Defines the <see cref="CurrentView"/> property.
        /// </summary>
        public static readonly DirectProperty<NavigationControl, object?> CurrentViewProperty =
            AvaloniaProperty.RegisterDirect<NavigationControl, object?>(nameof(CurrentView),
                o => o.CurrentView, (o, v) => o.CurrentView = v);

        /// <summary>
        /// Defines the <see cref="IsBackButtonVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool?> IsBackButtonVisibleProperty =
            AvaloniaProperty.Register<NavigationControl, bool?>(nameof(IsBackButtonVisible), true);

        /// <summary>
        /// Defines the <see cref="IsNavBarVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool?> IsNavBarVisibleProperty =
            AvaloniaProperty.Register<NavigationControl, bool?>(nameof(IsNavBarVisible), true);

        /// <summary>
        /// Defines the <see cref="BackRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent BackRequestedEvent =
            RoutedEvent.Register<NavigationControl, RoutedEventArgs>(nameof(BackRequested), RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<NavigationControl, IPageTransition?>(nameof(PageTransition),
                new CrossFade(TimeSpan.FromSeconds(0.125)));

        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
            AvaloniaProperty.Register<NavigationControl, IDataTemplate?>(nameof(HeaderTemplate));

        /// <summary>
        /// Defines the <see cref="INavigationRouter"/> property.
        /// </summary>
        public static readonly DirectProperty<NavigationControl, INavigationRouter?> NavigationRouterProperty =
            AvaloniaProperty.RegisterDirect<NavigationControl, INavigationRouter?>(nameof(NavigationRouter),
                o => o.NavigationRouter, (o,v) => o.NavigationRouter = v);

        /// <summary>
        /// Gets whether it's possible to go back in the stack
        /// </summary>
        public bool? CanGoBack
        {
            get => NavigationRouter?.CanGoBack;
            set
            {
                if (NavigationRouter != null)
                {
                    NavigationRouter.CanGoBack = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the visibility of the navigation bar
        /// </summary>
        public bool? IsBackButtonVisible
        {
            get => GetValue(IsBackButtonVisibleProperty);
            set => SetValue(IsBackButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the navigation bar
        /// </summary>
        public bool? IsNavBarVisible
        {
            get => GetValue(IsNavBarVisibleProperty);
            set => SetValue(IsNavBarVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation played when content appears and disappears.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets the header template
        /// </summary>
        public IDataTemplate? HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the navigation router.
        /// </summary>
        public INavigationRouter? NavigationRouter
        {
            get => _navigationRouter;
            set
            {
                var oldRouter = NavigationRouter;

                if(oldRouter != null)
                {
                    oldRouter.PropertyChanged -= NavigationRouter_PropertyChanged;
                }

                SetAndRaise(NavigationRouterProperty, ref _navigationRouter, value);

                if(value != null)
                {
                    value.PropertyChanged += NavigationRouter_PropertyChanged;
                }
            }
        }

        /// <summary>
        /// Gets or sets the animation played when content appears and disappears.
        /// </summary>
        public object? CurrentView
        {
            get => NavigationRouter?.CurrentView;
            set
            {
                var oldView = CurrentView;
                NavigationRouter?.NavigateTo(value);
                RaisePropertyChanged(CurrentViewProperty, oldView, CurrentView);
            }
        }

        /// <summary>
        /// Gets or sets the BackButton template part.
        /// </summary>
        private Button? BackButton
        {
            get { return _backButton; }
            set
            {
                if (_backButton != null)
                {
                    _backButton.Click -= BackButton_Clicked;
                }
                _backButton = value;
                if (_backButton != null)
                {
                    _backButton.Click += BackButton_Clicked;
                }
            }
        }

        public NavigationControl()
        {
            NavigationRouter = new NavigationRouter();
        }

        private void NavigationRouter_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(NavigationRouter.CurrentView):
                    RaisePropertyChanged(CurrentViewProperty, null, CurrentView);
                    RaisePropertyChanged(CanGoBackProperty, null, CanGoBack);
                    break;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            BackButton = e.NameScope.Get<Button>("PART_BackButton");
        }

        private async void BackButton_Clicked(object? sender, RoutedEventArgs eventArgs)
        {
            await NavigationRouter?.GoBack();
        }
    }
}
