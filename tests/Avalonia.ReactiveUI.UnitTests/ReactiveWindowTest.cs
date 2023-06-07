using System.Reactive.Disposables;
using Avalonia.Threading;
using Avalonia.UnitTests;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ReactiveWindowTest
    {
        public class ExampleViewModel : ReactiveObject, IActivatableViewModel
        {
            public bool IsActive { get; private set; }

            public ViewModelActivator Activator { get; } = new ViewModelActivator();

            public ExampleViewModel() => this.WhenActivated(disposables =>
            {
                IsActive = true;
                Disposable
                    .Create(() => IsActive = false)
                    .DisposeWith(disposables);
            });
        }

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

        [Fact]
        public void Should_Start_With_NotNull_Activated_ViewModel()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var view = new ExampleWindow { ViewModel = new ExampleViewModel() };

                Assert.False(view.ViewModel.IsActive);

                view.Show();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                Assert.NotNull(view.ViewModel);
                Assert.NotNull(view.DataContext);
                Assert.True(view.ViewModel.IsActive);

                view.Close();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                Assert.NotNull(view.ViewModel);
                Assert.NotNull(view.DataContext);
                Assert.False(view.ViewModel.IsActive);
            }
        }

        [Fact]
        public void Should_Start_With_NotNull_Activated_DataContext()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var view = new ExampleWindow { DataContext = new ExampleViewModel() };

                Assert.False(view.ViewModel.IsActive);

                view.Show();

                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
                Assert.NotNull(view.ViewModel);
                Assert.NotNull(view.DataContext);
                Assert.True(view.ViewModel.IsActive);

                view.Close();

                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
                Assert.NotNull(view.ViewModel);
                Assert.NotNull(view.DataContext);
                Assert.False(view.ViewModel.IsActive);
            }
        }
    }
}
