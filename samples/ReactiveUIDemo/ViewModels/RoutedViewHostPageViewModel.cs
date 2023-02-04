using ReactiveUI;

namespace ReactiveUIDemo.ViewModels
{
    internal class RoutedViewHostPageViewModel : ReactiveObject, IScreen
    {
        public RoutedViewHostPageViewModel()
        {
            Foo = new(this);
            Bar = new(this);
            Router.Navigate.Execute(Foo);
        }

        public RoutingState Router { get; } = new();
        public FooViewModel Foo { get; }
        public BarViewModel Bar { get; }

        public void ShowFoo() => Router.Navigate.Execute(Foo);
        public void ShowBar() => Router.Navigate.Execute(Bar);
    }
}
