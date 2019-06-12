// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.UnitTests;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ViewModelViewHostTest
    {
        public class FirstViewModel { }

        public class FirstView : ReactiveUserControl<FirstViewModel> { }

        public class SecondViewModel : ReactiveObject { }

        public class SecondView : ReactiveUserControl<SecondViewModel> { }

        public ViewModelViewHostTest()
        {
            Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
            Locator.CurrentMutable.Register(() => new FirstView(), typeof(IViewFor<FirstViewModel>));
            Locator.CurrentMutable.Register(() => new SecondView(), typeof(IViewFor<SecondViewModel>));
        }

        [Fact]
        public void ViewModelViewHost_View_Should_Stay_In_Sync_With_ViewModel() 
        {
            var defaultContent = new TextBlock();
            var host = new ViewModelViewHost 
            {
                DefaultContent = defaultContent,
                PageTransition = null
            };

            var root = new TestRoot 
            { 
                Child = host 
            };
            
            Assert.NotNull(host.Content);
            Assert.Equal(typeof(TextBlock), host.Content.GetType());
            Assert.Equal(defaultContent, host.Content);

            var first = new FirstViewModel();
            host.ViewModel = first;
            Assert.NotNull(host.Content);
            Assert.Equal(typeof(FirstView), host.Content.GetType());
            Assert.Equal(first, ((FirstView)host.Content).DataContext);
            Assert.Equal(first, ((FirstView)host.Content).ViewModel);

            var second = new SecondViewModel();
            host.ViewModel = second;
            Assert.NotNull(host.Content);
            Assert.Equal(typeof(SecondView), host.Content.GetType());
            Assert.Equal(second, ((SecondView)host.Content).DataContext);
            Assert.Equal(second, ((SecondView)host.Content).ViewModel);

            host.ViewModel = null;
            Assert.NotNull(host.Content);
            Assert.Equal(typeof(TextBlock), host.Content.GetType());
            Assert.Equal(defaultContent, host.Content);

            host.ViewModel = first;
            Assert.NotNull(host.Content);
            Assert.Equal(typeof(FirstView), host.Content.GetType());
            Assert.Equal(first, ((FirstView)host.Content).DataContext);
            Assert.Equal(first, ((FirstView)host.Content).ViewModel);
        }
    }
}