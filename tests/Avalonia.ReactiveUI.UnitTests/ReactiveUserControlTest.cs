using System.Reactive.Disposables;
using Avalonia.UnitTests;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ReactiveUserControlTest
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

        [Fact]
        public void Should_Start_With_NotNull_Activated_ViewModel()
        {
            var root = new TestRoot();
            var view = new ExampleView {ViewModel = new ExampleViewModel()};

            Assert.False(view.ViewModel.IsActive);

            root.Child = view;

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.True(view.ViewModel.IsActive);

            root.Child = null;

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.False(view.ViewModel.IsActive);
        }

        [Fact]
        public void Should_Start_With_NotNull_Activated_DataContext()
        {
            var root = new TestRoot();
            var view = new ExampleView {DataContext = new ExampleViewModel()};

            Assert.False(view.ViewModel.IsActive);

            root.Child = view;

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.True(view.ViewModel.IsActive);

            root.Child = null;

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.False(view.ViewModel.IsActive);
        }
    }
}
