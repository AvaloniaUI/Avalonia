using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class VirtualizingCarouselPanelTests : ScopedTestBase
    {
        [Fact]
        public void Initial_Item_Is_Displayed()
        {
            using var app = Start();
            var items = new[] { "foo", "bar" };
            var (target, _) = CreateTarget(items);

            Assert.Single(target.Children);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);
            Assert.Equal("foo", container.Content);
        }

        [Fact]
        public void Displays_Next_Item()
        {
            using var app = Start();
            var items = new[] { "foo", "bar" };
            var (target, carousel) = CreateTarget(items);

            carousel.SelectedIndex = 1;
            Layout(target);

            Assert.Single(target.Children);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);
            Assert.Equal("bar", container.Content);
        }

        [Fact]
        public void Handles_Inserted_Item()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, carousel) = CreateTarget(items);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);

            items.Insert(0, "baz");
            Layout(target);

            Assert.Single(target.Children);
            Assert.Same(container, target.Children[0]);
            Assert.Equal("foo", container.Content);
        }

        [Fact]
        public void Handles_Removed_Item()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, carousel) = CreateTarget(items);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);

            items.RemoveAt(0);
            Layout(target);

            Assert.Single(target.Children);
            Assert.Same(container, target.Children[0]);
            Assert.Equal("bar", container.Content);
        }

        [Fact]
        public void Handles_Replaced_Item()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, carousel) = CreateTarget(items);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);

            items[0] = "baz";
            Layout(target);

            Assert.Single(target.Children);
            Assert.Same(container, target.Children[0]);
            Assert.Equal("baz", container.Content);
        }

        [Fact]
        public void Handles_Moved_Item()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, carousel) = CreateTarget(items);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);

            items.Move(0, 1);
            Layout(target);

            Assert.Single(target.Children);
            Assert.Same(container, target.Children[0]);
            Assert.Equal("bar", container.Content);
        }

        [Fact]
        public void Handles_Moved_Item_Range()
        {
            using var app = Start();
            AvaloniaList<string> items = ["foo", "bar", "baz", "qux", "quux"];
            var (target, carousel) = CreateTarget(items);
            var container = Assert.IsType<ContentPresenter>(target.Children[0]);

            carousel.SelectedIndex = 3;
            Layout(target);
            items.MoveRange(0, 2, 4);
            Layout(target);

            Assert.Multiple(() =>
            {
                Assert.Single(target.Children);
                Assert.Same(container, target.Children[0]);
                Assert.Equal("qux", container.Content);
                Assert.Equal(1, carousel.SelectedIndex);
            });
        }

        [Fact]
        public void ViewportFraction_Centers_Selected_Item_And_Peeks_Neighbors()
        {
            using var app = Start();
            var items = new[] { "foo", "bar", "baz" };
            var (target, _) = CreateTarget(items, viewportFraction: 0.8, clientSize: new Size(400, 300));

            var realized = target.GetRealizedContainers()!
                .OfType<ContentPresenter>()
                .ToDictionary(x => (string)x.Content!);

            Assert.Equal(2, realized.Count);
            Assert.Equal(40d, realized["foo"].Bounds.X, 6);
            Assert.Equal(320d, realized["foo"].Bounds.Width, 6);
            Assert.Equal(360d, realized["bar"].Bounds.X, 6);
        }

        [Fact]
        public void ViewportFraction_OneThird_Shows_Three_Full_Items()
        {
            using var app = Start();
            var items = new[] { "foo", "bar", "baz", "qux" };
            var (target, carousel) = CreateTarget(items, viewportFraction: 1d / 3d, clientSize: new Size(300, 120));

            carousel.SelectedIndex = 1;
            Layout(target);

            var realized = target.GetRealizedContainers()!
                .OfType<ContentPresenter>()
                .ToDictionary(x => (string)x.Content!);

            Assert.Equal(3, realized.Count);
            Assert.Equal(0d, realized["foo"].Bounds.X, 6);
            Assert.Equal(100d, realized["bar"].Bounds.X, 6);
            Assert.Equal(200d, realized["baz"].Bounds.X, 6);
            Assert.Equal(100d, realized["bar"].Bounds.Width, 6);
        }

        [Fact]
        public void Changing_SelectedIndex_Repositions_Fractional_Viewport()
        {
            using var app = Start();
            var items = new[] { "foo", "bar", "baz" };
            var (target, carousel) = CreateTarget(items, viewportFraction: 0.8, clientSize: new Size(400, 300));

            carousel.SelectedIndex = 1;
            Layout(target);

            var realized = target.GetRealizedContainers()!
                .OfType<ContentPresenter>()
                .ToDictionary(x => (string)x.Content!);

            Assert.Equal(40d, realized["bar"].Bounds.X, 6);
            Assert.Equal(-280d, realized["foo"].Bounds.X, 6);
        }

        [Fact]
        public void Changing_ViewportFraction_Does_Not_Change_Selected_Item()
        {
            using var app = Start();
            var items = new[] { "foo", "bar", "baz" };
            var (target, carousel) = CreateTarget(items, viewportFraction: 0.72, clientSize: new Size(400, 300));

            carousel.WrapSelection = true;
            carousel.SelectedIndex = 2;
            Layout(target);

            carousel.ViewportFraction = 1d;
            Layout(target);

            var visible = target.Children
                .OfType<ContentPresenter>()
                .Where(x => x.IsVisible)
                .ToList();

            Assert.Single(visible);
            Assert.Equal("baz", visible[0].Content);
            Assert.Equal(2, carousel.SelectedIndex);
        }

        public class Transitions : ScopedTestBase
        {
            [Fact]
            public void Initial_Item_Does_Not_Start_Transition()
            {
                using var app = Start();
                var items = new Control[] { new Button(), new Canvas() };
                var transition = new Mock<IPageTransition>();
                var (target, _) = CreateTarget(items, transition.Object);

                transition.Verify(x => x.Start(
                        It.IsAny<Visual>(),
                        It.IsAny<Visual>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Fact]
            public void Changing_SelectedIndex_Starts_Transition()
            {
                using var app = Start();
                var items = new Control[] { new Button(), new Canvas() };
                var transition = new Mock<IPageTransition>();
                var (target, carousel) = CreateTarget(items, transition.Object);

                carousel.SelectedIndex = 1;
                Layout(target);

                transition.Verify(x => x.Start(
                        items[0],
                        items[1],
                        true,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Fact]
            public void Changing_SelectedIndex_transitions_forward_cycle()
            {
                using var app = Start();
                Dispatcher.UIThread.Invoke(() => // This sets up a proper sync context
                {
                    var items = new Control[] { new Button(), new Canvas(), new Label() };
                    var transition = new Mock<IPageTransition>();
                    var (target, carousel) = CreateTarget(items, transition.Object);
                    var cycleindexes = new[] { 1, 2, 0 };

                    for (int cycleIndex = 0; cycleIndex < cycleindexes.Length; cycleIndex++)
                    {
                        carousel.SelectedIndex = cycleindexes[cycleIndex];
                        Layout(target);
                        
                        Dispatcher.UIThread.RunJobs();

                        var index = cycleIndex;
                        transition.Verify(x => x.Start(
                                index > 0 ? items[cycleindexes[index - 1]] : items[0],
                                items[cycleindexes[index]],
                                true,
                                It.IsAny<CancellationToken>()),
                            Times.Once);
                    }
                });
            }

            [Fact]
            public void Changing_SelectedIndex_transitions_backward_cycle()
            {
                using var app = Start();
                Dispatcher.UIThread.Invoke(() => // This sets up a proper sync context
                {
                    var items = new Control[] { new Button(), new Canvas(), new Label() };
                    var transition = new Mock<IPageTransition>();
                    var (target, carousel) = CreateTarget(items, transition.Object);

                    var cycleindexes = new[] { 2, 1, 0 };

                    for (int cycleIndex = 0; cycleIndex < cycleindexes.Length; cycleIndex++)
                    {
                        carousel.SelectedIndex = cycleindexes[cycleIndex];
                        Layout(target);

                        Dispatcher.UIThread.RunJobs();
                        
                        var index = cycleIndex;
                        transition.Verify(x => x.Start(
                                index > 0 ? items[cycleindexes[index - 1]] : items[0],
                                items[cycleindexes[index]],
                                false,
                                It.IsAny<CancellationToken>()),
                            Times.Once);
                    }
                });
            }

            [Fact]
            public void TransitionFrom_Control_Is_Recycled_When_Transition_Completes()
            {
                using var app = Start();
                using var sync = UnitTestSynchronizationContext.Begin();
                var items = new Control[] { new Button(), new Canvas() };
                var transition = new Mock<IPageTransition>();
                var (target, carousel) = CreateTarget(items, transition.Object);
                var transitionTask = new TaskCompletionSource();

                transition.Setup(x => x.Start(
                        items[0],
                        items[1],
                        true,
                        It.IsAny<CancellationToken>()))
                    .Returns(() => transitionTask.Task);

                carousel.SelectedIndex = 1;
                Layout(target);

                Assert.Equal(items, target.Children);
                Assert.All(items, x => Assert.True(x.IsVisible));

                transitionTask.SetResult();
                sync.ExecutePostedCallbacks();

                Assert.Equal(items, target.Children);
                Assert.False(items[0].IsVisible);
                Assert.True(items[1].IsVisible);
            }

            [Fact]
            public void Existing_Transition_Is_Canceled_If_Interrupted()
            {
                using var app = Start();
                using var sync = UnitTestSynchronizationContext.Begin();
                var items = new Control[] { new Button(), new Canvas() };
                var transition = new Mock<IPageTransition>();
                var (target, carousel) = CreateTarget(items, transition.Object);
                var transitionTask = new TaskCompletionSource();
                CancellationToken? cancelationToken = null;

                transition.Setup(x => x.Start(
                        items[0],
                        items[1],
                        true,
                        It.IsAny<CancellationToken>()))
                    .Callback<Visual, Visual, bool, CancellationToken>((_, _, _, c) => cancelationToken = c)
                    .Returns(() => transitionTask.Task);

                carousel.SelectedIndex = 1;
                Layout(target);

                Assert.NotNull(cancelationToken);
                Assert.False(cancelationToken!.Value.IsCancellationRequested);

                carousel.SelectedIndex = 0;
                Layout(target);

                Assert.True(cancelationToken!.Value.IsCancellationRequested);
            }

            [Fact]
            public void Completed_Transition_Is_Flushed_Before_Starting_Next_Transition()
            {
                using var app = Start();
                using var sync = UnitTestSynchronizationContext.Begin();
                var items = new Control[] { new Button(), new Canvas(), new Label() };
                var transition = new Mock<IPageTransition>();

                transition.Setup(x => x.Start(
                        It.IsAny<Visual>(),
                        It.IsAny<Visual>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var (target, carousel) = CreateTarget(items, transition.Object);

                carousel.SelectedIndex = 1;
                Layout(target);

                carousel.SelectedIndex = 2;
                Layout(target);

                transition.Verify(x => x.Start(
                        items[0],
                        items[1],
                        true,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                transition.Verify(x => x.Start(
                        items[1],
                        items[2],
                        true,
                        It.IsAny<CancellationToken>()),
                    Times.Once);

                sync.ExecutePostedCallbacks();
            }

            [Fact]
            public void Interrupted_Transition_Resets_Current_Page_Before_Starting_Next_Transition()
            {
                using var app = Start();
                var items = new Control[] { new Button(), new Canvas(), new Label() };
                var transition = new DirtyStateTransition();
                var (target, carousel) = CreateTarget(items, transition);

                carousel.SelectedIndex = 1;
                Layout(target);

                carousel.SelectedIndex = 2;
                Layout(target);

                Assert.Equal(2, transition.Starts.Count);
                Assert.Equal(1d, transition.Starts[1].FromOpacity);
                Assert.Null(transition.Starts[1].FromTransform);
            }
        }

        private static IDisposable Start() => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        private static (VirtualizingCarouselPanel, Carousel) CreateTarget(
            IEnumerable items,
            IPageTransition? transition = null,
            double viewportFraction = 1d,
            Size? clientSize = null)
        {
            var size = clientSize ?? new Size(400, 300);
            var carousel = new Carousel
            {
                ItemsSource = items,
                Template = CarouselTemplate(),
                PageTransition = transition,
                ViewportFraction = viewportFraction,
                Width = size.Width,
                Height = size.Height,
            };

            var root = new TestRoot(carousel)
            {
                ClientSize = size,
            };
            root.LayoutManager.ExecuteInitialLayoutPass();
            return ((VirtualizingCarouselPanel)carousel.Presenter!.Panel!, carousel);
        }

        private static IControlTemplate CarouselTemplate()
        {
            return new FuncControlTemplate((c, ns) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty],
                    }.RegisterInNameScope(ns)
                }.RegisterInNameScope(ns));
        }

        private static FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope),
                    }
                });
        }

        private static void Layout(Control c) => c.GetLayoutManager()?.ExecuteLayoutPass();

        private sealed class DirtyStateTransition : IPageTransition
        {
            public List<(double FromOpacity, ITransform? FromTransform)> Starts { get; } = new();

            public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
            {
                Starts.Add((from?.Opacity ?? 1d, from?.RenderTransform));

                if (to is not null)
                {
                    to.Opacity = 0.25;
                    to.RenderTransform = new TranslateTransform { X = 50 };
                }

                return Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        public class WrapSelectionTests : ScopedTestBase
        {
            [Fact]
            public void Next_Wraps_To_First_Item_When_WrapSelection_Enabled()
            {
                using var app = Start();
                var items = new[] { "foo", "bar", "baz" };
                var (target, carousel) = CreateTarget(items);

                carousel.WrapSelection = true;
                carousel.SelectedIndex = 2; // Last item
                Layout(target);

                carousel.Next();
                Layout(target);

                Assert.Equal(0, carousel.SelectedIndex);
            }

            [Fact]
            public void Next_Does_Not_Wrap_When_WrapSelection_Disabled()
            {
                using var app = Start();
                var items = new[] { "foo", "bar", "baz" };
                var (target, carousel) = CreateTarget(items);

                carousel.WrapSelection = false;
                carousel.SelectedIndex = 2; // Last item
                Layout(target);

                carousel.Next();
                Layout(target);

                Assert.Equal(2, carousel.SelectedIndex); // Should stay at last item
            }

            [Fact]
            public void Previous_Wraps_To_Last_Item_When_WrapSelection_Enabled()
            {
                using var app = Start();
                var items = new[] { "foo", "bar", "baz" };
                var (target, carousel) = CreateTarget(items);

                carousel.WrapSelection = true;
                carousel.SelectedIndex = 0; // First item
                Layout(target);

                carousel.Previous();
                Layout(target);

                Assert.Equal(2, carousel.SelectedIndex); // Should wrap to last item
            }

            [Fact]
            public void Previous_Does_Not_Wrap_When_WrapSelection_Disabled()
            {
                using var app = Start();
                var items = new[] { "foo", "bar", "baz" };
                var (target, carousel) = CreateTarget(items);

                carousel.WrapSelection = false;
                carousel.SelectedIndex = 0; // First item
                Layout(target);

                carousel.Previous();
                Layout(target);

                Assert.Equal(0, carousel.SelectedIndex); // Should stay at first item
            }

            [Fact]
            public void WrapSelection_Works_With_Two_Items()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (target, carousel) = CreateTarget(items);

                carousel.WrapSelection = true;
                carousel.SelectedIndex = 1;
                Layout(target);

                carousel.Next();
                Layout(target);

                Assert.Equal(0, carousel.SelectedIndex);

                carousel.Previous();
                Layout(target);

                Assert.Equal(1, carousel.SelectedIndex);
            }

            [Fact]
            public void WrapSelection_Does_Not_Apply_To_Single_Item()
            {
                using var app = Start();
                var items = new[] { "foo" };
                var (target, carousel) = CreateTarget(items);

                carousel.WrapSelection = true;
                carousel.SelectedIndex = 0;
                Layout(target);

                carousel.Next();
                Layout(target);

                Assert.Equal(0, carousel.SelectedIndex);

                carousel.Previous();
                Layout(target);

                Assert.Equal(0, carousel.SelectedIndex);
            }
        }

        public class Gestures : ScopedTestBase
        {
            [Fact]
            public void Swiping_Forward_Realizes_Next_Item()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (panel, carousel) = CreateTarget(items);
                carousel.IsSwipeEnabled = true;

                var e = new SwipeGestureEventArgs(1, new Vector(10, 0), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Equal(2, panel.Children.Count);
                var target = panel.Children[1] as Control;
                Assert.NotNull(target);
                Assert.True(target.IsVisible);
                Assert.Equal("bar", ((target as ContentPresenter)?.Content));
            }

            [Fact]
            public void Swiping_Backward_At_Start_RubberBands_When_WrapSelection_False()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (panel, carousel) = CreateTarget(items);
                carousel.IsSwipeEnabled = true;
                carousel.WrapSelection = false;

                var e = new SwipeGestureEventArgs(1, new Vector(-10, 0), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Single(panel.Children);
            }

            [Fact]
            public void Swiping_Backward_At_Start_Wraps_When_WrapSelection_True()
            {
                using var app = Start();
                var items = new[] { "foo", "bar", "baz" };
                var (panel, carousel) = CreateTarget(items);
                carousel.IsSwipeEnabled = true;
                carousel.WrapSelection = true;

                var e = new SwipeGestureEventArgs(1, new Vector(-10, 0), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Equal(2, panel.Children.Count);
                var target = panel.Children[1] as Control;
                Assert.Equal("baz", ((target as ContentPresenter)?.Content));
            }

            [Fact]
            public void ViewportFraction_Swiping_Backward_At_Start_Wraps_When_WrapSelection_True()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar", "baz" };
                var (panel, carousel) = CreateTarget(items, viewportFraction: 0.8);
                carousel.IsSwipeEnabled = true;
                carousel.WrapSelection = true;
                Layout(panel);

                panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(-120, 0), default));

                Assert.True(carousel.IsSwiping);
                Assert.Contains(panel.Children.OfType<ContentPresenter>(), x => Equals(x.Content, "baz"));

                panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, default));

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromSeconds(1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(2, carousel.SelectedIndex);
            }

            [Fact]
            public void Swiping_Forward_At_End_RubberBands_When_WrapSelection_False()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (panel, carousel) = CreateTarget(items);
                carousel.IsSwipeEnabled = true;
                carousel.WrapSelection = false;
                carousel.SelectedIndex = 1;

                Layout(panel);
                Layout(panel);

                Assert.Equal(2, ((IReadOnlyList<string>?)carousel.ItemsSource)?.Count);
                Assert.Equal(1, carousel.SelectedIndex);
                Assert.False(carousel.WrapSelection, "WrapSelection should be false");

                var container = Assert.IsType<ContentPresenter>(panel.Children[0]);
                Assert.Equal("bar", container.Content);

                var e = new SwipeGestureEventArgs(1, new Vector(10, 0), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Single(panel.Children);
            }

            [Fact]
            public void Swiping_Locks_To_Dominant_Axis()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (panel, carousel) = CreateTarget(items, new CrossFade(TimeSpan.FromSeconds(1)));
                carousel.IsSwipeEnabled = true;

                var e = new SwipeGestureEventArgs(1, new Vector(10, 2), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
            }

            [Fact]
            public void Swipe_Completion_Does_Not_Update_With_Same_From_And_To()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar" };
                var transition = new TrackingInteractiveTransition();
                var (panel, carousel) = CreateTarget(items, transition);
                carousel.IsSwipeEnabled = true;

                panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
                panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromSeconds(1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.True(transition.UpdateCallCount > 0);
                Assert.False(transition.SawAliasedUpdate);
                Assert.Equal(1d, transition.LastProgress);
                Assert.Equal(1, carousel.SelectedIndex);
            }

            [Fact]
            public void Swipe_Completion_Keeps_Target_Final_Interactive_Visual_State()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar" };
                var transition = new TransformTrackingInteractiveTransition();
                var (panel, carousel) = CreateTarget(items, transition);
                carousel.IsSwipeEnabled = true;

                panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
                panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromSeconds(1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(1, carousel.SelectedIndex);
                var realized = Assert.Single(panel.Children.OfType<ContentPresenter>(), x => Equals(x.Content, "bar"));
                Assert.NotNull(transition.LastTargetTransform);
                Assert.Same(transition.LastTargetTransform, realized.RenderTransform);
            }

            [Fact]
            public void Swipe_Completion_Hides_Outgoing_Page_Before_Resetting_Visual_State()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar" };
                var transition = new OutgoingTransformTrackingInteractiveTransition();
                var (panel, carousel) = CreateTarget(items, transition);
                carousel.IsSwipeEnabled = true;

                var outgoing = Assert.Single(panel.Children.OfType<ContentPresenter>(), x => Equals(x.Content, "foo"));
                bool? hiddenWhenReset = null;
                outgoing.PropertyChanged += (_, args) =>
                {
                    if (args.Property == Visual.RenderTransformProperty &&
                        args.GetNewValue<ITransform?>() is null)
                    {
                        hiddenWhenReset = !outgoing.IsVisible;
                    }
                };

                panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
                panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromSeconds(1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.True(hiddenWhenReset);
            }

            [Fact]
            public void RubberBand_Swipe_Release_Animates_Back_Through_Intermediate_Progress()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar" };
                var transition = new ProgressTrackingInteractiveTransition();
                var (panel, carousel) = CreateTarget(items, transition);
                carousel.IsSwipeEnabled = true;
                carousel.WrapSelection = false;

                panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(-100, 0), default));

                var releaseStartProgress = transition.Progresses[^1];
                var updatesBeforeRelease = transition.Progresses.Count;

                panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, default));

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromSeconds(0.1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var postReleaseProgresses = transition.Progresses.Skip(updatesBeforeRelease).ToArray();

                Assert.Contains(postReleaseProgresses, p => p > 0 && p < releaseStartProgress);

                clock.Pulse(TimeSpan.FromSeconds(1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(0d, transition.Progresses[^1]);
                Assert.Equal(0, carousel.SelectedIndex);
            }

            [Fact]
            public void ViewportFraction_SelectedIndex_Change_Drives_Progress_Updates()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar", "baz" };
                var transition = new ProgressTrackingInteractiveTransition();
                var (panel, carousel) = CreateTarget(items, transition, viewportFraction: 0.8);

                carousel.SelectedIndex = 1;

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromSeconds(0.1));
                clock.Pulse(TimeSpan.FromSeconds(1));
                sync.ExecutePostedCallbacks();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.NotEmpty(transition.Progresses);
                Assert.Contains(transition.Progresses, p => p > 0 && p < 1);
                Assert.Equal(1d, transition.Progresses[^1]);
                Assert.Equal(1, carousel.SelectedIndex);
            }

            private sealed class TrackingInteractiveTransition : IProgressPageTransition
            {
                public int UpdateCallCount { get; private set; }
                public bool SawAliasedUpdate { get; private set; }
                public double LastProgress { get; private set; }

                public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
                    => Task.CompletedTask;

                public void Update(
                    double progress,
                    Visual? from,
                    Visual? to,
                    bool forward,
                    double pageLength,
                    IReadOnlyList<PageTransitionItem> visibleItems)
                {
                    UpdateCallCount++;
                    LastProgress = progress;

                    if (from is not null && ReferenceEquals(from, to))
                        SawAliasedUpdate = true;
                }

                public void Reset(Visual visual)
                {
                    visual.RenderTransform = null;
                    visual.Opacity = 1;
                    visual.ZIndex = 0;
                    visual.Clip = null;
                }
            }

            private sealed class ProgressTrackingInteractiveTransition : IProgressPageTransition
            {
                public List<double> Progresses { get; } = new();

                public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
                    => Task.CompletedTask;

                public void Update(
                    double progress,
                    Visual? from,
                    Visual? to,
                    bool forward,
                    double pageLength,
                    IReadOnlyList<PageTransitionItem> visibleItems)
                {
                    Progresses.Add(progress);
                }

                public void Reset(Visual visual)
                {
                    visual.RenderTransform = null;
                    visual.Opacity = 1;
                    visual.ZIndex = 0;
                    visual.Clip = null;
                }
            }

            private sealed class TransformTrackingInteractiveTransition : IProgressPageTransition
            {
                public TransformGroup? LastTargetTransform { get; private set; }

                public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
                    => Task.CompletedTask;

                public void Update(
                    double progress,
                    Visual? from,
                    Visual? to,
                    bool forward,
                    double pageLength,
                    IReadOnlyList<PageTransitionItem> visibleItems)
                {
                    if (to is not Control target)
                        return;

                    if (target.RenderTransform is not TransformGroup group)
                    {
                        group = new TransformGroup
                        {
                            Children =
                            {
                                new ScaleTransform(),
                                new TranslateTransform()
                            }
                        };
                        target.RenderTransform = group;
                    }

                    var scale = Assert.IsType<ScaleTransform>(group.Children[0]);
                    var translate = Assert.IsType<TranslateTransform>(group.Children[1]);
                    scale.ScaleX = scale.ScaleY = 0.9 + (0.1 * progress);
                    translate.X = 100 * (1 - progress);
                    LastTargetTransform = group;
                }

                public void Reset(Visual visual)
                {
                    visual.RenderTransform = null;
                }
            }

            private sealed class OutgoingTransformTrackingInteractiveTransition : IProgressPageTransition
            {
                public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
                    => Task.CompletedTask;

                public void Update(
                    double progress,
                    Visual? from,
                    Visual? to,
                    bool forward,
                    double pageLength,
                    IReadOnlyList<PageTransitionItem> visibleItems)
                {
                    if (from is Control source)
                        source.RenderTransform = new TranslateTransform(100 * progress, 0);

                    if (to is Control target)
                        target.RenderTransform = new TranslateTransform(100 * (1 - progress), 0);
                }

                public void Reset(Visual visual)
                {
                    visual.RenderTransform = null;
                }
            }

            [Fact]
            public void Vertical_Swipe_Forward_Realizes_Next_Item()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var transition = new PageSlide(TimeSpan.FromSeconds(1), PageSlide.SlideAxis.Vertical);
                var (panel, carousel) = CreateTarget(items, transition);
                carousel.IsSwipeEnabled = true;

                var e = new SwipeGestureEventArgs(1, new Vector(0, 10), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Equal(2, panel.Children.Count);
                var target = panel.Children[1] as ContentPresenter;
                Assert.NotNull(target);
                Assert.Equal("bar", target.Content);
            }

            [Fact]
            public void New_Swipe_Interrupts_Active_Completion_Animation()
            {
                var clock = new MockGlobalClock();

                using var app = UnitTestApplication.Start(
                    TestServices.MockPlatformRenderInterface.With(globalClock: clock));
                using var sync = UnitTestSynchronizationContext.Begin();

                var items = new[] { "foo", "bar", "baz" };
                var transition = new TrackingInteractiveTransition();
                var (panel, carousel) = CreateTarget(items, transition);
                carousel.IsSwipeEnabled = true;

                panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
                panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

                clock.Pulse(TimeSpan.Zero);
                clock.Pulse(TimeSpan.FromMilliseconds(50));
                sync.ExecutePostedCallbacks();

                Assert.Equal(0, carousel.SelectedIndex);

                panel.RaiseEvent(new SwipeGestureEventArgs(2, new Vector(10, 0), default));

                Assert.True(carousel.IsSwiping);
                Assert.Equal(1, carousel.SelectedIndex);
            }

            [Fact]
            public void Swipe_With_NonInteractive_Transition_Does_Not_Crash()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var transition = new Mock<IPageTransition>();
                transition.Setup(x => x.Start(It.IsAny<Visual>(), It.IsAny<Visual>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                var (panel, carousel) = CreateTarget(items, transition.Object);
                carousel.IsSwipeEnabled = true;

                var e = new SwipeGestureEventArgs(1, new Vector(10, 0), default);
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Equal(2, panel.Children.Count);
            }
        }
    }
}
