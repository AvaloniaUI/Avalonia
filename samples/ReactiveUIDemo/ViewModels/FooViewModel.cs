using ReactiveUI;

namespace ReactiveUIDemo.ViewModels
{
    internal class FooViewModel : ReactiveObject, IRoutableViewModel
    {
        public FooViewModel(IScreen screen) => HostScreen = screen;
        public string UrlPathSegment => "Foo";
        public IScreen HostScreen { get; }
    }
}
