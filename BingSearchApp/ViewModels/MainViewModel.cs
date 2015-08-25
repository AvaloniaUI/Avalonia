namespace BingSearchApp.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using ReactiveUI;

    // ReSharper disable once UnusedMember.Global
    public class MainViewModel : ReactiveObject
    {
        private readonly IWebSearchService searchService;
        private bool isExecuting;
        private string searchText;

        public MainViewModel(IWebSearchService searchService)
        {
            this.searchService = searchService;
            SearchText = string.Empty;

            var searchTextObservable = this.ObservableForProperty(model => model.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.MainThreadScheduler);

            searchTextObservable.Select(async textChange => await Search(textChange.Value))
                .Switch()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(SetSearchResults);
        }

        private async Task<IEnumerable<SearchResult>> Search(string query)
        {
            IsExecuting = true;
            var results = await Task.Factory.StartNew(() => searchService.Search(query));
            IsExecuting = false;
            return results;
        }

        public bool IsExecuting
        {
            get { return isExecuting; }
            set
            {
                this.RaiseAndSetIfChanged(ref isExecuting, value);
            }
        }

        private void SetSearchResults(IEnumerable<SearchResult> results)
        {
            SearchResults = results.Select(r => new SearchResultViewModel(r)).ToList();
        }

        public IEnumerable<SearchResultViewModel> SearchResults
        {
            get { return searchResults; }
            set
            {
                this.RaiseAndSetIfChanged(ref searchResults, value);
            }
        }

        private IEnumerable<SearchResultViewModel> searchResults;

        public string SearchText
        {
            get { return searchText; }
            set
            {
                this.RaiseAndSetIfChanged(ref searchText, value);
            }
        }
    }
}
