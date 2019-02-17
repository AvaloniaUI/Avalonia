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

namespace Avalonia
{
    public class RoutedViewHostTest
    {
        public class FirstRoutableViewModel : ReactiveObject, IRoutableViewModel
        {
            public string UrlPathSegment => "first";

            public IScreen HostScreen { get; set; }
        }

        public class FirstRoutableView : ReactiveUserControl<FirstRoutableViewModel> { }

        public class SecondRoutableViewModel : ReactiveObject, IRoutableViewModel
        {
            public string UrlPathSegment => "second";

            public IScreen HostScreen { get; set; }
        }

        public class SecondRoutableView : ReactiveUserControl<SecondRoutableViewModel> { }

        public class ScreenViewModel : ReactiveObject, IScreen
        {
            public RoutingState Router { get; } = new RoutingState();
        }

        public RoutedViewHostTest()
        {
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.Register(() => new FirstRoutableView(), typeof(IViewFor<FirstRoutableViewModel>));
            Locator.CurrentMutable.Register(() => new SecondRoutableView(), typeof(IViewFor<SecondRoutableViewModel>));
        }

        [Fact]
        public void RoutedViewHostShouldStayInSyncWithRoutingState() 
        {
            var screen = new ScreenViewModel();
            var defaultContent = new TextBlock();
            var host = new RoutedViewHost 
            { 
                Router = screen.Router,
                DefaultContent = defaultContent,
                FadeOutAnimation = null,
                FadeInAnimation = null
            };

            var root = new TestRoot 
            { 
                Child = host 
            };
            
            Assert.NotNull(host.Content);
            Assert.Equal(typeof(TextBlock), host.Content.GetType());
            Assert.Equal(defaultContent, host.Content);

            screen.Router.Navigate
                .Execute(new FirstRoutableViewModel())
                .Subscribe();

            Assert.NotNull(host.Content);
            Assert.Equal(typeof(FirstRoutableView), host.Content.GetType());

            screen.Router.Navigate
                .Execute(new SecondRoutableViewModel())
                .Subscribe();

            Assert.NotNull(host.Content);
            Assert.Equal(typeof(SecondRoutableView), host.Content.GetType());

            screen.Router.NavigateBack
                .Execute(Unit.Default)
                .Subscribe();

            Assert.NotNull(host.Content);
            Assert.Equal(typeof(FirstRoutableView), host.Content.GetType());

            screen.Router.NavigateBack
                .Execute(Unit.Default)
                .Subscribe();

            Assert.NotNull(host.Content);
            Assert.Equal(typeof(TextBlock), host.Content.GetType());
            Assert.Equal(defaultContent, host.Content);
        }
    }
}