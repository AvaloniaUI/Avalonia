using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUIDemo.ViewModels;

namespace ReactiveUIDemo.Views
{
    internal partial class FooView : UserControl, IViewFor<FooViewModel>
    {
        public FooView()
        {
            InitializeComponent();
        }

        public FooViewModel? ViewModel { get; set; }
        
        object? IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (FooViewModel?)value;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
