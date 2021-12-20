using System;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class TouchDeviceTests
    {
        [Fact]
        public void Tapped_Event_Is_Fired_With_Touch()
        {
            using (UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager())))
            {
                var root = new TestRoot();
                var touchDevice = new TouchDevice();

                var isTapped = false;
                var executedTimes = 0;
                root.Tapped += (a, e) =>
                {
                    isTapped = true;
                    executedTimes++;
                };
                TapOnce(InputManager.Instance, touchDevice, root);
                Assert.True(isTapped);
                Assert.Equal(1, executedTimes);
            }
        }

        [Fact]
        public void DoubleTapped_Event_Is_Fired_With_Touch()
        {
            var platformSettingsMock = new Mock<IPlatformSettings>();
            platformSettingsMock.Setup(x => x.DoubleClickTime).Returns(new TimeSpan(200));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
               .Bind<IPlatformSettings>().ToConstant(platformSettingsMock.Object);
            using (UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager())))
            {
                var root = new TestRoot();
                var touchDevice = new TouchDevice();

                var isDoubleTapped = false;
                var doubleTappedExecutedTimes = 0;
                var tappedExecutedTimes = 0;
                root.DoubleTapped += (a, e) =>
                {
                    isDoubleTapped = true;
                    doubleTappedExecutedTimes++;
                };
                root.Tapped += (a, e) =>
                {
                    tappedExecutedTimes++;
                };
                TapOnce(InputManager.Instance, touchDevice, root);
                TapOnce(InputManager.Instance, touchDevice, root, touchPointId: 1);
                Assert.Equal(2, tappedExecutedTimes);
                Assert.True(isDoubleTapped);
                Assert.Equal(1, doubleTappedExecutedTimes);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void PointerPressed_Counts_Clicks_Correctly(int clickCount)
        {
            var platformSettingsMock = new Mock<IPlatformSettings>();
            platformSettingsMock.Setup(x => x.DoubleClickTime).Returns(new TimeSpan(200));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
               .Bind<IPlatformSettings>().ToConstant(platformSettingsMock.Object);
            using (UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager())))
            {
                var root = new TestRoot();
                var touchDevice = new TouchDevice();

                var pointerPressedExecutedTimes = 0;
                var pointerPressedClicks = 0;
                root.PointerPressed += (a, e) =>
                {
                    pointerPressedClicks = e.ClickCount;
                    pointerPressedExecutedTimes++;
                };
                for (int i = 0; i < clickCount; i++)
                {
                    TapOnce(InputManager.Instance, touchDevice, root, touchPointId: i);
                }

                Assert.Equal(clickCount, pointerPressedExecutedTimes);
                Assert.Equal(pointerPressedClicks, clickCount);
            }
        }

        [Fact]
        public void DoubleTapped_Not_Fired_When_Click_Too_Late()
        {
            var platformSettingsMock = new Mock<IPlatformSettings>();
            platformSettingsMock.Setup(x => x.DoubleClickTime).Returns(new TimeSpan(0, 0, 0, 0, 20));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
               .Bind<IPlatformSettings>().ToConstant(platformSettingsMock.Object);
            using (UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager())))
            {
                var root = new TestRoot();
                var touchDevice = new TouchDevice();

                var isDoubleTapped = false;
                var doubleTappedExecutedTimes = 0;
                var tappedExecutedTimes = 0;
                root.DoubleTapped += (a, e) =>
                {
                    isDoubleTapped = true;
                    doubleTappedExecutedTimes++;
                };
                root.Tapped += (a, e) =>
                {
                    tappedExecutedTimes++;
                };
                TapOnce(InputManager.Instance, touchDevice, root);
                TapOnce(InputManager.Instance, touchDevice, root, 21, 1);
                Assert.Equal(2, tappedExecutedTimes);
                Assert.False(isDoubleTapped);
                Assert.Equal(0, doubleTappedExecutedTimes);
            }
        }

        [Fact]
        public void DoubleTapped_Not_Fired_When_Second_Click_Is_From_Different_Touch_Contact()
        {
            var tmp = new Mock<IPlatformSettings>();
            tmp.Setup(x => x.DoubleClickTime).Returns(new TimeSpan(200));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
               .Bind<IPlatformSettings>().ToConstant(tmp.Object);
            using (UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager())))
            {
                var root = new TestRoot();
                var touchDevice = new TouchDevice();

                var isDoubleTapped = false;
                var doubleTappedExecutedTimes = 0;
                var tappedExecutedTimes = 0;
                root.DoubleTapped += (a, e) =>
                {
                    isDoubleTapped = true;
                    doubleTappedExecutedTimes++;
                };
                root.Tapped += (a, e) =>
                {
                    tappedExecutedTimes++;
                };
                InputManager.Instance.ProcessInput(new RawTouchEventArgs(touchDevice, 0,
                                             root,
                                             RawPointerEventType.TouchBegin,
                                             new Point(0, 0),
                                             RawInputModifiers.None,
                                             0));
                InputManager.Instance.ProcessInput(new RawTouchEventArgs(touchDevice, 0,
                                             root,
                                             RawPointerEventType.TouchBegin,
                                             new Point(0, 0),
                                             RawInputModifiers.None,
                                             1));
                InputManager.Instance.ProcessInput(new RawTouchEventArgs(touchDevice, 0,
                                            root,
                                            RawPointerEventType.TouchEnd,
                                            new Point(0, 0),
                                            RawInputModifiers.None,
                                            0));
                InputManager.Instance.ProcessInput(new RawTouchEventArgs(touchDevice, 0,
                                             root,
                                             RawPointerEventType.TouchEnd,
                                             new Point(0, 0),
                                             RawInputModifiers.None,
                                             1));
                Assert.Equal(2, tappedExecutedTimes);
                Assert.False(isDoubleTapped);
                Assert.Equal(0, doubleTappedExecutedTimes);
            }
        }

        private static void TapOnce(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
        {
            inputManager.ProcessInput(new RawTouchEventArgs(device, timestamp,
                                               root,
                                               RawPointerEventType.TouchBegin,
                                               new Point(0, 0),
                                               RawInputModifiers.None,
                                               touchPointId));
            inputManager.ProcessInput(new RawTouchEventArgs(device, timestamp,
                                                root,
                                                RawPointerEventType.TouchEnd,
                                                new Point(0, 0),
                                                RawInputModifiers.None,
                                                touchPointId));
        }
    }
}
