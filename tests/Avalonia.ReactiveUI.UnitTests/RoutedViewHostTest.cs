using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia;
using ReactiveUI;
using DynamicData;
using Xunit;
using Splat;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Reactive;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class RoutedViewHostTest
    {
        public class FirstRoutableViewModel : ReactiveObject, IRoutableViewModel
        {
            public string UrlPathSegment => "first";

            public IScreen HostScreen { get; set; }
        }

        public class FirstRoutableView : ReactiveUserControl<FirstRoutableViewModel> { }

        public class AlternativeFirstRoutableView : ReactiveUserControl<FirstRoutableViewModel> { }

        public class SecondRoutableViewModel : ReactiveObject, IRoutableViewModel
        {
            public string UrlPathSegment => "second";

            public IScreen HostScreen { get; set; }
        }

        public class SecondRoutableView : ReactiveUserControl<SecondRoutableViewModel> { }

        public class AlternativeSecondRoutableView : ReactiveUserControl<SecondRoutableViewModel> { }

        public class ScreenViewModel : ReactiveObject, IScreen
        {
            public RoutingState Router { get; } = new RoutingState();
        }

        public static string AlternativeViewContract => "AlternativeView";

        public RoutedViewHostTest()
        {
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.Register(() => new FirstRoutableView(), typeof(IViewFor<FirstRoutableViewModel>));
            Locator.CurrentMutable.Register(() => new SecondRoutableView(), typeof(IViewFor<SecondRoutableViewModel>));
            Locator.CurrentMutable.Register(() => new AlternativeFirstRoutableView(), typeof(IViewFor<FirstRoutableViewModel>), AlternativeViewContract);
            Locator.CurrentMutable.Register(() => new AlternativeSecondRoutableView(), typeof(IViewFor<SecondRoutableViewModel>), AlternativeViewContract);
        }

        [Fact]
        public void RoutedViewHost_Should_Stay_In_Sync_With_RoutingState() 
        {
            var screen = new ScreenViewModel();
            var defaultContent = new TextBlock();
            var host = new RoutedViewHost 
            { 
                Router = screen.Router,
                DefaultContent = defaultContent,
                PageTransition = null
            };

            var root = new TestRoot
            {
                Child = host
            };
            
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.NotNull(host.Content);
            Assert.IsType<TextBlock>(host.Content);
            Assert.Equal(defaultContent, host.Content);

            var first = new FirstRoutableViewModel();
            screen.Router.Navigate.Execute(first).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<FirstRoutableView>(host.Content);
            Assert.Equal(first, ((FirstRoutableView)host.Content).DataContext);
            Assert.Equal(first, ((FirstRoutableView)host.Content).ViewModel);

            var second = new SecondRoutableViewModel();
            screen.Router.Navigate.Execute(second).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<SecondRoutableView>(host.Content);
            Assert.Equal(second, ((SecondRoutableView)host.Content).DataContext);
            Assert.Equal(second, ((SecondRoutableView)host.Content).ViewModel);

            screen.Router.NavigateBack.Execute(Unit.Default).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<FirstRoutableView>(host.Content);
            Assert.Equal(first, ((FirstRoutableView)host.Content).DataContext);
            Assert.Equal(first, ((FirstRoutableView)host.Content).ViewModel);

            screen.Router.NavigateBack.Execute(Unit.Default).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<TextBlock>(host.Content);
            Assert.Equal(defaultContent, host.Content);
        }

        [Fact]
        public void RoutedViewHost_Should_Stay_In_Sync_With_RoutingState_And_Contract()
        {
            var screen = new ScreenViewModel();
            var defaultContent = new TextBlock();
            var host = new RoutedViewHost
            {
                Router = screen.Router,
                DefaultContent = defaultContent,
                PageTransition = null
            };

            var root = new TestRoot
            {
                Child = host
            };

            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.NotNull(host.Content);
            Assert.IsType<TextBlock>(host.Content);
            Assert.Equal(defaultContent, host.Content);

            var first = new FirstRoutableViewModel();
            screen.Router.Navigate.Execute(first).Subscribe();

            host.ViewContract = null;
            Assert.NotNull(host.Content);
            Assert.IsType<FirstRoutableView>(host.Content);
            Assert.Equal(first, ((FirstRoutableView)host.Content).DataContext);
            Assert.Equal(first, ((FirstRoutableView)host.Content).ViewModel);

            host.ViewContract = AlternativeViewContract;
            Assert.NotNull(host.Content);
            Assert.IsType<AlternativeFirstRoutableView>(host.Content);
            Assert.Equal(first, ((AlternativeFirstRoutableView)host.Content).DataContext);
            Assert.Equal(first, ((AlternativeFirstRoutableView)host.Content).ViewModel);

            var second = new SecondRoutableViewModel();
            screen.Router.Navigate.Execute(second).Subscribe();

            host.ViewContract = null;
            Assert.NotNull(host.Content);
            Assert.IsType<SecondRoutableView>(host.Content);
            Assert.Equal(second, ((SecondRoutableView)host.Content).DataContext);
            Assert.Equal(second, ((SecondRoutableView)host.Content).ViewModel);

            host.ViewContract = AlternativeViewContract;
            Assert.NotNull(host.Content);
            Assert.IsType<AlternativeSecondRoutableView>(host.Content);
            Assert.Equal(second, ((AlternativeSecondRoutableView)host.Content).DataContext);
            Assert.Equal(second, ((AlternativeSecondRoutableView)host.Content).ViewModel);

            screen.Router.NavigateBack.Execute(Unit.Default).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<AlternativeFirstRoutableView>(host.Content);
            Assert.Equal(first, ((AlternativeFirstRoutableView)host.Content).DataContext);
            Assert.Equal(first, ((AlternativeFirstRoutableView)host.Content).ViewModel);

            screen.Router.NavigateBack.Execute(Unit.Default).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<TextBlock>(host.Content);
            Assert.Equal(defaultContent, host.Content);
        }

        [Fact]
        public void RoutedViewHost_Should_Show_Default_Content_When_Router_Is_Null()
        {
            var screen = new ScreenViewModel();
            var defaultContent = new TextBlock();
            var host = new RoutedViewHost 
            { 
                DefaultContent = defaultContent,
                PageTransition = null,
                Router = null
            };

            var root = new TestRoot
            {
                Child = host
            };

            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.NotNull(host.Content);
            Assert.Equal(defaultContent, host.Content);

            host.Router = screen.Router;

            Assert.NotNull(host.Content);
            Assert.Equal(defaultContent, host.Content);
            
            var first = new FirstRoutableViewModel();
            screen.Router.Navigate.Execute(first).Subscribe();

            Assert.NotNull(host.Content);
            Assert.IsType<FirstRoutableView>(host.Content);

            host.Router = null;
            
            Assert.NotNull(host.Content);
            Assert.Equal(defaultContent, host.Content);

            host.Router = screen.Router;
            
            Assert.NotNull(host.Content);
            Assert.IsType<FirstRoutableView>(host.Content);
        }
    }
}
