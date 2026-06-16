using System;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform
{
    public class InputPaneTests : ScopedTestBase
    {
        [Fact]
        public void Resize_With_Input_Pane_ScrollViewer_Brings_Focused_Into_View()
        {
            var clock = new MockGlobalClock();
            using (UnitTestApplication.Start(TestServices.RealFocus.
                With(globalClock: clock)))
            {
                var inputPane = new TestInputPane(new Rect(0, 200, 200, 200));
                var target = new Button
                {
                    Width = 200,
                    Height = 100
                };

                var panel = new StackPanel()
                {
                    Width = 200,
                    Height = 1000,
                    Orientation = Layout.Orientation.Vertical,
                    Children =
                    {
                        new Border()
                        {
                            Width = 200,
                            Height = 900,
                        },
                        target,
                        new Border()
                        {
                            Width = 200,
                            Height = 100,
                        }
                    }
                };

                var scrollViewer = new ScrollViewer
                {
                    Template = new FuncControlTemplate<ScrollViewer>(CreateScrollViewerTemplate),
                    Content = panel,
                    Width = 200,
                    Height = 600,
                    ResizeWithInputPane = true
                };

                var impl = CreateMockTopLevelImpl(inputPane);
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = scrollViewer
                };

                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(0, scrollViewer.Offset.Y);

                target.Focus();

                inputPane.Open();

                clock.Pulse(TimeSpan.FromSeconds(5));

                var timers = Dispatcher.SnapshotTimersForUnitTests();

                foreach (var timer in timers)
                {
                    DispatcherTimerUtils.ForceFire(timer);
                }

                topLevel.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(600, scrollViewer.Offset.Y);
            }
        }

        [Fact]
        public void InputPaneAwareView_Resizes_Content_When_Behavior_Resize()
        {
            var clock = new MockGlobalClock();
            using (UnitTestApplication.Start(TestServices.RealFocus.
                With(globalClock: clock)))
            {
                var inputPane = new TestInputPane(new Rect(0, 200, 200, 200));

                var border = new Border();

                var paneView = new InputPaneAwareView()
                {
                    Child = border,
                    Height = 500,
                    Width = 200,
                    Behavior = InputPaneAwareBehavior.Resize
                };

                var impl = CreateMockTopLevelImpl(inputPane);
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = paneView
                };

                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(500, border.Bounds.Height);

                inputPane.Open();

                clock.Pulse(TimeSpan.FromSeconds(5));

                topLevel.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(300, border.Bounds.Height);
            }
        }

        [Fact]
        public void InputPaneAwareView_Resizes_Content_When_Behavior_Pan()
        {
            var clock = new MockGlobalClock();
            using (UnitTestApplication.Start(TestServices.RealFocus.
                With(globalClock: clock)))
            {
                var inputPane = new TestInputPane(new Rect(0, 200, 200, 200));

                var border = new Border();

                var paneView = new InputPaneAwareView()
                {
                    Child = border,
                    Height = 500,
                    Width = 200,
                    Behavior = InputPaneAwareBehavior.Pan
                };

                var impl = CreateMockTopLevelImpl(inputPane);
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = paneView
                };

                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(0, border.Bounds.Top);

                inputPane.Open();

                clock.Pulse(TimeSpan.FromSeconds(5));

                topLevel.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(-200, border.Bounds.Top);
            }
        }

        internal static Control CreateScrollViewerTemplate(ScrollViewer control, INameScope scope)
        {
            return new Grid
            {
                Children =
                {
                    new Decorator()
                    {
                        Name = "PART_KeyboardAwareDecorator",
                        Child  = new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope)
                    }.RegisterInNameScope(scope),
                },
            };
        }

        static Mock<ITopLevelImpl> CreateMockTopLevelImpl(TestInputPane inputPane)
        {
            var topLevel = new Mock<ITopLevelImpl>();
            topLevel.Setup(x => x.RenderScaling).Returns(1);
            topLevel.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            topLevel.Setup(x => x.TryGetFeature(typeof(IInputPane))).Returns(inputPane);
            return topLevel;
        }

        private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestInputPane(Rect openRect) : IInputPane
        {
            public InputPaneState State { get; private set; }

            public Rect OccludedRect { get; private set; }

            public event EventHandler<InputPaneStateEventArgs>? StateChanged;

            public void Open()
            {
                var oldRect = OccludedRect;
                OccludedRect = openRect;

                State = InputPaneState.Open;

                StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, oldRect, OccludedRect));
            }

            public void Close()
            {
                var oldRect = OccludedRect;
                OccludedRect = default;

                State = InputPaneState.Closed;

                StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, oldRect, OccludedRect));
            }
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {

        }
    }
}
