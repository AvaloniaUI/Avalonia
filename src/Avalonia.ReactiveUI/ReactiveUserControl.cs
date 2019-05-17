// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
using ReactiveUI;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ReactiveUI UserControl that implements <see cref="IViewFor{TViewModel}"/> 
    /// and will activate your ViewModel automatically if it supports activation.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    public class ReactiveUserControl<TViewModel> : UserControl, IViewFor<TViewModel> where TViewModel : class
    {
        public static readonly AvaloniaProperty<TViewModel> ViewModelProperty = AvaloniaProperty
            .Register<ReactiveUserControl<TViewModel>, TViewModel>(nameof(ViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveUserControl{TViewModel}"/> class.
        /// </summary>
        public ReactiveUserControl()
        {
            DataContextChanged += (sender, args) => ViewModel = DataContext as TViewModel;
        }

        /// <summary>
        /// The ViewModel.
        /// </summary>
        public TViewModel ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TViewModel)value;
        }
    }
}
