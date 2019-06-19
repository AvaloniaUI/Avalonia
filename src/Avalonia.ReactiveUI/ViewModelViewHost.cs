// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
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
        public static readonly AvaloniaProperty<object> ViewModelProperty =
            AvaloniaProperty.Register<ViewModelViewHost, object>(nameof(ViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelViewHost"/> class.
        /// </summary>
        public ViewModelViewHost()
        {
            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .Subscribe(NavigateToViewModel)
                    .DisposeWith(disposables);
            });
        }

        /// <summary>
        /// Gets or sets the ViewModel to display.
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the view locator.
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