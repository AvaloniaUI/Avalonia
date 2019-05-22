// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using ReactiveUI;
using Splat;

namespace Avalonia.ReactiveUI 
{
    /// <summary>
    /// This content control will automatically load the View associated with
    /// the ViewModel property and display it. This control is very useful
    /// inside a DataTemplate to display the View associated with a ViewModel.
    /// </summary>
    public class ViewModelViewHost : TransitioningUserControl, IViewFor, IEnableLogger
    {
        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="ViewModel"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<object> ViewModelProperty =
            AvaloniaProperty.Register<ViewModelViewHost, object>(nameof(ViewModel));
        
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
        /// Updates the Content when ViewModel changes.
        /// </summary>
        /// <param name="e">Property changed event arguments.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(ViewModel)) NavigateToViewModel(e.NewValue);
            base.OnPropertyChanged(e);
        }
        
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