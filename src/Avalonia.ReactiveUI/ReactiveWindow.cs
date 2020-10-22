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
    /// A ReactiveUI <see cref="Window"/> that implements the <see cref="IViewFor{TViewModel}"/> interface and will
    /// activate your ViewModel automatically if the view model implements <see cref="IActivatableViewModel"/>. When
    /// the DataContext property changes, this class will update the ViewModel property with the new DataContext value,
    /// and vice versa.
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
            this.WhenActivated(disposables => { });
            this.WhenAnyValue(x => x.ViewModel)
                .Skip(1)
                .Subscribe(model => DataContext = model);
            this.WhenAnyValue(x => x.DataContext)
                .Skip(1)
                .Subscribe(context => ViewModel = context as TViewModel);
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
