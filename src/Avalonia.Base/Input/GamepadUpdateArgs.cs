using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class GamepadInteractionEventArgs : RoutedEventArgs
    {
        public GamepadInteractionEventArgs(GamepadUpdateArgs args, object? source) : base(GamepadManager.GamepadInteractionEvent, source)
        {
            GamepadUpdateArgs = args;
            Source = source;
        }

        public GamepadUpdateArgs GamepadUpdateArgs { get; set; }
    }

    /// <summary>
    /// Update arguments for the Gamepad, if Handled will not be routed onto the Gui for navigation and interaction. 
    /// </summary>
    public class GamepadUpdateArgs : HandledEventArgs
    {
        /// <summary>
        /// Represents the "device index". A device that has been lost and reconnected will have the same index.
        /// </summary>
        public int Device { get; internal set; }
        /// <summary>
        /// Information about the vendor and/or manufacturer of the device. 
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// The human-readable name of the device. 
        /// </summary>
        public string HumanName { get; set; } = string.Empty;
        /// <summary>
        /// The State associated with the event / update. 
        /// </summary>
        public GamepadState State { get; set; } = new();
        /// <summary>
        /// True when the device is connected as of this update. 
        /// </summary>
        public bool Connected { get; set; }
        /// <summary>
        /// When the update occurred. 
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// The type of event
        /// </summary>
        public GamepadEventType EventType { get; set; }

        public enum GamepadEventType
        {
            Invalid = 0x0,
            Error = unchecked((int)0xFFFFFFFF),
            /// <summary>
            /// The first event in the stream of events from a gamepad
            /// </summary>
            Initialized = 0x1,
            /// <summary>
            /// The device was disconnected, and it has returned to us. 
            /// </summary>
            Reconnected = 0x2,
            /// <summary>
            /// The device has been lost in the void, probably unplugged. 
            /// </summary>
            Disconnected = 0x3,
            /// <summary>
            /// The device has changed state from an axis or button.
            /// </summary>
            StateChange = 0x4,
        }
    }

    public record struct GamepadState : IEquatable<GamepadState>
    {
        public Vector LeftAnalogStick { get; internal set; }
        public Vector RightAnalogStick { get; internal set; }
        private GamepadButtons _buttons;
        public ButtonState GetButtonState(GamepadButton button) 
        {
            return _buttons[(int)button];
        }
        public void SetButtonState(GamepadButton button, ButtonState state)
        {
            _buttons[(int)button] = state;
        }
    }

    public enum GamepadButton
    {
        /// <summary>
        /// X (PS5), A (Xbox), B (Switch)
        /// </summary>
        FaceButtonSouth,
        /// <summary>
        /// Circle (PS5), B (Xbox), A (Switch)
        /// </summary>
        FaceButtonEast,
        /// <summary>
        /// Square (PS5), X (Xbox), Y (Switch)
        /// </summary>
        FaceButtonWest,
        /// <summary>
        /// Triangle (PS5), Y (Xbox), X (Switch)
        /// </summary>
        FaceButtonNorth,
        /// <summary>
        /// L1 (PS5), LB (Xbox), L (Switch)
        /// </summary>
        LeftShoulder,
        /// <summary>
        /// R1 (PS5), RB (Xbox), R (Switch)
        /// </summary>
        RightShoulder,
        /// <summary>
        /// L2 (PS5), LT (Xbox), ZL (Switch)
        /// </summary>
        LeftTrigger,
        /// <summary>
        /// R2 (PS5), RT (Xbox), ZR (Switch)
        /// </summary>
        RightTrigger,
        /// <summary>
        /// Share (PS5), Back (Xbox), Minus (Switch)
        /// </summary>
        MiddleButtonLeft,
        /// <summary>
        /// Options (PS5), Start (Xbox), Plus (Switch)
        /// </summary>
        MiddleButtonRight,
        /// <summary>
        /// Left Analog Stick Depressed
        /// </summary>
        LeftStickButton,
        /// <summary>
        /// Right Analog Stick Depressed
        /// </summary>
        RightStickButton,
        /// <summary>
        /// Dpad Up
        /// </summary>
        DPadUp,
        /// <summary>
        /// Dpad Down
        /// </summary>
        DPadDown,
        /// <summary>
        /// Dpad Left
        /// </summary>
        DPadLeft,
        /// <summary>
        /// Dpad Right
        /// </summary>
        DPadRight,
    }

#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable CA1815 // should implement equals
    // this struct is a glorified fixed-buffer and these "unused fields" will be used. 
    public struct GamepadButtons
    {
        private ButtonState _0;
        private ButtonState _1;
        private ButtonState _2;
        private ButtonState _3;
        private ButtonState _4;
        private ButtonState _5;
        private ButtonState _6;
        private ButtonState _7;
        private ButtonState _8;
        private ButtonState _9;
        private ButtonState _10;
        private ButtonState _11;
        private ButtonState _12;
        private ButtonState _13;
        private ButtonState _14;
        private ButtonState _15;

        public ButtonState this[int index] 
        {
            get
            {
                if (index < 0)
                    throw new IndexOutOfRangeException();
                if (index > 15)
                    throw new IndexOutOfRangeException();
                // unsafe black magic because NS2.0 doesn't support MemoryMarshal.CreateSpan 
                return Unsafe.Add(ref _0, index);
            }
            set
            {
                if (index < 0)
                    throw new IndexOutOfRangeException();
                if (index > 15)
                    throw new IndexOutOfRangeException();
                // unsafe black magic because NS2.0 doesn't support MemoryMarshal.CreateSpan 
                Unsafe.Add(ref _0, index) = value;
            }
        }
    }
#pragma warning restore CA1823 // Avoid unused private fields
#pragma warning restore CA1815 // should implement equals

    public record struct ButtonState : IEquatable<ButtonState>
    {
        public bool Pressed { get; set; }
        public bool Touched { get; set; }
        public bool JustPressed { get; set; }
        public bool JustReleased { get; set; }
        public double Value { get; set; }
    }
}
