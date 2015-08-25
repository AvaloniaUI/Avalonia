namespace Perspex.Xaml.Desktop
{
    using Controls;
    using OmniXaml.AppServices.Mvvm;

    public class PerspexWindow : Window, IView
    {
        public void SetViewModel(object viewModel)
        {
            this.DataContext = viewModel;
        }
    }
}