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
    public class RoutedViewHost : UserControl, IActivatable, IEnableLogger
    {
        /// <summary>
        /// The router dependency property.
        /// </summary>
        public static readonly AvaloniaProperty<RoutingState> RouterProperty =
            AvaloniaProperty.Register<RoutedViewHost, RoutingState>(nameof(Router));
        
        /// <summary>
        /// The default content property.
        /// </summary> 
        public static readonly AvaloniaProperty<object> DefaultContentProperty =
            AvaloniaProperty.Register<RoutedViewHost, object>(nameof(DefaultContent));

        /// <summary>
        /// Fade in animation property.
        /// </summary>
        public static readonly AvaloniaProperty<IAnimation> FadeInAnimationProperty =
            AvaloniaProperty.Register<RoutedViewHost, IAnimation>(nameof(DefaultContent),
                CreateOpacityAnimation(0d, 1d, TimeSpan.FromSeconds(0.25)));

        /// <summary>
        /// Fade out animation property.
        /// </summary>
        public static readonly AvaloniaProperty<IAnimation> FadeOutAnimationProperty =
            AvaloniaProperty.Register<RoutedViewHost, IAnimation>(nameof(DefaultContent),
                CreateOpacityAnimation(1d, 0d, TimeSpan.FromSeconds(0.25)));
    
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
        /// Gets or sets the content displayed whenever there is no page currently routed.
        /// </summary>
        public object DefaultContent
        {
            get => GetValue(DefaultContentProperty);
            set => SetValue(DefaultContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation played when page appears.
        /// </summary>
        public IAnimation FadeInAnimation
        {
            get => GetValue(FadeInAnimationProperty);
            set => SetValue(FadeInAnimationProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation played when page disappears.
        /// </summary>
        public IAnimation FadeOutAnimation
        {
            get => GetValue(FadeOutAnimationProperty);
            set => SetValue(FadeOutAnimationProperty, value);
        }
    
        /// <summary>
        /// Duplicates the Content property with a private setter.
        /// </summary>
        public new object Content
        {
            get => base.Content;
            private set => base.Content = value;
        }

        /// <summary>
        /// Gets or sets the ReactiveUI view locator used by this router.
        /// </summary>
        public IViewLocator ViewLocator { get; set; }
    
        /// <summary>
        /// Invoked when ReactiveUI router navigates to a view model.
        /// </summary>
        /// <param name="viewModel">ViewModel to which the user navigates.</param>
        /// <exception cref="Exception">
        /// Thrown when ViewLocator is unable to find the appropriate view.
        /// </exception>
        private void NavigateToViewModel(IRoutableViewModel viewModel)
        {
            if (viewModel == null)
            {
                this.Log().Info("ViewModel is null, falling back to default content.");
                UpdateContent(DefaultContent);
                return;
            }
    
            var viewLocator = ViewLocator ?? global::ReactiveUI.ViewLocator.Current;
            var view = viewLocator.ResolveView(viewModel);
            if (view == null) throw new Exception($"Couldn't find view for '{viewModel}'. Is it registered?");
    
            this.Log().Info($"Ready to show {view} with autowired {viewModel}.");
            view.ViewModel = viewModel;
            if (view is IStyledElement styled)
                styled.DataContext = viewModel;
            UpdateContent(view);
        }
    
        /// <summary>
        /// Updates the content with transitions.
        /// </summary>
        /// <param name="newContent">New content to set.</param>
        private async void UpdateContent(object newContent)
        {
            if (FadeOutAnimation != null)
                await FadeOutAnimation.RunAsync(this, Clock);
            Content = newContent;
            if (FadeInAnimation != null)
                await FadeInAnimation.RunAsync(this, Clock);
        }
    
        /// <summary>
        /// Creates opacity animation for this routed view host.
        /// </summary>
        /// <param name="from">Opacity to start from.</param>
        /// <param name="to">Opacity to finish with.</param>
        /// <param name="duration">Duration of the animation.</param>
        /// <returns>Animation object instance.</returns>
        private static IAnimation CreateOpacityAnimation(double from, double to, TimeSpan duration) 
        {
            return new Avalonia.Animation.Animation
            {
                Duration = duration,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = from
                            }
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = to
                            }
                        },
                        Cue = new Cue(1d)
                    }
                }
            };
        }
    }
}
