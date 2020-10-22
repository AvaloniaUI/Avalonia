using System;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
using ReactiveUI;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ReactiveUI Window that implements <see cref="IViewFor{TViewModel}"/> and will activate your ViewModel
    /// automatically if it supports activation. When the DataContext property changes, this class will update the
    /// ViewModel property with the new DataContext value, and vice versa.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    public class ReactiveWindow<TViewModel> : Window, IViewFor<TViewModel> where TViewModel : class
    {
        public static readonly StyledProperty<TViewModel> ViewModelProperty = AvaloniaProperty
            .Register<ReactiveWindow<TViewModel>, TViewModel>(nameof(ViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveWindow{TViewModel}"/> class.
        /// </summary>
        public ReactiveWindow()
        {
            this.WhenAnyValue(x => x.DataContext)
                .Subscribe(context => ViewModel = context as TViewModel);
            this.WhenAnyValue(x => x.ViewModel)
                .Subscribe(model => DataContext = model);
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
