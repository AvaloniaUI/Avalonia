using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Input;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Input
{
    public class WindowsGamepadManager : GamepadManager
    {
        private const int OPEN_EXISTING = 3;
        private const int FILE_SHARE_READ = 0x00000001;
        private const int FILE_SHARE_WRITE = 0x00000002;
        private const uint GENERIC_READ = (0x80000000);
        private const int GENERIC_WRITE = (0x40000000);
        private const ushort HID_USAGE_PAGE_GENERIC = ((ushort)(0x01));
        private const ushort HID_USAGE_GENERIC_GAMEPAD = ((ushort)(0x05));
        private const ushort HID_USAGE_GENERIC_JOYSTICK = ((ushort)(0x04));
        private const ushort HID_USAGE_GENERIC_MULTI_AXIS_CONTROLLER = ((ushort)(0x08));
        private const nint GIDC_ARRIVAL = 1;
        private const nint GIDC_REMOVAL = 2;
        private const int RID_INPUT = 0x10000003;
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RIDEV_DEVNOTIFY = 0x00002000;
        private const int RIDI_DEVICENAME = 0x20000007;
        private const int XINPUT_MAX_DEVICES = 4;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private SimpleWindow? _simpleWindow;
        private Thread _messagePumpThread;
        private List<InternalGamepadData> _knownDevices = new();
        private int _currentXInputIndex = 0;

        public double GamepadAnalogStickDeadZone { get; set; } = 0.08;

        public WindowsGamepadManager()
        {
            Instance = this;
            _messagePumpThread = new Thread(MessagePumpThreadStart);
            _messagePumpThread.SetApartmentState(ApartmentState.STA);
            _messagePumpThread.Name = $"{nameof(WindowsGamepadManager)}::{nameof(MessagePumpThreadStart)}";
            _messagePumpThread.IsBackground = true;
            _messagePumpThread.Start();
        }

        private unsafe void MessagePumpThreadStart()
        {
            // I noticed this occurring way too fast, so slow it down a bit to give user code some time to register their event-handlers
            Thread.Sleep(500);
            _simpleWindow = new(GamepadWndProc);

            RAWINPUTDEVICE* rids = stackalloc RAWINPUTDEVICE[4];
            Span<RAWINPUTDEVICE> spanRids = new Span<RAWINPUTDEVICE>(rids, 4);

            for (int i = 0; i < 4; i++)
            {
                spanRids[i].usUsagePage = HID_USAGE_PAGE_GENERIC;
                // if you add these two options together, then you get input even out of focus 
                spanRids[i].dwFlags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY;
                spanRids[i].hwndTarget = _simpleWindow.Handle;
            }

            spanRids[0].usUsage = HID_USAGE_GENERIC_GAMEPAD;
            spanRids[1].usUsage = HID_USAGE_GENERIC_JOYSTICK;
            spanRids[2].usUsage = HID_USAGE_GENERIC_MULTI_AXIS_CONTROLLER;

            var registerReturn = UnmanagedMethods.RegisterRawInputDevices(rids, 3, (uint)sizeof(RAWINPUTDEVICE));
            if (registerReturn == 0x0)
            {
                // TODO: Log failure to register raw input devices 
            }

            // for XInput, which says to poll at 8ms or 125 Hz
            // null for proc so it posts the message to our WndProc for convenience
            UnmanagedMethods.SetTimer(_simpleWindow.Handle, IntPtr.Zero, 8, null);

            while (true)
            {
                if (UnmanagedMethods.GetMessage(out var msg, _simpleWindow.Handle, 0, 0) != 0x0)
                {
                    UnmanagedMethods.TranslateMessage(ref msg);
                    UnmanagedMethods.DispatchMessage(ref msg);
                }
            }
        }

        private unsafe IntPtr GamepadWndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lparam)
        {
            UnmanagedMethods.WindowsMessage msg = (UnmanagedMethods.WindowsMessage)message;
            switch (msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_INPUT:
                    {
                        RAWINPUT rwInput = default;
                        uint rwInputSize = (uint)sizeof(RAWINPUT);
                        // Turns out this isn't an HRESULT (haha)
                        // See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdata
                        var result = UnmanagedMethods.GetRawInputData(lparam, RID_INPUT, (IntPtr)(&rwInput), &rwInputSize, (uint)(sizeof(RAWINPUTHEADER)));
                        if (result == unchecked((uint)-1)) // yeah, apparently it is just -1 but a uint 
                        {
                            // an error has occurred 
                            // TODO: Log error
                        }
                        else
                        {
                            DispatchRawInputStateChange(rwInput);
                        }
                    }
                    break;
                case UnmanagedMethods.WindowsMessage.WM_INPUT_DEVICE_CHANGE:
                    {
                        // info: the LPARAM is the HANDLE to the raw input device
                        if (wParam == GIDC_ARRIVAL)
                        {
                            // Process device being added / initialized
                            RawInputDeviceAdded(lparam);
                        }
                        else if (wParam == GIDC_REMOVAL)
                        {
                            // Process device being removed / disconnected 
                            RawInputDeviceRemoved(lparam);
                        }
                    }
                    break;
                case UnmanagedMethods.WindowsMessage.WM_TIMER:
                    {
                        DoXInput();
                    }
                    break;
            }

            return UnmanagedMethods.DefWindowProc(hwnd, message, wParam, lparam);
        }

        private unsafe void RawInputDeviceAdded(IntPtr deviceHandle)
        {
            Console.WriteLine(deviceHandle);
            Trace.WriteLine(deviceHandle);

            // get the device ID 
            uint nameSize = 0;
            UnmanagedMethods.GetRawInputDeviceInfoW(deviceHandle, RIDI_DEVICENAME, null, &nameSize);
            // this stack allocation is fine since the current thread is NOT the Dispatcher thread
            // AND we're very shallow in our calling stack
            char* pDeviceName = stackalloc char[(int)nameSize];
            UnmanagedMethods.GetRawInputDeviceInfoW(deviceHandle, RIDI_DEVICENAME, pDeviceName, &nameSize);
            // note that deviceName is the really long //?/HID style name 
            // See https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdeviceinfow
            // And windows guarantees that the same device will have the same device-name 
            string deviceName = new string(pDeviceName);

            InternalGamepadData? data = null;
            for (int i = 0; i < _knownDevices.Count; i++)
            {
                if (string.Equals(_knownDevices[i].Id, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    data = _knownDevices[i];
                }
            }
            if (data is null)
            {
                var handle = UnmanagedMethods.CreateFileW((ushort*)pDeviceName, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, null, OPEN_EXISTING, 0x0, IntPtr.Zero);
                if (handle == IntPtr.Zero || handle == INVALID_HANDLE_VALUE)
                {
                    return;
                }

                // if you're wondering where 2046 comes from, it's half of 4092 
                // and if you're wondering where 4092 comes from 
                // https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/hidsdi/nf-hidsdi-hidd_getproductstring
                char* pwstrProductString = stackalloc char[2046];
                if (UnmanagedMethods.HidD_GetProductString(handle, pwstrProductString, 4092) == 0)
                {
                    // TODO: Log failure
                    UnmanagedMethods.CloseHandle(handle);
                    return;
                }
                string humanName = new string(pwstrProductString);
                // okay this looks weird, but apparently that's just how it's done. 🤷‍
#if NETSTANDARD2_0
                bool isXInputDevice = deviceName.IndexOf("IG_", StringComparison.OrdinalIgnoreCase) != -1;
#else
                bool isXInputDevice = deviceName.Contains("IG_", StringComparison.OrdinalIgnoreCase);
#endif

                UnmanagedMethods.CloseHandle(handle);
                data = new InternalGamepadData(_knownDevices.Count, deviceName, humanName, isXInputDevice);
                data.LastHandle = deviceHandle;
                if (isXInputDevice)
                {
                    data.XInputDeviceIndex = _currentXInputIndex;
                    _currentXInputIndex++;
                    if (_currentXInputIndex > 3)
                    {
                        // you know, theoretically someone could have like, thousands of xbox controllers
                        // and connect four of them up, and then disconnect them all and then start 
                        // connecting other controllers 
                        // and we're supposed to keep the same xinput index for devices known to us
                        // yet also xinput doesn't give us identifying information about each device
                        // So this is an impossible problem, and we're trying our best
                        _currentXInputIndex = 0;
                    }
                }
                _knownDevices.Add(data);

                Trace.WriteLine($"New Device! [{humanName}] is {(isXInputDevice ? "" : "NOT ")}an XInput device!");
                data.EventTracking = GamepadEventArgs.GamepadEventType.Initialized;
            }
            else
            {
                data.EventTracking = GamepadEventArgs.GamepadEventType.Reconnected;
                data.LastHandle = deviceHandle;
                Trace.WriteLine($"Recognized Device! [{data.HumanName}] is {(data.IsXInputDevice ? "" : "NOT ")} an XInput device!");
            }

            data.IsConnected = true;

            PushUpdate(data);
        }

        private void RawInputDeviceRemoved(IntPtr deviceHandle)
        {
            for (int i = 0; i < _knownDevices.Count; i++)
            {
                var data = _knownDevices[i];
                if (data.LastHandle == deviceHandle)
                {
                    data.LastState = default;
                    data.EventTracking = GamepadEventArgs.GamepadEventType.Disconnected;
                    data.IsConnected = false;
                    PushUpdate(data);
                }
            }
        }

        private void DispatchRawInputStateChange(RAWINPUT rawInputEvent)
        {
            for (int i = 0; i < _knownDevices.Count; i++)
            {
                var data = _knownDevices[i];
                if (rawInputEvent.header.hDevice == data.LastHandle)
                {
                    if (data.IsXInputDevice)
                    {
                        return;
                    }
                    else
                    {
                        // TODO: Mapping and bindings operations
                    }
                }
            }
        }

        private unsafe void DoXInput()
        {
            XINPUT_STATE state = default;
            for (uint xinputIndex = 0; xinputIndex < XINPUT_MAX_DEVICES; xinputIndex++)
            {
                uint status = UnmanagedMethods.XINPUT_GET_STATE(xinputIndex, &state);
                if (status == UnmanagedMethods.ERROR_DEVICE_NOT_CONNECTED)
                {
                    // uninterested, raw-input will tell us when the device is disconnected/. 
                }
                else if (status == 0x0) // ERROR_SUCCESS :) 
                {
                    // report device status update
                    for (int ii = 0; ii < _knownDevices.Count; ii++)
                    {
                        var data = _knownDevices[ii];
                        if (data.XInputDeviceIndex == xinputIndex)
                        {
                            if (data.XInputDwPacketNumber != state.dwPacketNumber)
                            {
                                data.XInputDwPacketNumber = state.dwPacketNumber;
                                data.EventTracking = GamepadEventArgs.GamepadEventType.StateChange;
                                var gamepadState = new GamepadState();
                                gamepadState.LeftAnalogStick = DualAxisHandle(state.Gamepad.sThumbLX, state.Gamepad.sThumbLY, GamepadAnalogStickDeadZone);
                                gamepadState.RightAnalogStick = DualAxisHandle(state.Gamepad.sThumbRX, state.Gamepad.sThumbRY, GamepadAnalogStickDeadZone);
                                var buttons = state.Gamepad.wButtons;

                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button0, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button1, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button2, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button3, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button4, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button5, buttons);
                                // NOTE - Buttons 6 and 7 are left-trigger and right-trigger, and are analog on XInput 
                                gamepadState.SetButtonState(GamepadButton.Button6, GetNewButtonState(gamepadState.GetButtonState(GamepadButton.Button6), state.Gamepad.bLeftTrigger / 255f));
                                gamepadState.SetButtonState(GamepadButton.Button7, GetNewButtonState(gamepadState.GetButtonState(GamepadButton.Button7), state.Gamepad.bRightTrigger / 255f));
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button8, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button9, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button10, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button11, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button12, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button13, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button14, buttons);
                                SetButtonFromXInput(ref gamepadState, GamepadButton.Button15, buttons);
                                data.LastState = gamepadState;
                                PushUpdate(data);
                            }
                        }
                    }
                }
            }
        }

        private void PushUpdate(InternalGamepadData data)
        {
            PushGamepadEvent(
                new GamepadEventArgs()
                {
                    Connected = data.IsConnected,
                    Device = data.Index,
                    HumanName = data.HumanName,
                    Id = data.Id,
                    Timestamp = DateTime.Now,
                    Source = this,
                    EventType = data.EventTracking,
                    State = data.LastState,
                }
            );
        }

        private class InternalGamepadData
        {
            public InternalGamepadData(int index, string id, string humanName, bool isXInputDevice)
            {
                Index = index;
                Id = id;
                HumanName = humanName;
                IsXInputDevice = isXInputDevice;
            }
            public int Index { get; set; }
            public string Id { get; set; }
            public string HumanName { get; set; }
            public bool IsXInputDevice { get; set; }
            public bool IsConnected { get; set; }
            public GamepadEventArgs.GamepadEventType EventTracking { get; set; }
            public GamepadState LastState { get; set; }
            public DateTime Timestamp { get; set; }
            public IntPtr LastHandle { get; set; }
            public int XInputDeviceIndex { get; set; } = -1;
            public uint XInputDwPacketNumber { get; set; }
        }

        private ushort XInputFlagFromGamepadButton(GamepadButton button)
        {
            switch (button)
            {
                case GamepadButton.Button0:
                    return 4096;
                case GamepadButton.Button1:
                    return 8192;
                case GamepadButton.Button2:
                    return 16384;
                case GamepadButton.Button3:
                    return 32768;
                case GamepadButton.Button4:
                    return 256;
                case GamepadButton.Button5:
                    return 512;
                case GamepadButton.Button6:
                case GamepadButton.Button7:
                default:
                    throw new Exception("Okay, these aren't XInput buttons, sorry! Programmer error!");
                case GamepadButton.Button8:
                    return 32;
                case GamepadButton.Button9:
                    return 16;
                case GamepadButton.Button10:
                    return 64;
                case GamepadButton.Button11:
                    return 128;
                case GamepadButton.Button12:
                    return 1;
                case GamepadButton.Button13:
                    return 2;
                case GamepadButton.Button14:
                    return 4;
                case GamepadButton.Button15:
                    return 8;
            }
        }

        private void SetButtonFromXInput(ref GamepadState target, GamepadButton button, ushort xinputButtons)
        {
            var flag = XInputFlagFromGamepadButton(button);
            var previousState = target.GetButtonState(button);
            if (IsMatch(xinputButtons, flag))
            {
                // we know that the button is pressed 
                target.SetButtonState(button, new ButtonState() 
                { 
                    JustPressed = previousState.Pressed ? false : true,
                    Value = 1.0d,
                    JustReleased = false,
                    Touched = true,
                    Pressed = true,
                }
                );
            }
            else
            {
                // we know that the button is released 
                target.SetButtonState(button, new ButtonState()
                {
                    JustPressed = false,
                    Value = 0.0d,
                    JustReleased = previousState.Pressed ? true : false,
                    Touched = false,
                    Pressed = false,
                }
                );
            }
        }

        private ButtonState GetNewButtonState(ButtonState previousState, double newValue)
        {
            if (newValue > 0)
            {
                // we know that the button is at least touched 
                return new ButtonState()
                {
                    JustPressed = previousState.Pressed ? false : true,
                    Value = newValue,
                    JustReleased = false,
                    Touched = true,
                    Pressed = newValue > 0.5,
                };
            }
            else
            {
                // we know that the button is released 
                return new ButtonState()
                {
                    JustPressed = false,
                    Value = 0.0d,
                    JustReleased = previousState.Pressed ? true : false,
                    Touched = false,
                    Pressed = false,
                };
            }
        }

        private bool IsMatch(ushort buttonState, ushort stateToCheck) => (buttonState & stateToCheck) == stateToCheck;

        private Vector DualAxisHandle(short xAxis, short yAxis, double deadZone)
        {
            double x = ShortAxisToDouble(xAxis);
            double y = ShortAxisToDouble(yAxis);

            double mag = Math.Sqrt(x * x + y * y);

            if (Math.Abs(mag) < deadZone)
            { return new(0.0f, 0.0f); }

            if (mag > 1.0f)
                mag = 1.0f;
            var directionRads = Math.Atan2(y, x);

            mag = Linear(mag, deadZone, 1.0f, 0.0f, 1.0f);

            return new(Math.Cos(directionRads) * mag, Math.Sin(directionRads) * mag);
        }

        private static double Linear(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }

        private static double ShortAxisToDouble(short axisValue)
        {
            return axisValue > 0 ? axisValue / (double)short.MaxValue : axisValue / (double)Math.Abs((double)short.MinValue);
        }
    }
}
