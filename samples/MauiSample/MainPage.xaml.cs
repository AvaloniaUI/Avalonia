using ControlCatalog;

namespace MauiSample
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public string Label => "This is a bound label";

        public object View { get; set; }

        public MainPage()
        {
            View = new MainView();
            BindingContext = this;
            InitializeComponent();
        }
    }
}
