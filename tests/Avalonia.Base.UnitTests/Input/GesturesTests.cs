using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Base.UnitTests.Utilities;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Moq;
using Xunit;
// ReSharper disable RedundantArgumentDefaultValue

namespace Avalonia.Base.UnitTests.Input
{
    public class GesturesTests
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

            root.AddHandler(Gestures.TappedEvent, (_, _) => raised = true);

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

            root.AddHandler(Gestures.TappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Right);

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

            root.AddHandler(Gestures.RightTappedEvent, (_, _) => raised = true);

            _mouse.Click(border, MouseButton.Right);

            Assert.True(raised);
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

        [Fact]
        public void DoubleTapped_Should_Not_Be_Raised_For_Middle_Button()
        {
            Border border = new Border();
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(Gestures.DoubleTappedEvent, (_, _) => raised = true);

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

            root.AddHandler(Gestures.DoubleTappedEvent, (_, _) => raised = true);

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
            Gestures.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            HoldingState holding = HoldingState.Cancelled;

            root.AddHandler(Gestures.HoldingEvent, (_, e) => holding = e.HoldingState);
            
            _mouse.Down(border);
            Assert.False(holding != HoldingState.Cancelled);
            
            // Verify timer duration, but execute it immediately.
            var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
            Assert.Equal(iSettingsMock.Object.HoldWaitDuration, timer.Interval);
            timer.ForceFire();

            Assert.True(holding == HoldingState.Started);

            _mouse.Up(border);

            Assert.True(holding == HoldingState.Completed);
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
            Gestures.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(Gestures.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Started);
            
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
            Gestures.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            var raised = false;

            root.AddHandler(Gestures.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Completed);
            
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
            Gestures.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot
            {
                Child = border
            };
            var cancelled = false;

            root.AddHandler(Gestures.HoldingEvent, (_, e) => cancelled = e.HoldingState == HoldingState.Cancelled);
            
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
            Gestures.SetIsHoldWithMouseEnabled(border, true);
            var root = new TestRoot()
            {
                Child = border
            };
            var cancelled = false;

            root.AddHandler(Gestures.HoldingEvent, (_, e) => cancelled = e.HoldingState == HoldingState.Cancelled);
            
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
            Gestures.SetIsHoldWithMouseEnabled(border, true);
            var testRoot = new TestRoot
            {
                Child = border
            };
            var raised = false;

            testRoot.AddHandler(Gestures.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Completed);

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

            root.AddHandler(Gestures.TappedEvent, (_, _) => result.Add("dt"));
            root.AddHandler(Gestures.DoubleTappedEvent, (_, _) => result.Add("ddt"));
            border.AddHandler(Gestures.TappedEvent, (_, _) => result.Add("bt"));
            border.AddHandler(Gestures.DoubleTappedEvent, (_, _) => result.Add("bdt"));
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

            root.AddHandler(Gestures.PinchEvent, (_, _) => raised = true);

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

            root.AddHandler(Gestures.PinchEvent, (_, _) => raised = true);

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

            root.AddHandler(Gestures.PinchEvent, (_, _) => raised = true);

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

            root.AddHandler(Gestures.ScrollGestureEvent, (_, _) => raised = true);

            var firstTouch = new TouchTestHelper();

            firstTouch.Down(border, position: new Point(5, 5));
            firstTouch.Move(border, position: new Point(20, 20));

            Assert.False(raised);

            firstTouch.Move(border, position: new Point(70, 20));

            Assert.True(raised);
        }
    }
}
