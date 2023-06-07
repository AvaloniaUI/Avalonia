using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUIDemo.ViewModels;

namespace ReactiveUIDemo.Views
{
    internal partial class BarView : UserControl, IViewFor<BarViewModel>
    {
        public BarView()
        {
            InitializeComponent();
        }

        public BarViewModel? ViewModel { get; set; }
        
        object? IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (BarViewModel?)value;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
