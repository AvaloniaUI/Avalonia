using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using ReactiveUI;
using Splat;

namespace Avalonia
{
    /// <summary>
    /// This control hosts the View associated with a Router, and will display
    /// the View and wire up the ViewModel whenever a new ViewModel is navigated to.
    /// </summary>
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
    
        private readonly IAnimation _fadeOutAnimation = CreateOpacityAnimation(1d, 0d, TimeSpan.FromSeconds(0.25));
        private readonly IAnimation _fadeInAnimation = CreateOpacityAnimation(0d, 1d, TimeSpan.FromSeconds(0.25));
    
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedViewHost"/> class.
        /// </summary>
        public RoutedViewHost()
        {
            this.WhenActivated(disposables =>
            {
                this.WhenAnyObservable(x => x.Router.CurrentViewModel)
                    .DistinctUntilChanged()
                    .Subscribe(HandleViewModelChange)
                    .DisposeWith(disposables);
            });
        }
        
        /// <summary>
        /// Gets or sets the ReactiveUI view locator used by this router.
        /// </summary>
        public IViewLocator ViewLocator { get; set; }
    
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
        /// Duplicates the Content property with a private setter.
        /// </summary>
        public new object Content
        {
            get => base.Content;
            private set => base.Content = value;
        }
    
        /// <summary>
        /// Invoked when ReactiveUI router navigates to a view model.
        /// </summary>
        /// <param name="viewModel">ViewModel to which the user navigates.</param>
        /// <exception cref="Exception">
        /// Thrown when ViewLocator is unable to find the appropriate view.
        /// </exception>
        private void HandleViewModelChange(IRoutableViewModel viewModel)
        {
            if (viewModel == null)
            {
                this.Log().Info("ViewModel is null, falling back to default content.");
                UpdateContent(DefaultContent);
                return;
            }
    
            var viewLocator = ViewLocator ?? ReactiveUI.ViewLocator.Current;
            var view = viewLocator.ResolveView(viewModel);
            if (view == null) throw new Exception($"Couldn't find view for '{viewModel}'. Is it registered?");
    
            this.Log().Info($"Ready to show {view} with autowired {viewModel}.");
            view.ViewModel = viewModel;
            UpdateContent(view);
        }
    
        /// <summary>
        /// Updates the content with transitions.
        /// </summary>
        /// <param name="newContent">New content to set.</param>
        private async void UpdateContent(object newContent)
        {
            await _fadeOutAnimation.RunAsync(this, null);
            Content = newContent;
            await _fadeInAnimation.RunAsync(this, null);
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
