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
            get => _showNavBar;
            set => RaiseAndSetIfChanged(ref _showNavBar, value);
        }

        public bool? ShowBackButton
        {
            get => _showBackButton;
            set => RaiseAndSetIfChanged(ref _showBackButton, value);
        }
        public INavigationRouter NavigationRouter
        {
            get => _navigationRouter; 
            set => RaiseAndSetIfChanged(ref _navigationRouter, value);
        }

        public async void NavigateTo(object page)
        {
            if (NavigationRouter != null)
            {
                await NavigationRouter.NavigateToAsync(page);
            }
        }
    }
}
