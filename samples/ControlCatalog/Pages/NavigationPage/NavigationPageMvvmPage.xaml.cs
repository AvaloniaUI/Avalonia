using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageMvvmPage : UserControl
    {
        private readonly NavigationPageMvvmShellViewModel _viewModel;
        private bool _initialized;

        public NavigationPageMvvmPage()
        {
            InitializeComponent();

            ISamplePageFactory pageFactory = new SamplePageFactory();
            ISampleNavigationService navigationService = new SampleNavigationService(DemoNav, pageFactory);
            _viewModel = new NavigationPageMvvmShellViewModel(navigationService);
            DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;
            await _viewModel.InitializeAsync();
        }
    }
}
