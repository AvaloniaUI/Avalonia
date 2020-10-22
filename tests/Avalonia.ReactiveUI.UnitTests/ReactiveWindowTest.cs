using Avalonia.UnitTests;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ReactiveWindowTest
    {
        public class ExampleViewModel : ReactiveObject { }

        public class ExampleWindow : ReactiveWindow<ExampleViewModel> { }

        public ReactiveWindowTest() =>
            Locator
                .CurrentMutable
                .RegisterConstant(
                    new AvaloniaActivationForViewFetcher(),
                    typeof(IActivationForViewFetcher));

        [Fact]
        public void Data_Context_Should_Stay_In_Sync_With_Reactive_Window_View_Model() 
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var view = new ExampleWindow();
                var viewModel = new ExampleViewModel();
                view.Show();

                Assert.Null(view.ViewModel);
                Assert.Null(view.DataContext);

                view.DataContext = viewModel;
                Assert.Equal(viewModel, view.ViewModel);
                Assert.Equal(viewModel, view.DataContext);

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
}
