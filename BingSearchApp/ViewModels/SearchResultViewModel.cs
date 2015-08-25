namespace BingSearchApp.ViewModels
{
    using System;
    using System.Diagnostics;
    using ReactiveUI;

    public class SearchResultViewModel : ReactiveObject
    {
        private readonly SearchResult searchResult;

        public SearchResultViewModel(SearchResult searchResult)
        {
            this.searchResult = searchResult;

            OpenUrlCommand = ReactiveCommand.Create();
            OpenUrlCommand.Subscribe(_ => OpenUrl(searchResult.Url) );
        }

        private void OpenUrl(Uri url)
        {
            var psi = new ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = url.ToString();
            Process.Start(psi);
        }

        public string Title
        {
            get { return searchResult.Title; }
        }

        public string Description
        {
            get { return searchResult.Description; }
        }

        public Uri Url
        {
            get { return searchResult.Url; }
        }

        public string DisplayUrl
        {
            get { return searchResult.DisplayUrl; }
        }

        public ReactiveCommand<object> OpenUrlCommand { get; private set; }
    }
}