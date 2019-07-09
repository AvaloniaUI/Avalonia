// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
using Avalonia.ReactiveUI;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class AvaloniaActivationForViewFetcherTest
    {
        public class TestUserControl : UserControl, IActivatable { }

        public class TestUserControlWithWhenActivated : UserControl, IActivatable
        {
            public bool Active { get; private set; }

            public TestUserControlWithWhenActivated()
            {
                this.WhenActivated(disposables => 
                {
                    Active = true;
                    Disposable
                        .Create(() => Active = false)
                        .DisposeWith(disposables);
                });
            }
        }

        public class TestWindowWithWhenActivated : Window, IActivatable
        {
            public bool Active { get; private set; }

            public TestWindowWithWhenActivated()
            {
                this.WhenActivated(disposables => 
                {
                    Active = true;
                    Disposable
                        .Create(() => Active = false)
                        .DisposeWith(disposables);
                });
            }
        }

        public class ActivatableViewModel : ISupportsActivation
        {
            public ViewModelActivator Activator { get; }

            public bool IsActivated { get; private set; }

            public ActivatableViewModel() 
            {
                Activator = new ViewModelActivator();
                this.WhenActivated(disposables => 
                {
                    IsActivated = true;
                    Disposable
                        .Create(() => IsActivated = false)
                        .DisposeWith(disposables);
                });
            }
        }

        public class ActivatableWindow : ReactiveWindow<ActivatableViewModel>
        {
            public ActivatableWindow()
            {
                InitializeComponent();
                Assert.IsType<Border>(Content);
                this.WhenActivated(disposables => { });
            }

            private void InitializeComponent()
            {
                var loader = new AvaloniaXamlLoader();
                loader.Load(@"
<Window xmlns='https://github.com/avaloniaui'>
    <Border/>
</Window>", null, this);
            }
        }

        public class ActivatableUserControl : ReactiveUserControl<ActivatableViewModel>
        {
            public ActivatableUserControl()
            {
                InitializeComponent();
                Assert.IsType<Border>(Content);
                this.WhenActivated(disposables => { });
            }

            private void InitializeComponent()
            {
                var loader = new AvaloniaXamlLoader();
                loader.Load(@"
<UserControl xmlns='https://github.com/avaloniaui'>
    <Border/>
</UserControl>", null, this);
            }
        }

        public AvaloniaActivationForViewFetcherTest()
        {
            Locator.CurrentMutable.RegisterConstant(
                new AvaloniaActivationForViewFetcher(), 
                typeof(IActivationForViewFetcher));
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
            var userControl = new TestUserControlWithWhenActivated();
            Assert.False(userControl.Active);

            var fakeRenderedDecorator = new TestRoot();
            fakeRenderedDecorator.Child = userControl;
            Assert.True(userControl.Active);

            fakeRenderedDecorator.Child = null;
            Assert.False(userControl.Active);
        }

        [Fact]
        public void Activation_For_View_Fetcher_Should_Support_Windows() 
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform)) 
            {
                var window = new TestWindowWithWhenActivated();
                Assert.False(window.Active);

                window.Show();
                Assert.True(window.Active);

                window.Close();
                Assert.False(window.Active);
            }
        }

        [Fact]
        public void Activatable_Window_View_Model_Is_Activated_And_Deactivated() 
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform)) 
            {
                var viewModel = new ActivatableViewModel();
                var window = new ActivatableWindow { ViewModel = viewModel };
                Assert.False(viewModel.IsActivated);

                window.Show();
                Assert.True(viewModel.IsActivated);

                window.Close();
                Assert.False(viewModel.IsActivated);
            }
        }

        [Fact]
        public void Activatable_User_Control_View_Model_Is_Activated_And_Deactivated() 
        {
            var root = new TestRoot();
            var viewModel = new ActivatableViewModel();
            var control = new ActivatableUserControl { ViewModel = viewModel };
            Assert.False(viewModel.IsActivated);

            root.Child = control;
            Assert.True(viewModel.IsActivated);

            root.Child = null;
            Assert.False(viewModel.IsActivated);
        }
    }
}
