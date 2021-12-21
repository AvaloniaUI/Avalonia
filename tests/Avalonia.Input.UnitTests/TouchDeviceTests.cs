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
                Assert.Equal(1, tappedExecutedTimes);
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
                SendXTouchContactsWithIds(InputManager.Instance, touchDevice, root, RawPointerEventType.TouchBegin, 0, 1);
                SendXTouchContactsWithIds(InputManager.Instance, touchDevice, root, RawPointerEventType.TouchEnd, 0, 1);
                Assert.Equal(2, tappedExecutedTimes);
                Assert.False(isDoubleTapped);
                Assert.Equal(0, doubleTappedExecutedTimes);
            }
        }

        [Fact]
        public void Click_Counting_Should_Work_Correctly_With_Few_Touch_Contacts()
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

                var pointerPressedExecutedTimes = 0;
                var tappedExecutedTimes = 0;
                var isDoubleTapped = false;
                var doubleTappedExecutedTimes = 0;
                root.PointerPressed += (a, e) =>
                {
                    pointerPressedExecutedTimes++;
                    switch (pointerPressedExecutedTimes)
                    {
                        case <= 2:
                            Assert.True(e.ClickCount == 1);
                            break;
                        case 3:
                            Assert.True(e.ClickCount == 2);
                            break;
                        case 4:
                            Assert.True(e.ClickCount == 3);
                            break;
                        case 5:
                            Assert.True(e.ClickCount == 4);
                            break;
                        case 6:
                            Assert.True(e.ClickCount == 5);
                            break;
                        case 7:
                            Assert.True(e.ClickCount == 1);
                            break;
                        case 8:
                            Assert.True(e.ClickCount == 1);
                            break;
                        case 9:
                            Assert.True(e.ClickCount == 2);
                            break;
                        default:
                            break;
                    }
                };
                root.DoubleTapped += (a, e) =>
                {
                    isDoubleTapped = true;
                    doubleTappedExecutedTimes++;
                };
                root.Tapped += (a, e) =>
                {
                    tappedExecutedTimes++;
                };
                SendXTouchContactsWithIds(InputManager.Instance, touchDevice, root, RawPointerEventType.TouchBegin, 0, 1);
                SendXTouchContactsWithIds(InputManager.Instance, touchDevice, root, RawPointerEventType.TouchEnd, 0, 1);
                TapOnce(InputManager.Instance, touchDevice, root, touchPointId: 2);
                TapOnce(InputManager.Instance, touchDevice, root, touchPointId: 3);
                TapOnce(InputManager.Instance, touchDevice, root, touchPointId: 4);
                SendXTouchContactsWithIds(InputManager.Instance, touchDevice, root, RawPointerEventType.TouchBegin, 5, 6, 7);
                SendXTouchContactsWithIds(InputManager.Instance, touchDevice, root, RawPointerEventType.TouchEnd, 5, 6, 7);
                TapOnce(InputManager.Instance, touchDevice, root, touchPointId: 8);
                Assert.Equal(6, tappedExecutedTimes);
                Assert.Equal(9, pointerPressedExecutedTimes);
                Assert.True(isDoubleTapped);
                Assert.Equal(3, doubleTappedExecutedTimes);

            }
        }
        private static void SendXTouchContactsWithIds(IInputManager inputManager, TouchDevice device, IInputRoot root, RawPointerEventType type, params long[] touchPointIds)
        {
            for (int i = 0; i < touchPointIds.Length; i++)
            {
                inputManager.ProcessInput(new RawTouchEventArgs(device, 0,
                                                              root,
                                                              type,
                                                              new Point(0, 0),
                                                              RawInputModifiers.None,
                                                              touchPointIds[i]));
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
