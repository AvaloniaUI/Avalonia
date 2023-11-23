using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class TransitioningContentControlTests
    {
        [Fact]
        public void Transition_Should_Not_Be_Run_When_First_Shown()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("foo");

            Assert.Equal(0, transition.StartCount);
        }

        [Fact]
        public void ContentPresenters2_Should_Initially_Be_Hidden()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("foo");
            var presenter2 = GetContentPresenters2(target);

            Assert.False(presenter2.IsVisible);
        }

        [Fact]
        public void Transition_Should_Be_Run_On_Layout()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("foo");

            target.Content = "bar";
            Assert.Equal(0, transition.StartCount);

            Layout(target);
            Assert.Equal(1, transition.StartCount);
        }

        [Fact]
        public void Control_Transition_Should_Be_Run_On_Layout()
        {
            using var app = Start();
            var (target, transition) = CreateTarget(new Button());

            target.Content = new Canvas();
            Assert.Equal(0, transition.StartCount);

            Layout(target);
            Assert.Equal(1, transition.StartCount);
        }

        [Fact]
        public void Control_Should_Connect_To_VisualTree_Once()
        {
            using var app = Start();
            var (target, transition) = CreateTarget(new Control());

            var control = new Control();
            int counter = 0;

            control.AttachedToVisualTree += (s,e) => counter++;

            target.Content = control;
            Layout(target);
            target.Content = new Control();
            Layout(target);
            
            Assert.Equal(1, counter);
        }

        [Fact]
        public void ContentPresenters2_Should_Be_Setup()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("foo");
            var presenter1 = target.Presenter!;
            var presenter2 = GetContentPresenters2(target);

            target.Content = "bar";
            Layout(target);

            Assert.True(presenter2.IsVisible);
            Assert.Equal("foo", presenter1.Content);
            Assert.Equal("bar", presenter2.Content);
        }

        [Fact]
        public void Old_Presenter_Should_Be_Hidden_When_Transition_Completes()
        {
            using var app = Start();
            using var sync = UnitTestSynchronizationContext.Begin();
            var (target, transition) = CreateTarget("foo");
            var presenter1 = target.Presenter!;
            var presenter2 = GetContentPresenters2(target);

            target.Content = "bar";
            Layout(target);
            Assert.True(presenter1.IsVisible);
            Assert.True(presenter2.IsVisible);

            transition.Complete();
            sync.ExecutePostedCallbacks();
            Assert.True(presenter2.IsVisible);
            Assert.False(presenter1.IsVisible);

            target.Content = "foo";
            Layout(target);
            Assert.True(presenter1.IsVisible);
            Assert.True(presenter2.IsVisible);

            transition.Complete();
            sync.ExecutePostedCallbacks();
            Assert.True(presenter1.IsVisible);
            Assert.False(presenter2.IsVisible);
        }

        [Fact]
        public void Transition_Should_Be_Canceled_If_Content_Changes_While_Running()
        {
            using var app = Start();
            using var sync = UnitTestSynchronizationContext.Begin();
            var (target, transition) = CreateTarget("foo");

            target.Content = "bar";
            Layout(target);
            target.Content = "baz";

            Assert.Equal(0, transition.CancelCount);

            Layout(target);

            Assert.Equal(1, transition.CancelCount);
        }

        [Fact]
        public void New_Transition_Should_Be_Started_If_Content_Changes_While_Running()
        {
            using var app = Start();
            using var sync = UnitTestSynchronizationContext.Begin();
            var (target, transition) = CreateTarget("foo");
            var presenter2 = GetContentPresenters2(target);

            target.Content = "bar";
            Layout(target);

            target.Content = "baz";

            var startedRaised = 0;

            transition.Started += (from, to, forward) =>
            {
                var fromPresenter = Assert.IsType<ContentPresenter>(from);
                var toPresenter = Assert.IsType<ContentPresenter>(to);

                Assert.Same(presenter2, fromPresenter);
                Assert.Same(target.Presenter, toPresenter);
                Assert.Equal("bar", fromPresenter.Content);
                Assert.Equal("baz", toPresenter.Content);
                Assert.True(forward);
                Assert.Equal(1, transition.CancelCount);

                ++startedRaised;
            };

            Layout(target);
            sync.ExecutePostedCallbacks();

            Assert.Equal(1, startedRaised);
            Assert.Equal("baz", target.Presenter!.Content);
            Assert.Equal("bar", presenter2.Content);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Transition_Should_Be_Reversed_If_Property_Is_Set(bool reversed)
        {
            using var app = Start();
            using var sync = UnitTestSynchronizationContext.Begin();
            var (target, transition) = CreateTarget("foo");
            var presenter2 = GetContentPresenters2(target);

            target.IsTransitionReversed = reversed;

            target.Content = "bar";

            var startedRaised = 0;

            transition.Started += (from, to, forward) =>
            {
                Assert.Equal(reversed, !forward);

                ++startedRaised;
            };

            Layout(target);
            sync.ExecutePostedCallbacks();

            Assert.Equal(1, startedRaised);
            Assert.Equal("foo", target.Presenter!.Content);
            Assert.Equal("bar", presenter2.Content);
        }

        [Fact]
        public void Logical_Children_Should_Not_Be_Duplicated()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("");
            target.PageTransition = null;

            var childControl = new Control();
            target.Content = childControl;

            Assert.Equal(1, target.LogicalChildren.Count);
            Assert.Equal(target.LogicalChildren[0], childControl);
        }

        [Fact]
        public void First_Presenter_Should_Register_TCC_As_His_Host()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("");
            target.PageTransition = null;

            var childControl = new Control();
            target.Presenter!.Content = childControl;

            Assert.Equal(1, target.LogicalChildren.Count);
            Assert.Equal(target.LogicalChildren[0], childControl);
        }

        [Fact]
        public void Old_Content_Should_Be_Null_When_New_Content_Is_Old_one()
        {
            using var app = Start();
            var (target, transition) = CreateTarget("");
            var presenter2 = GetContentPresenters2(target);
            target.PageTransition = null;

            var childControl = new Control();
            target.Presenter!.Content = childControl;

            const string fakePage1 = "fakePage1";
            const string fakePage2 = "fakePage2";

            target.Presenter!.Content = fakePage1;
            target.Presenter!.Content = fakePage2;
            target.Presenter!.Content = fakePage1;

            Assert.Equal(fakePage1, target.Presenter!.Content);
            Assert.Equal(null, presenter2.Content);
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(
                TestServices.MockThreadingInterface.With(
                    fontManagerImpl: new HeadlessFontManagerStub(),
                    renderInterface: new HeadlessPlatformRenderInterface(),
                    textShaperImpl: new HeadlessTextShaperStub()));
        }

        private static (TransitioningContentControl, TestTransition) CreateTarget(object content)
        {
            var transition = new TestTransition();
            var target = new TransitioningContentControl
            {
                Content = content,
                PageTransition = transition,
                Template = CreateTemplate(),
            }; 

            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (target, transition);
        }

        private static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate((x, ns) =>
            {
                return new Panel
                {
                    Children =
                    {
                        new ContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        },
                        new ContentPresenter
                        {
                            Name = "PART_ContentPresenter2",
                        },
                    }
                };
            });
        }

        private static ContentPresenter GetContentPresenters2(TransitioningContentControl target)
        {
            return Assert.IsType<ContentPresenter>(target
                .GetTemplateChildren()
                .First(x => x.Name == "PART_ContentPresenter2"));
        }

        private void Layout(Control c)
        {
            (c.GetVisualRoot() as ILayoutRoot)?.LayoutManager.ExecuteLayoutPass();
        }

        private class TestTransition : IPageTransition
        {
            private TaskCompletionSource? _tcs;

            public int StartCount { get; private set; }
            public int FinishCount { get; private set; }
            public int CancelCount { get; private set; }

            public event Action<Visual?, Visual?, bool>? Started;

            public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
            {
                ++StartCount;
                Started?.Invoke(from, to, forward);
                if (_tcs is not null)
                    throw new InvalidOperationException("Transition already running");
                _tcs = new TaskCompletionSource();
                cancellationToken.Register(() => _tcs?.TrySetResult());
                await _tcs.Task;
                _tcs = null;

                if (!cancellationToken.IsCancellationRequested)
                    ++FinishCount;
                else
                    ++CancelCount;
            }

            public void Complete() => _tcs!.TrySetResult();
        }
    }
}
