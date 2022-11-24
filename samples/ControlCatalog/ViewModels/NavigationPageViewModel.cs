using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class NavigationPageViewModel : ViewModelBase
    {
        private bool? _showNavBar = true;
        private bool? _showBackButton = true;
        private INavigationRouter _navigationRouter;
        private string? _title;

        public NavigationPageViewModel()
        {
            _navigationRouter = new StackNavigationRouter();
        }

        public bool? ShowNavBar
        {
            get => _showNavBar;
            set => RaiseAndSetIfChanged(ref _showNavBar, value);
        }

        public string? Title
        {
            get => _title;
            set => RaiseAndSetIfChanged(ref _title, value);
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
                if(page is ListBoxPageViewModel)
                {
                    Title = "ListBox Page";
                }
                else if(page is MenuPageViewModel)
                {
                    Title = "Menu Page";
                }

                await NavigationRouter.NavigateToAsync(page);
            }
        }
    }
}
