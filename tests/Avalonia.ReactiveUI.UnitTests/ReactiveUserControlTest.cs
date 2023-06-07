using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Threading;
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
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.True(view.ViewModel.IsActive);

            root.Child = null;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

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
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.True(view.ViewModel.IsActive);

            root.Child = null;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

            Assert.NotNull(view.ViewModel);
            Assert.NotNull(view.DataContext);
            Assert.False(view.ViewModel.IsActive);
        }
        
        [Fact]
        public void Should_Inherit_DataContext()
        {
            var vm1 = new ExampleViewModel();
            var vm2 = new ExampleViewModel();
            var view = new ExampleView();
            var root = new TestRoot(view);
            
            Assert.Null(view.DataContext);
            Assert.Null(view.ViewModel);
            
            root.DataContext = vm1;
            
            Assert.Same(vm1, view.DataContext);
            Assert.Same(vm1, view.ViewModel);
            
            root.DataContext = null;
            
            Assert.Null(view.DataContext);
            Assert.Null(view.ViewModel);
            
            root.DataContext = vm2;
            
            Assert.Same(vm2, view.DataContext);
            Assert.Same(vm2, view.ViewModel);
        }

        [Fact]
        public void Should_Not_Overlap_Change_Notifications()
        {
            var vm1 = new ExampleViewModel();
            var vm2 = new ExampleViewModel();

            var view1 = new ExampleView();
            var view2 = new ExampleView();
            
            Assert.Null(view1.DataContext);
            Assert.Null(view2.DataContext);
            Assert.Null(view1.ViewModel);
            Assert.Null(view2.ViewModel);

            view1.DataContext = vm1;
            
            Assert.Same(vm1, view1.DataContext);
            Assert.Same(vm1, view1.ViewModel);
            Assert.Null(view2.DataContext);
            Assert.Null(view2.ViewModel);

            view2.DataContext = vm2;

            Assert.Same(vm1, view1.DataContext);
            Assert.Same(vm1, view1.ViewModel);
            Assert.Same(vm2, view2.DataContext);
            Assert.Same(vm2, view2.ViewModel);

            view1.ViewModel = null;

            Assert.Null(view1.DataContext);
            Assert.Null(view1.ViewModel);
            Assert.Same(vm2, view2.DataContext);
            Assert.Same(vm2, view2.ViewModel);

            view2.ViewModel = null;

            Assert.Null(view1.DataContext);
            Assert.Null(view2.DataContext);
            Assert.Null(view1.ViewModel);
            Assert.Null(view2.ViewModel);
        }
    }
}
