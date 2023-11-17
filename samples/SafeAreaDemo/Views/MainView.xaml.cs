using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SafeAreaDemo.ViewModels;

namespace SafeAreaDemo.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc/>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
            if (insetsManager != null && DataContext is MainViewModel viewModel)
            {
                viewModel.InsetsManager = insetsManager;
            }
        }
    }
}
