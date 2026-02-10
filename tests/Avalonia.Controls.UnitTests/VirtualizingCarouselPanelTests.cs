using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
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
        }

        private static IDisposable Start() => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        private static (VirtualizingCarouselPanel, Carousel) CreateTarget(
            IEnumerable items,
            IPageTransition? transition = null)
        {
            var carousel = new Carousel
            {
                ItemsSource = items,
                Template = CarouselTemplate(),
                PageTransition = transition,
            };

            var root = new TestRoot(carousel);
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

        private static void Layout(Control c) => ((ILayoutRoot)c.GetVisualRoot()!).LayoutManager.ExecuteLayoutPass();

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

                // Simulate swipe start (delta X > 0)
                var e = new SwipeGestureEventArgs(1, new Vector(10, 0));
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Equal(2, panel.Children.Count);
                var target = panel.Children[1] as Control;
                Assert.NotNull(target);
                Assert.True(target.IsVisible);
                Assert.Equal("bar", ((target as ContentPresenter)?.Content));
            }

            [Fact]
            public void Swiping_Backward_At_Start_Is_Blocked_When_WrapSelection_False()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (panel, carousel) = CreateTarget(items);
                carousel.IsSwipeEnabled = true;
                carousel.WrapSelection = false;

                // Simulate swipe start (delta X < 0)
                var e = new SwipeGestureEventArgs(1, new Vector(-10, 0));
                panel.RaiseEvent(e);

                Assert.False(carousel.IsSwiping);
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

                // Simulate swipe start (delta X < 0)
                var e = new SwipeGestureEventArgs(1, new Vector(-10, 0));
                panel.RaiseEvent(e);

                Assert.True(carousel.IsSwiping);
                Assert.Equal(2, panel.Children.Count);
                var target = panel.Children[1] as Control;
                Assert.Equal("baz", ((target as ContentPresenter)?.Content));
            }

            [Fact]
            public void Swiping_Forward_At_End_Is_Blocked_When_WrapSelection_False()
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

                // Verify the correct item is realized at index 1 before the swipe
                var container = Assert.IsType<ContentPresenter>(panel.Children[0]);
                Assert.Equal("bar", container.Content);

                // Simulate swipe start (delta X > 0)
                var e = new SwipeGestureEventArgs(1, new Vector(10, 0));
                panel.RaiseEvent(e);

                Assert.False(carousel.IsSwiping, "Carousel should NOT be swiping at the end boundary");
                Assert.Single(panel.Children);
            }

            [Fact]
            public void Swiping_Locks_To_Dominant_Axis()
            {
                using var app = Start();
                var items = new[] { "foo", "bar" };
                var (panel, carousel) = CreateTarget(items, new CrossFade(TimeSpan.FromSeconds(1)));
                carousel.IsSwipeEnabled = true;

                // Simulate swipe with more X than Y
                var e = new SwipeGestureEventArgs(1, new Vector(10, 2));
                panel.RaiseEvent(e);
                
                Assert.True(carousel.IsSwiping);
            }
        }
    }
}
