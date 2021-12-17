using System;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
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
                var inputManager = InputManager.Instance;
                var root = new TestRoot();
                var touchDevice = new TouchDevice();

                var isTapped = false;
                var executedTimes = 0;
                root.Tapped += (a, e) =>
                {
                    isTapped = true;
                    executedTimes++;
                };
                TapOnce(inputManager, touchDevice, root);
                Assert.True(isTapped);
                Assert.Equal(1, executedTimes);
            }
        }
        private static void TapOnce(IInputManager inputManager, TouchDevice device, IInputRoot root)
        {
            inputManager.ProcessInput(new RawTouchEventArgs(device, 0,
                                               root,
                                               RawPointerEventType.TouchBegin,
                                               new Point(0, 0),
                                               RawInputModifiers.None,
                                               1));
            inputManager.ProcessInput(new RawTouchEventArgs(device, 0,
                                                root,
                                                RawPointerEventType.TouchEnd,
                                                new Point(0, 0),
                                                RawInputModifiers.None,
                                                1));
        }
    }
}
