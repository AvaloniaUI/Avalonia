using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
using ReactiveUI;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ReactiveUI <see cref="UserControl"/> that implements the <see cref="IViewFor{TViewModel}"/> interface and
    /// will activate your ViewModel automatically if the view model implements <see cref="IActivatableViewModel"/>.
    /// When the DataContext property changes, this class will update the ViewModel property with the new DataContext
    /// value, and vice versa.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    public class ReactiveUserControl<TViewModel> : UserControl, IViewFor<TViewModel> where TViewModel : class
    {
        public static readonly StyledProperty<TViewModel?> ViewModelProperty = AvaloniaProperty
            .Register<ReactiveUserControl<TViewModel>, TViewModel?>(nameof(ViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveUserControl{TViewModel}"/> class.
        /// </summary>
        public ReactiveUserControl()
        {
            // This WhenActivated block calls ViewModel's WhenActivated
            // block if the ViewModel implements IActivatableViewModel.
            this.WhenActivated(disposables => { });
            this.GetObservable(ViewModelProperty).Subscribe(OnViewModelChanged);
        }

        /// <summary>
        /// The ViewModel.
        /// </summary>
        public TViewModel? ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TViewModel?)value;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            ViewModel = DataContext as TViewModel;
        }

        private void OnViewModelChanged(object? value)
        {
            if (value == null)
            {
                ClearValue(DataContextProperty);
            }
            else if (DataContext != value)
            {
                DataContext = value;
            }
        }
    }
}
