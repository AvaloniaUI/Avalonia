using Avalonia.UnitTests;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ReactiveUserControlTest
    {
        public class ExampleViewModel : ReactiveObject { }

        public class ExampleView : ReactiveUserControl<ExampleViewModel> { }

        public ReactiveUserControlTest() =>
            Locator
                .CurrentMutable
                .RegisterConstant(
                    new AvaloniaActivationForViewFetcher(),
                    typeof(IActivationForViewFetcher));

        [Fact]
        public void Data_Context_Should_Stay_In_Sync_With_Reactive_User_Control_View_Model() 
        {
            var root = new TestRoot();
            var view = new ExampleView();
            root.Child = view;

            var viewModel = new ExampleViewModel();
            Assert.Null(view.ViewModel);

            view.DataContext = viewModel;
            Assert.Equal(view.ViewModel, viewModel);
            Assert.Equal(view.DataContext, viewModel);

            view.DataContext = null;
            Assert.Null(view.ViewModel);
            Assert.Null(view.DataContext);

            view.ViewModel = viewModel;
            Assert.Equal(viewModel, view.ViewModel);
            Assert.Equal(viewModel, view.DataContext);

            view.ViewModel = null;
            Assert.Null(view.ViewModel);
            Assert.Null(view.DataContext);
        }
    }
}
