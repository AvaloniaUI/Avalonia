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

namespace Avalonia 
{
    public class AvaloniaActivationForViewFetcherTest
    {
        public class TestUserControl : UserControl, IActivatable { }

        public class TestUserControlWithWhenActivated : UserControl, IActivatable
        {
            public bool Active { get; private set; }

            public TestUserControlWithWhenActivated()
            {
                this.WhenActivated(disposables => {
                    Active = true;
                    Disposable
                        .Create(() => Active = false)
                        .DisposeWith(disposables);
                });
            }
        }

        [Fact]
        public void Visual_Element_Is_Activated_And_Deactivated()
        {
            var userControl = new TestUserControl();
            var activationForViewFetcher = new AvaloniaActivationForViewFetcher();

            activationForViewFetcher
                .GetActivationForView(userControl)
                .ToObservableChangeSet(scheduler: ImmediateScheduler.Instance)
                .Bind(out var activated)
                .Subscribe();

            var fakeRenderedDecorator = new TestRoot();
            fakeRenderedDecorator.Child = userControl;
            Assert.True(activated[0]);
            Assert.Equal(1, activated.Count);

            fakeRenderedDecorator.Child = null;
            Assert.True(activated[0]);
            Assert.False(activated[1]);
            Assert.Equal(2, activated.Count);
        }

        [Fact]
        public void Get_Affinity_For_View_Should_Return_Non_Zero_For_Visual_Elements() 
        {
            var userControl = new TestUserControl();
            var activationForViewFetcher = new AvaloniaActivationForViewFetcher();

            var forUserControl = activationForViewFetcher.GetAffinityForView(userControl.GetType());
            var forNonUserControl = activationForViewFetcher.GetAffinityForView(typeof(object));

            Assert.NotEqual(0, forUserControl);
            Assert.Equal(0, forNonUserControl);
        }

        [Fact]
        public void Activation_For_View_Fetcher_Should_Support_When_Activated()
        {
            Locator.CurrentMutable.RegisterConstant(
                new AvaloniaActivationForViewFetcher(), 
                typeof(IActivationForViewFetcher));

            var userControl = new TestUserControlWithWhenActivated();
            Assert.False(userControl.Active);

            var fakeRenderedDecorator = new TestRoot();
            fakeRenderedDecorator.Child = userControl;
            Assert.True(userControl.Active);

            fakeRenderedDecorator.Child = null;
            Assert.False(userControl.Active);
        }
    }
}