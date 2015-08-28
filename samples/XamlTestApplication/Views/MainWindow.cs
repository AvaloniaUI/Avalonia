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
            this.InitializeComponent();

            DevTools.Attach(this);
        }

        private void InitializeComponent()
        {
            var loader = new PerspexXamlLoader(new PerspexInflatableTypeFactory());
            loader.Load(this.GetType());
        }
    }
}