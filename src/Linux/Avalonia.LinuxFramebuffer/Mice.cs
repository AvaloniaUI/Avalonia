using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer
{
    public unsafe class Mice
    {
        private readonly double _width;
        private readonly double _height;
        private double _x;
        private double _y;

        public event Action<RawInputEventArgs> Event;

        public Mice(double width, double height)
        {
            _width = width;
            _height = height;
        }

        public void Start() => AvaloniaLocator.Current.GetService<IRuntimePlatform>().PostThreadPoolItem(Worker);

        private void Worker()
        {

            var mouseDevices = EvDevDevice.MouseDevices.Where(d => d.IsMouse).ToList();
            if (mouseDevices.Count == 0)
                return;
            var are = new AutoResetEvent(false);
            while (true)
            {
                try
                {
                    var rfds = new fd_set {count = mouseDevices.Count};
                    for (int c = 0; c < mouseDevices.Count; c++)
                        rfds.fds[c] = mouseDevices[c].Fd;
                    IntPtr* timeval = stackalloc IntPtr[2];
                    timeval[0] = new IntPtr(0);
                    timeval[1] = new IntPtr(100);
                    are.WaitOne(30);
                    foreach (var dev in mouseDevices)
                    {
                        while(true)
                        {
                            var ev = dev.NextEvent();
                            if (!ev.HasValue)
                                break;

                            LinuxFramebufferPlatform.Threading.Send(() => ProcessEvent(dev, ev.Value));
                        } 
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.ToString());
                }
            }
        }

        static double TranslateAxis(input_absinfo axis, int value, double max)
        {
            return (value - axis.minimum) / (double) (axis.maximum - axis.minimum) * max;
        }

        private void ProcessEvent(EvDevDevice device, input_event ev)
        {
            if (ev.type == (short)EvType.EV_REL)
            {
                if (ev.code == (short) AxisEventCode.REL_X)
                    _x = Math.Min(_width, Math.Max(0, _x + ev.value));
                else if (ev.code == (short) AxisEventCode.REL_Y)
                    _y = Math.Min(_height, Math.Max(0, _y + ev.value));
                else
                    return;
                Event?.Invoke(new RawMouseEventArgs(LinuxFramebufferPlatform.MouseDevice,
                    LinuxFramebufferPlatform.Timestamp,
                    LinuxFramebufferPlatform.TopLevel.InputRoot, RawMouseEventType.Move, new Point(_x, _y),
                    InputModifiers.None));
            }
            if (ev.type ==(int) EvType.EV_ABS)
            {
                if (ev.code == (short) AbsAxis.ABS_X && device.AbsX.HasValue)
                    _x = TranslateAxis(device.AbsX.Value, ev.value, _width);
                else if (ev.code == (short) AbsAxis.ABS_Y && device.AbsY.HasValue)
                    _y = TranslateAxis(device.AbsY.Value, ev.value, _height);
                else
                    return;
                Event?.Invoke(new RawMouseEventArgs(LinuxFramebufferPlatform.MouseDevice,
                    LinuxFramebufferPlatform.Timestamp,
                    LinuxFramebufferPlatform.TopLevel.InputRoot, RawMouseEventType.Move, new Point(_x, _y),
                    InputModifiers.None));
            }
            if (ev.type == (short) EvType.EV_KEY)
            {
                RawMouseEventType? type = null;
                if (ev.code == (ushort) EvKey.BTN_LEFT)
                    type = ev.value == 1 ? RawMouseEventType.LeftButtonDown : RawMouseEventType.LeftButtonUp;
                if (ev.code == (ushort)EvKey.BTN_RIGHT)
                    type = ev.value == 1 ? RawMouseEventType.RightButtonDown : RawMouseEventType.RightButtonUp;
                if (ev.code == (ushort) EvKey.BTN_MIDDLE)
                    type = ev.value == 1 ? RawMouseEventType.MiddleButtonDown : RawMouseEventType.MiddleButtonUp;
                if (!type.HasValue)
                    return;

                Event?.Invoke(new RawMouseEventArgs(LinuxFramebufferPlatform.MouseDevice,
                    LinuxFramebufferPlatform.Timestamp,
                    LinuxFramebufferPlatform.TopLevel.InputRoot, type.Value, new Point(_x, _y), default(InputModifiers)));
            }
        }
    }
}
