using ReactiveUI;

namespace ReactiveUIDemo.ViewModels
{
    internal class BarViewModel : ReactiveObject, IRoutableViewModel
    {
        public BarViewModel(IScreen screen) => HostScreen = screen;
        public string UrlPathSegment => "Bar";
        public IScreen HostScreen { get; }
    }
}
