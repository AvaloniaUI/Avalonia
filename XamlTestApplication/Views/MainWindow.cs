namespace XamlTestApplication.Views
{
    using OmniXaml.AppServices.Mvvm;
    using Perspex.Diagnostics;
    using Perspex.Xaml.Desktop;

    [ViewToken("Main", typeof(MainWindow))]
    public class MainWindow : PerspexWindow
    {
        public MainWindow()
        {
            DevTools.Attach(this);
        }
    }
}