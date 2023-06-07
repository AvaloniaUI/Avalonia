using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia;
using ReactiveUI;
using Splat;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// This control hosts the View associated with ReactiveUI RoutingState,
    /// and will display the View and wire up the ViewModel whenever a new
    /// ViewModel is navigated to. Nested routing is also supported.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ReactiveUI routing consists of an IScreen that contains current
    /// RoutingState, several IRoutableViewModels, and a platform-specific
    /// XAML control called RoutedViewHost.
    /// </para>
    /// <para>
    /// RoutingState manages the ViewModel navigation stack and allows
    /// ViewModels to navigate to other ViewModels. IScreen is the root of
    /// a navigation stack; despite the name, its views don't have to occupy
    /// the whole screen. RoutedViewHost monitors an instance of RoutingState,
    /// responding to any changes in the navigation stack by creating and
    /// embedding the appropriate view.
    /// </para>
    /// <para>
    /// Place this control to a view containing your ViewModel that implements
    /// IScreen, and bind IScreen.Router property to RoutedViewHost.Router property.
    /// <code>
    /// <![CDATA[
    /// <rxui:RoutedViewHost
    ///     HorizontalAlignment="Stretch"
    ///     VerticalAlignment="Stretch"
    ///     Router="{Binding Router}">
    ///     <rxui:RoutedViewHost.DefaultContent>
    ///         <TextBlock Text="Default Content"/>
    ///     </rxui:RoutedViewHost.DefaultContent>
    /// </rxui:RoutedViewHost>
    /// ]]>
    /// </code>
    /// </para>
    /// <para>
    /// See <see href="https://reactiveui.net/docs/handbook/routing/">
    /// ReactiveUI routing documentation website</see> for more info.
    /// </para>
    /// </remarks>
    public class RoutedViewHost : TransitioningContentControl, IActivatableView, IEnableLogger
    {
        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="Router"/> property.
        /// </summary>
        public static readonly StyledProperty<RoutingState?> RouterProperty =
            AvaloniaProperty.Register<RoutedViewHost, RoutingState?>(nameof(Router));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="ViewContract"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> ViewContractProperty =
            AvaloniaProperty.Register<RoutedViewHost, string?>(nameof(ViewContract));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="DefaultContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DefaultContentProperty =
            ViewModelViewHost.DefaultContentProperty.AddOwner<RoutedViewHost>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedViewHost"/> class.
        /// </summary>
        public RoutedViewHost()
        {
            this.WhenActivated(disposables =>
            {
                var routerRemoved = this
                    .WhenAnyValue(x => x.Router)
                    .Where(router => router == null)!
                    .Cast<object?>();

                var viewContract = this.WhenAnyValue(x => x.ViewContract);

                this.WhenAnyValue(x => x.Router)
                    .Where(router => router != null)
                    .SelectMany(router => router!.CurrentViewModel)
                    .Merge(routerRemoved)
                    .CombineLatest(viewContract)
                    .Subscribe(tuple => NavigateToViewModel(tuple.First, tuple.Second))
                    .DisposeWith(disposables);
            });
        }

        /// <summary>
        /// Gets or sets the <see cref="RoutingState"/> of the view model stack.
        /// </summary>
        public RoutingState? Router
        {
            get => GetValue(RouterProperty);
            set => SetValue(RouterProperty, value);
        }

        /// <summary>
        /// Gets or sets the view contract.
        /// </summary>
        public string? ViewContract
        {
            get => GetValue(ViewContractProperty);
            set => SetValue(ViewContractProperty, value);
        }

        /// <summary>
        /// Gets or sets the content displayed whenever there is no page currently routed.
        /// </summary>
        public object? DefaultContent
        {
            get => GetValue(DefaultContentProperty);
            set => SetValue(DefaultContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the ReactiveUI view locator used by this router.
        /// </summary>
        public IViewLocator? ViewLocator { get; set; }

        protected override Type StyleKeyOverride => typeof(TransitioningContentControl);
        
        /// <summary>
        /// Invoked when ReactiveUI router navigates to a view model.
        /// </summary>
        /// <param name="viewModel">ViewModel to which the user navigates.</param>
        /// <param name="contract">The contract for view resolution.</param>
        private void NavigateToViewModel(object? viewModel, string? contract)
        {
            if (Router == null)
            {
                this.Log().Warn("Router property is null. Falling back to default content.");
                Content = DefaultContent;
                return;
            }

            if (viewModel == null)
            {
                this.Log().Info("ViewModel is null. Falling back to default content.");
                Content = DefaultContent;
                return;
            }

            var viewLocator = ViewLocator ?? global::ReactiveUI.ViewLocator.Current;
            var viewInstance = viewLocator.ResolveView(viewModel, contract);
            if (viewInstance == null)
            {
                if (contract == null)
                {
                    this.Log().Warn($"Couldn't find view for '{viewModel}'. Is it registered? Falling back to default content.");
                }
                else
                {
                    this.Log().Warn($"Couldn't find view with contract '{contract}' for '{viewModel}'. Is it registered? Falling back to default content.");
                }

                Content = DefaultContent;
                return;
            }

            if (contract == null)
            {
                this.Log().Info($"Ready to show {viewInstance} with autowired {viewModel}.");
            }
            else
            {
                this.Log().Info($"Ready to show {viewInstance} with autowired {viewModel} and contract '{contract}'.");
            }

            viewInstance.ViewModel = viewModel;
            if (viewInstance is IDataContextProvider provider)
                provider.DataContext = viewModel;
            Content = viewInstance;
        }
    }
}
