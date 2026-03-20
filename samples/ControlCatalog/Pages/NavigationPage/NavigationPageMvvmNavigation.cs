using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    internal interface ISampleNavigationService
    {
        event EventHandler<NavigationStateChangedEventArgs>? StateChanged;

        Task NavigateToAsync(ViewModelBase viewModel);

        Task GoBackAsync();

        Task PopToRootAsync();
    }

    internal interface ISamplePageFactory
    {
        ContentPage CreatePage(ViewModelBase viewModel);
    }

    internal sealed class NavigationStateChangedEventArgs : EventArgs
    {
        public NavigationStateChangedEventArgs(string currentPageHeader, int navigationDepth, string lastAction)
        {
            CurrentPageHeader = currentPageHeader;
            NavigationDepth = navigationDepth;
            LastAction = lastAction;
        }

        public string CurrentPageHeader { get; }

        public int NavigationDepth { get; }

        public string LastAction { get; }
    }

    internal sealed class SampleNavigationService : ISampleNavigationService
    {
        private readonly NavigationPage _navigationPage;
        private readonly ISamplePageFactory _pageFactory;

        public SampleNavigationService(NavigationPage navigationPage, ISamplePageFactory pageFactory)
        {
            _navigationPage = navigationPage;
            _pageFactory = pageFactory;

            _navigationPage.Pushed += (_, e) => PublishState($"Pushed {e.Page?.Header}");
            _navigationPage.Popped += (_, e) => PublishState($"Popped {e.Page?.Header}");
            _navigationPage.PoppedToRoot += (_, _) => PublishState("Popped to root");
        }

        public event EventHandler<NavigationStateChangedEventArgs>? StateChanged;

        public async Task NavigateToAsync(ViewModelBase viewModel)
        {
            var page = _pageFactory.CreatePage(viewModel);
            await _navigationPage.PushAsync(page);
        }

        public async Task GoBackAsync()
        {
            if (_navigationPage.NavigationStack.Count <= 1)
            {
                PublishState("Already at the root page");
                return;
            }

            await _navigationPage.PopAsync();
        }

        public async Task PopToRootAsync()
        {
            if (_navigationPage.NavigationStack.Count <= 1)
            {
                PublishState("Already at the root page");
                return;
            }

            await _navigationPage.PopToRootAsync();
        }

        private void PublishState(string lastAction)
        {
            var header = _navigationPage.CurrentPage?.Header?.ToString() ?? "None";

            StateChanged?.Invoke(this, new NavigationStateChangedEventArgs(
                header,
                _navigationPage.NavigationStack.Count,
                lastAction));
        }
    }
}
