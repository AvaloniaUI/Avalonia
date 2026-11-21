using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;
// ReSharper disable RedundantArgumentDefaultValue

namespace Avalonia.Base.UnitTests.Input
{
    public class GesturesTests : ScopedTestBase
    {
        private readonly MouseTestHelper _mouse = new MouseTestHelper();

        [Fact]
        public void Tapped_Should_Follow_Pointer_Pressed_Released()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, false);

            _mouse.Click(border);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
        }

        [Fact]
        public void Tapped_Should_Be_Raised_Even_When_Pressed_Released_Handled()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, true);

            _mouse.Click(border);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
        }

        [Fact]
        public void Tapped_Should_Not_Be_Raised_For_Middle_Button()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Middle);

            Assert.False(raised);
        }

        [Fact]
        public void Tapped_Should_Not_Be_Raised_For_Right_Button()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Right);

            Assert.False(raised);
        }

        [Fact]
        public void Tapped_Should_Be_Raised_From_Captured_Control()
        {
            Border inner = new Border()
            {
                Focusable = true,
                Name = "Inner"
            };
            Border border = new Border()
            {
                Focusable = true,
                Child = inner,
                Name = "Parent"
            };
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            border.PointerPressed += (s, e) =>
            {
                e.Pointer.Capture(inner);
            };
            _mouse.Click(border, MouseButton.Left);

            root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Left);


            Assert.True(raised);
        }

        [Fact]
        public void Tapped_Should_Be_Raised_After_Hold_Duration_When_Hold_With_Mouse_Is_Disabled()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            var border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, false);

            _mouse.Down(border);

            // The pointer is held longer than the hold duration before being released.
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            timer.ForceFire();

            _mouse.Up(border);

            Assert.Equal(["bp", "dp", "br", "dr", "bt", "dt"], result);
        }

        [Fact]
        public void Tapped_Should_Not_Be_Raised_For_Drag()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

            _mouse.Down(border, position: new Point(10, 10));
            _mouse.Move(border, new Point(60, 60));
            _mouse.Up(border, position: new Point(60, 60));

            Assert.False(raised);
        }

        [Fact]
        public void RightTapped_Should_Be_Raised_For_Right_Button()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.RightTappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Right);

            Assert.True(raised);
        }

        [Fact]
        public void RightTapped_Should_Be_Raised_When_Pointer_Moves_Within_Tap_Size()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.RightTappedEvent, (_, _) => raised = true);

            _mouse.Down(border, MouseButton.Right, new Point(50, 50));
            _mouse.Move(border, new Point(52, 52));
            _mouse.Up(border, MouseButton.Right, new Point(52, 52));

            Assert.True(raised);
        }

        [Fact]
        public void RightTapped_Should_Not_Be_Raised_For_Drag()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.RightTappedEvent, (_, _) => raised = true);

            _mouse.Down(border, MouseButton.Right, new Point(50, 50));
            _mouse.Move(border, new Point(100, 100));
            _mouse.Up(border, MouseButton.Right, new Point(100, 100));

            Assert.False(raised);
        }

        [Fact]
        public void DoubleTapped_Should_Follow_Pointer_Pressed_Released_Pressed()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, false);

            _mouse.Click(border);
            _mouse.Down(border, clickCount: 2);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
        }

        [Fact]
        public void DoubleTapped_Should_Be_Raised_Even_When_Pressed_Released_Handled()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, true);

            _mouse.Click(border);
            _mouse.Down(border, clickCount: 2);

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
        }

        [Theory]
        [InlineData(PointerType.Touch, 56)]
        [InlineData(PointerType.Touch, 50)]
        [InlineData(PointerType.Pen, 56)]
        [InlineData(PointerType.Pen, 50)]
        public void Tapped_Should_Not_Be_Raised_After_DoubleTapped_When_Pointer_Moves_Slightly(
            PointerType pointerType,
            double secondPressX)
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(10, 10));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();
            var pointer = new MouseTestHelper(pointerType);

            root.AddHandler(InputElement.TappedEvent, (_, _) => result.Add("t"));
            root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => result.Add("dt"));

            pointer.Click(border, position: new Point(50, 50));
            pointer.Down(border, position: new Point(secondPressX, 50), clickCount: 2);
            pointer.Move(border, new Point(56, 50));
            pointer.Up(border, position: new Point(56, 50));

            Assert.Equal(new[] { "t", "dt" }, result);
        }

        [Fact]
        public void Tapped_Should_Not_Be_Raised_On_DoubleTapped_Release()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, false);

            _mouse.DoubleClick(border);
            _mouse.Up(border);

            Assert.Equal(["bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt", "br", "dr"], result);
        }

        [Fact]
        public void DoubleTapped_Should_Be_Raised_After_Control_Changes()
        {
            var border1 = new Border();
            var border2 = new Border { IsVisible = false };
            var root = new TestRoot
            {
                Child = new Panel { Children = { border1, border2 } }
            };
            var result = new List<string>();

            border1.AddHandler(InputElement.DoubleTappedEvent, (_, _) =>
            {
                result.Add("dt1");
                border1.IsVisible = false;
                border2.IsVisible = true;
            });
            border2.AddHandler(InputElement.DoubleTappedEvent, (_, _) => result.Add("dt2"));

            _mouse.DoubleClick(border1);
            _mouse.Up(border1);
            _mouse.Down(border2, clickCount: 3);
            _mouse.Up(border2);
            _mouse.Down(border2, clickCount: 4);

            Assert.Equal(["dt1", "dt2"], result);
        }

        [Fact]
        public void DoubleTapped_Should_Not_Be_Raised_When_Click_Count_Is_Reset()
        {
            var border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var result = new List<string>();

            AddHandlers(root, border, result, false);

            _mouse.Click(border);
            _mouse.Click(border);

            Assert.Equal(["bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "br", "dr", "bt", "dt"], result);
        }

        [Fact]
        public void DoubleTapped_Should_Not_Be_Raised_For_Middle_Button()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Middle);
            _mouse.Down(border, MouseButton.Middle, clickCount: 2);

            Assert.False(raised);
        }

        [Fact]
        public void DoubleTapped_Should_Not_Be_Raised_For_Right_Button()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Right);
            _mouse.Down(border, MouseButton.Right, clickCount: 2);

            Assert.False(raised);
        }

        [Fact]
        public void Hold_Should_Be_Raised_After_Hold_Duration()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            HoldingState holding = HoldingState.Canceled;

            root.AddHandler(InputElement.HoldingEvent, (_, e) => holding = e.HoldingState);

            _mouse.Down(border);
            Assert.False(holding != HoldingState.Canceled);

            // Verify timer duration, but execute it immediately.
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            Assert.True(holding == HoldingState.Started);

            _mouse.Up(border);

            Assert.True(holding == HoldingState.Completed);
        }

        [Fact]
        public void Started_Hold_Gesture_Should_Raise_Context_Requested_Event()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            var flyout = new Flyout();
            Border border = new Border()
            {
                ContextFlyout = flyout
            };
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };

            var contextRequested = false;

            flyout.Opened += (s, e) => contextRequested = true;

            _mouse.Down(border);
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            timer.ForceFire();
            _mouse.Up(border);

            Assert.True(contextRequested);
        }

        [Fact]
        public void Cancelled_Hold_Gesture_Should_Cancel_Context_Flyout()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            var flyout = new Flyout();
            Border border = new Border()
            {
                ContextFlyout = flyout
            };
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };

            var contextRequested = false;
            var contextCanceled = false;

            flyout.Opened += (s, e) => contextRequested = true;
            flyout.Closed += (s, e) => contextCanceled = true;

            _mouse.Down(border);
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            timer.ForceFire();
            _mouse.Move(border, new Point(100, 100));

            Assert.True(contextRequested);
            Assert.True(contextCanceled);
        }

        [Fact]
        public void Hold_Should_Not_Raised_When_Pointer_Released_Before_Timer()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Started);

            _mouse.Down(border);
            Assert.False(raised);

            _mouse.Up(border);
            Assert.False(raised);

            // Verify timer duration, but execute it immediately.
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            Assert.False(raised);
        }

        [Fact]
        public void Hold_Should_Not_Raised_When_Pointer_Is_Moved_Before_Timer()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Completed);

            _mouse.Down(border);
            Assert.False(raised);

            _mouse.Move(border, position: new Point(20, 20));
            Assert.False(raised);

            // Verify timer duration, but execute it immediately.
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            Assert.False(raised);
        }

        [Fact]
        public void Hold_Should_Be_Cancelled_When_Second_Contact_Is_Detected()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            var cancelled = false;

            root.AddHandler(InputElement.HoldingEvent, (_, e) => cancelled = e.HoldingState == HoldingState.Canceled);

            _mouse.Down(border);
            Assert.False(cancelled);

            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            var secondMouse = new MouseTestHelper();

            secondMouse.Down(border);

            Assert.True(cancelled);
        }

        [Fact]
        public void Hold_Should_Be_Cancelled_When_Pointer_Moves_Too_Far()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot()
            {
                Child = border
            };
            var cancelled = false;

            root.AddHandler(InputElement.HoldingEvent, (_, e) => cancelled = e.HoldingState == HoldingState.Canceled);

            _mouse.Down(border);

            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            _mouse.Move(border, position: new Point(3, 3));

            Assert.False(cancelled);

            _mouse.Move(border, position: new Point(20, 20));

            Assert.True(cancelled);
        }

        [Fact]
        public void Hold_Should_Not_Be_Raised_For_Multiple_Contacts()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.HoldWaitDuration).Returns(TimeSpan.FromMilliseconds(300));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);

            using var app = UnitTestApplication.Start();

            Border border = new Border();
            InputElement.SetIsHoldWithMouseEnabled(border, true);
            var testRoot = new TestRoot
            {
                Child = border
            };
            var raised = false;

            testRoot.AddHandler(InputElement.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Completed);

            var secondMouse = new MouseTestHelper();

            _mouse.Down(border, MouseButton.Left);

            // Verify timer duration, but execute it immediately.
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            secondMouse.Down(border, MouseButton.Left);

            Assert.False(raised);
        }

        private static void AddHandlers(
            TestRoot root,
            Border border,
            IList<string> result,
            bool markHandled)
        {
            root.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
            {
                result.Add("dp");

                if (markHandled)
                {
                    e.Handled = true;
                }
            });

            root.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
            {
                result.Add("dr");

                if (markHandled)
                {
                    e.Handled = true;
                }
            });

            border.AddHandler(InputElement.PointerPressedEvent, (_, _) => result.Add("bp"));
            border.AddHandler(InputElement.PointerReleasedEvent, (_, _) => result.Add("br"));

            root.AddHandler(InputElement.TappedEvent, (_, _) => result.Add("dt"));
            root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => result.Add("ddt"));
            border.AddHandler(InputElement.TappedEvent, (_, _) => result.Add("bt"));
            border.AddHandler(InputElement.DoubleTappedEvent, (_, _) => result.Add("bdt"));
        }

        [Fact]
        public void Pinched_Should_Not_Be_Raised_For_Same_Pointer()
        {
            var touch = new TouchTestHelper();

            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new PinchGestureRecognizer());
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.PinchEvent, (_, _) => raised = true);

            var firstPoint = new Point(5, 5);
            var secondPoint = new Point(10, 10);

            touch.Down(border, position: firstPoint);
            touch.Down(border, position: secondPoint);
            touch.Down(border, position: new Point(20, 20));

            Assert.False(raised);
        }

        [Fact]
        public void Pinched_Should_Be_Raised_For_Two_Pointers_Moving()
        {
            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new PinchGestureRecognizer());
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.PinchEvent, (_, _) => raised = true);

            var firstPoint = new Point(5, 5);
            var secondPoint = new Point(10, 10);

            var firstTouch = new TouchTestHelper();
            var secondTouch = new TouchTestHelper();

            firstTouch.Down(border, position: firstPoint);
            secondTouch.Down(border, position: secondPoint);
            secondTouch.Move(border, position: new Point(20, 20));

            Assert.True(raised);
        }

        [Fact]
        public void Gestures_Should_Be_Cancelled_When_Pointer_Capture_Is_Lost()
        {
            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new PinchGestureRecognizer());
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.PinchEvent, (_, _) => raised = true);

            var firstPoint = new Point(5, 5);
            var secondPoint = new Point(10, 10);

            var firstTouch = new TouchTestHelper();
            var secondTouch = new TouchTestHelper();

            firstTouch.Down(border, position: firstPoint);

            firstTouch.Cancel();

            secondTouch.Down(border, position: secondPoint);
            secondTouch.Move(border, position: new Point(20, 20));

            Assert.False(raised);
        }

        [Fact]
        public void Scrolling_Should_Start_After_Start_Distance_Is_Exceeded()
        {
            Border border = new Border()
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Red)
            };
            border.GestureRecognizers.Add(new ScrollGestureRecognizer()
            {
                CanHorizontallyScroll = true,
                CanVerticallyScroll = true,
                ScrollStartDistance = 50
            });
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(InputElement.ScrollGestureEvent, (_, _) => raised = true);

            var firstTouch = new TouchTestHelper();

            firstTouch.Down(border, position: new Point(5, 5));
            firstTouch.Move(border, position: new Point(20, 20));

            Assert.False(raised);

            firstTouch.Move(border, position: new Point(70, 20));

            Assert.True(raised);
        }
    }
}
