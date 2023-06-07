using System;
using System.Reactive.Disposables;

using Avalonia.Controls;
using Avalonia.Styling;
using ReactiveUI;
using Splat;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// This content control will automatically load the View associated with
    /// the ViewModel property and display it. This control is very useful
    /// inside a DataTemplate to display the View associated with a ViewModel.
    /// </summary>
    public class ViewModelViewHost : TransitioningContentControl, IViewFor, IEnableLogger
    {
        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="ViewModel"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<object?> ViewModelProperty =
            AvaloniaProperty.Register<ViewModelViewHost, object?>(nameof(ViewModel));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="ViewContract"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> ViewContractProperty =
            AvaloniaProperty.Register<ViewModelViewHost, string?>(nameof(ViewContract));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="DefaultContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DefaultContentProperty =
            AvaloniaProperty.Register<ViewModelViewHost, object?>(nameof(DefaultContent));

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelViewHost"/> class.
        /// </summary>
        public ViewModelViewHost()
        {
            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel, x => x.ViewContract)
                    .Subscribe(tuple => NavigateToViewModel(tuple.Item1, tuple.Item2))
                    .DisposeWith(disposables);
            });
        }

        /// <summary>
        /// Gets or sets the ViewModel to display.
        /// </summary>
        public object? ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
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
        /// Gets or sets the view locator.
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
            if (viewInstance is StyledElement styled)
                styled.DataContext = viewModel;
            Content = viewInstance;
        }
    }
}
