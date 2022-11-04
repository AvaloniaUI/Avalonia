using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class NavigationPageViewModel : ViewModelBase
    {
        private bool? _showNavBar = true;
        private bool? _showBackButton = true;
        private INavigationRouter _navigationRouter;

        public NavigationPageViewModel()
        {
            _navigationRouter = new NavigationRouter();
        }

        public bool? ShowNavBar
        {
            get => _showNavBar; set
            {
                _showNavBar = value;

                RaisePropertyChanged();
            }
        }

        public bool? ShowBackButton
        {
            get => _showBackButton; set
            {
                _showBackButton = value;

                RaisePropertyChanged();
            }
        }
        public INavigationRouter NavigationRouter
        {
            get => _navigationRouter; set
            {
                _navigationRouter = value;

                RaisePropertyChanged();
            }
        }

        public async void NavigateTo(object page)
        {
            if (NavigationRouter != null)
            {
                await NavigationRouter.NavigateTo(page);
            }
        }
    }
}
