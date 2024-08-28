using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ContainerDemo.ViewModels;

namespace ContainerDemo.Views
{
    public partial class MainView : UserControl
    {
        private MainViewModel _viewModel;

        public MainView()
        {
            _viewModel = new MainViewModel();
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc/>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            DataContext = _viewModel;
        }

        private void SplitView_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == SplitView.DisplayModeProperty && e.GetNewValue<SplitViewDisplayMode>() is { } displayMode)
            {
                _viewModel.IsPaneOpen = !(displayMode == SplitViewDisplayMode.Overlay);
            }
        }
    }
}
