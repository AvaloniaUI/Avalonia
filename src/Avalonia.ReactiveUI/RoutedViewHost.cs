// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
    public class RoutedViewHost : TransitioningContentControl, IActivatable, IEnableLogger
    {
        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="Router"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<RoutingState> RouterProperty =
            AvaloniaProperty.Register<RoutedViewHost, RoutingState>(nameof(Router));
    
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedViewHost"/> class.
        /// </summary>
        public RoutedViewHost()
        {
            this.WhenActivated(disposables =>
            {
                this.WhenAnyObservable(x => x.Router.CurrentViewModel)
                    .DistinctUntilChanged()
                    .Subscribe(NavigateToViewModel)
                    .DisposeWith(disposables);
            });
        }
        
        /// <summary>
        /// Gets or sets the <see cref="RoutingState"/> of the view model stack.
        /// </summary>
        public RoutingState Router
        {
            get => GetValue(RouterProperty);
            set => SetValue(RouterProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the ReactiveUI view locator used by this router.
        /// </summary>
        public IViewLocator ViewLocator { get; set; }
    
        /// <summary>
        /// Invoked when ReactiveUI router navigates to a view model.
        /// </summary>
        /// <param name="viewModel">ViewModel to which the user navigates.</param>
        private void NavigateToViewModel(object viewModel)
        {
            if (viewModel == null)
            {
                this.Log().Info("ViewModel is null. Falling back to default content.");
                Content = DefaultContent;
                return;
            }
    
            var viewLocator = ViewLocator ?? global::ReactiveUI.ViewLocator.Current;
            var viewInstance = viewLocator.ResolveView(viewModel);
            if (viewInstance == null)
            {
                this.Log().Warn($"Couldn't find view for '{viewModel}'. Is it registered? Falling back to default content.");
                Content = DefaultContent;
                return;
            }
    
            this.Log().Info($"Ready to show {viewInstance} with autowired {viewModel}.");
            viewInstance.ViewModel = viewModel;
            if (viewInstance is IStyledElement styled)
                styled.DataContext = viewModel;
            Content = viewInstance;
        }
    }
}