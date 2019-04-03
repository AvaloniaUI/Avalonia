using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Avalonia.LinuxFramebuffer
{
    public class EvDevAxisInfo
    {
        public int Maximum { get; set; }
        public int Minimum { get; set; }
    }

    internal unsafe class EvDevDevice
    {
        private static readonly Lazy<List<EvDevDevice>> AllInputDevices = new Lazy<List<EvDevDevice>>(()
            => OpenMouseDevices());

        private IntPtr _dev;

        public EvDevDevice(int fd, IntPtr dev)
        {
            Fd = fd;
            _dev = dev;
            Name = Marshal.PtrToStringAnsi(NativeUnsafeMethods.libevdev_get_name(_dev));

            //Loading EventTypes
            var eventTypes = new List<EvType>();
            foreach (EvType type in Enum.GetValues(typeof(EvType)))
            {
                if (NativeUnsafeMethods.libevdev_has_event_type(dev, type) != 0)
                    eventTypes.Add(type);
            }
            EventTypes = eventTypes.AsReadOnly();

            var ptr = NativeUnsafeMethods.libevdev_get_abs_info(dev, (int)AbsAxis.ABS_X);
            if (ptr != null)
                AbsX = *ptr;
            ptr = NativeUnsafeMethods.libevdev_get_abs_info(dev, (int)AbsAxis.ABS_Y);
            if (ptr != null)
                AbsY = *ptr;

            if (EventTypes.Contains(EvType.EV_REL))
                Type = EvDevDeviceType.Mouse;
            else if (EventTypes.Contains(EvType.EV_ABS))
                Type = EvDevDeviceType.Touch;
            else
                Type = EvDevDeviceType.Unknown;
        }

        public static IReadOnlyList<EvDevDevice> InputDevices => AllInputDevices.Value;

        public input_absinfo? AbsX { get; }

        public input_absinfo? AbsY { get; }

        public IReadOnlyList<EvType> EventTypes { get; }

        public int Fd { get; }

        public string Name { get; }
        public EvDevDeviceType Type { get; private set; }

        // public bool IsMouse => EventTypes.Contains(EvType.EV_REL) || EventTypes.Contains(EvType.EV_ABS);
        public static EvDevDevice Open(string device)
        {
            var fd = NativeUnsafeMethods.open(device, 2048, 0);
            if (fd <= 0)
                throw new Exception($"Unable to open {device} code {Marshal.GetLastWin32Error()}");
            IntPtr dev;
            var rc = NativeUnsafeMethods.libevdev_new_from_fd(fd, out dev);
            if (rc < 0)
            {
                NativeUnsafeMethods.close(fd);
                throw new Exception($"Unable to initialize evdev for {device} code {Marshal.GetLastWin32Error()}");
            }
            return new EvDevDevice(fd, dev);
        }

        public input_event? NextEvent()
        {
            input_event ev;
            if (NativeUnsafeMethods.libevdev_next_event(_dev, 2, out ev) == 0)
                return ev;
            return null;
        }

        private static List<EvDevDevice> OpenMouseDevices()
        {
            var rv = new List<EvDevDevice>();
            foreach (var dev in Directory.GetFiles("/dev/input", "event*").Select(Open))
            {
                if (dev.Type == EvDevDeviceType.Unknown)
                {
                    NativeUnsafeMethods.close(dev.Fd);
                    Console.WriteLine("# Mouse-Device NOT added: " + dev.Name);
                }
                else
                {
                    rv.Add(dev);
                    Console.WriteLine($"# Mouse-Device added: [Name: {dev.Name}, Type: {dev.Type}]");
                }
            }
            return rv;
        }
    }

    public enum EvDevDeviceType
    {
        Unknown,
        Mouse,
        Touch,
    }
}
