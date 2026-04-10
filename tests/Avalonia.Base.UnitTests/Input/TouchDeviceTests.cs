using System;
using System.Windows.Input;
using Avalonia.Base.UnitTests.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class TouchDeviceTests : PointerTestsBase
    {
        [Fact]
        public void Tapped_Event_Is_Fired_With_Touch()
        {
            using var app = UnitTestApp(new TimeSpan(200));
            var root = new TestRoot();
            var touchDevice = new TouchDevice();

            var isTapped = false;
            var executedTimes = 0;
            root.Tapped += (a, e) =>
            {
                isTapped = true;
                executedTimes++;
            };
            TapOnce(InputManager.Instance!, touchDevice, root);
            Assert.True(isTapped);
            Assert.Equal(1, executedTimes);

        }

        [Fact]
        public void DoubleTapped_Event_Is_Fired_With_Touch()
        {
            using var app = UnitTestApp(new TimeSpan(200));
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
            var inputManager = InputManager.Instance!;
            TapOnce(inputManager, touchDevice, root);
            TapOnce(inputManager, touchDevice, root, touchPointId: 1);
            Assert.Equal(1, tappedExecutedTimes);
            Assert.True(isDoubleTapped);
            Assert.Equal(1, doubleTappedExecutedTimes);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void PointerPressed_Counts_Clicks_Correctly(int clickCount)
        {
            using var app = UnitTestApp(new TimeSpan(200));
            var root = new TestRoot();
            var touchDevice = new TouchDevice();

            var pointerPressedExecutedTimes = 0;
            var pointerPressedClicks = 0;
            root.PointerPressed += (a, e) =>
            {
                pointerPressedClicks = e.ClickCount;
                pointerPressedExecutedTimes++;
            };
            var inputManager = InputManager.Instance!;
            for (int i = 0; i < clickCount; i++)
            {
                TapOnce(inputManager, touchDevice, root, touchPointId: i);
            }

            Assert.Equal(clickCount, pointerPressedExecutedTimes);
            Assert.Equal(pointerPressedClicks, clickCount);
        }

        [Fact]
        public void DoubleTapped_Not_Fired_When_Click_Too_Late()
        {
            using var app = UnitTestApp(new TimeSpan(0, 0, 0, 0, 20));
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
            var inputManager = InputManager.Instance!;
            TapOnce(inputManager, touchDevice, root);
            TapOnce(inputManager, touchDevice, root, 21, 1);
            Assert.Equal(2, tappedExecutedTimes);
            Assert.False(isDoubleTapped);
            Assert.Equal(0, doubleTappedExecutedTimes);

        }

        [Fact]
        public void DoubleTapped_Not_Fired_When_Second_Click_Is_From_Different_Touch_Contact()
        {
            using var app = UnitTestApp(new TimeSpan(200));
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
            var inputManager = InputManager.Instance!;
            SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchBegin, 0, 1);
            SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchEnd, 0, 1);
            Assert.Equal(2, tappedExecutedTimes);
            Assert.False(isDoubleTapped);
            Assert.Equal(0, doubleTappedExecutedTimes);
        }

        [Fact]
        public void Touch_Pointer_Should_Set_Focus_On_Pointer_Released()
        {
            using var scope = AvaloniaLocator.EnterScope();
            using var app = UnitTestApplication.Start(
               TestServices.RealFocus);

            var impl = CreateTopLevelImplMock();

            var renderer = new Mock<IHitTester>();
            var root = new TestTopLevel(impl.Object)
            {
                HitTesterOverride = renderer.Object,
            };
            var host = root.TopLevelHost;

            host.Focusable = true;
            var touchDevice = new TouchDevice();
            var inputManager = InputManager.Instance!;

            Assert.False(host.IsFocused);

            Press(InputManager.Instance!, touchDevice, root.InputRoot);

            Assert.False(host.IsFocused);
            Release(InputManager.Instance!, touchDevice, root.InputRoot);

            Assert.True(host.IsFocused);
        }

        [Fact]
        public void Click_Counting_Should_Work_Correctly_With_Few_Touch_Contacts()
        {
            using var app = UnitTestApp(new TimeSpan(200));

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
            var inputManager = InputManager.Instance!;
            SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchBegin, 0, 1);
            SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchEnd, 0, 1);
            TapOnce(inputManager, touchDevice, root, touchPointId: 2);
            TapOnce(inputManager, touchDevice, root, touchPointId: 3);
            TapOnce(inputManager, touchDevice, root, touchPointId: 4);
            SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchBegin, 5, 6, 7);
            SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchEnd, 5, 6, 7);
            TapOnce(inputManager, touchDevice, root, touchPointId: 8);
            Assert.Equal(6, tappedExecutedTimes);
            Assert.Equal(9, pointerPressedExecutedTimes);
            Assert.True(isDoubleTapped);
            Assert.Equal(3, doubleTappedExecutedTimes);
        }

        [Fact]
        public void ToggleButton_Does_Not_Toggle_When_Command_Becomes_Disabled_Between_TouchBegin_And_TouchEnd()
        {
            using var app = UnitTestApp(new TimeSpan(200));

            var renderer = new Mock<IHitTester>();
            var impl = CreateTopLevelImplMock();
            var command = new TestCommand(true);
            var target = new ToggleButton
            {
                Width = 100,
                Height = 100,
                Command = command,
            };
            var root = CreateInputRoot(impl.Object, target, renderer.Object);
            var device = new TouchDevice();
            var touchBegin = new RawPointerEventArgs(device, 0, root.PresentationSource, RawPointerEventType.TouchBegin, new Point(50, 50), RawInputModifiers.None)
            {
                RawPointerId = 1
            };
            var touchEnd = new RawPointerEventArgs(device, 1, root.PresentationSource, RawPointerEventType.TouchEnd, new Point(50, 50), RawInputModifiers.None)
            {
                RawPointerId = 1
            };

            SetHit(renderer, target);

            impl.Object.Input!(touchBegin);

            Assert.True(target.IsPressed);
            Assert.False(target.IsChecked ?? false);

            command.IsEnabled = false;

            Assert.False(target.IsEffectivelyEnabled);

            impl.Object.Input!(touchEnd);

            Assert.False(target.IsChecked ?? false);
        }

        private IDisposable UnitTestApp(TimeSpan doubleClickTime = new TimeSpan())
        {
            var unitTestApp = UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager()));
            var iSettingsMock = new Mock<IPlatformSettings>();
            iSettingsMock.Setup(x => x.GetDoubleTapTime(It.IsAny<PointerType>())).Returns(doubleClickTime);
            iSettingsMock.Setup(x => x.GetDoubleTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            iSettingsMock.Setup(x => x.GetTapSize(It.IsAny<PointerType>())).Returns(new Size(16, 16));
            AvaloniaLocator.CurrentMutable.BindToSelf(this)
               .Bind<IPlatformSettings>().ToConstant(iSettingsMock.Object);
            return unitTestApp;
        }
        
        private static void SendXTouchContactsWithIds(IInputManager inputManager, TouchDevice device, IInputRoot root, RawPointerEventType type, params long[] touchPointIds)
        {
            for (int i = 0; i < touchPointIds.Length; i++)
            {
                inputManager.ProcessInput(new RawPointerEventArgs(device, 0,
                                                              root,
                                                              type,
                                                              new Point(0, 0),
                                                              RawInputModifiers.None)
                {
                    RawPointerId = touchPointIds[i]
                });
            }
        }


        private static void TapOnce(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
        {
            Press(inputManager, device, root, timestamp, touchPointId);
            Release(inputManager, device, root, timestamp, touchPointId);
        }

        private static void Release(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
        {
            inputManager.ProcessInput(new RawPointerEventArgs(device, timestamp,
                                                root,
                                                RawPointerEventType.TouchEnd,
                                                new Point(0, 0),
                                                RawInputModifiers.None)
            {
                RawPointerId = touchPointId
            });
        }

        private static void Press(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
        {
            inputManager.ProcessInput(new RawPointerEventArgs(device, timestamp,
                                               root,
                                               RawPointerEventType.TouchBegin,
                                               new Point(0, 0),
                                               RawInputModifiers.None)
            {
                RawPointerId = touchPointId
            });
        }

        private sealed class TestCommand : ICommand
        {
            private bool _enabled;
            private EventHandler? _canExecuteChanged;

            public TestCommand(bool enabled)
            {
                _enabled = enabled;
            }

            public bool IsEnabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value)
                    {
                        return;
                    }

                    _enabled = value;
                    _canExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public event EventHandler? CanExecuteChanged
            {
                add => _canExecuteChanged += value;
                remove => _canExecuteChanged -= value;
            }

            public bool CanExecute(object? parameter) => _enabled;

            public void Execute(object? parameter)
            {
            }
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {

        }
    }
}
