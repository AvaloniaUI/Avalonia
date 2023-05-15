using Avalonia.Controls;
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

        protected override void OnLoaded()
        {
            base.OnLoaded();

            var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
            if (insetsManager != null && DataContext is MainViewModel viewModel)
            {
                viewModel.InsetsManager = insetsManager;
            }
        }
    }
}
