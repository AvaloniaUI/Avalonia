namespace BingSearchApp.ViewModels
{
    using ReactiveUI;

    public class AppKeyViewModel : ReactiveObject
    {
        public AppKeyViewModel(string currentKey)
        {
            AppKey = currentKey;
        }

        public string AppKey { get; set; }
    }
}