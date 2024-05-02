using System;
using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    public enum RawKeyEventType
    {
        KeyDown,
        KeyUp
    }

    [PrivateApi]
    public class RawKeyEventArgs : RawInputEventArgs
    {
        [Obsolete("Use the overload that takes a physical key and key symbol instead.")]
        public RawKeyEventArgs(
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key,
            RawInputModifiers modifiers)
            : this(device, timestamp, root, type, key, modifiers, PhysicalKey.None, KeyDeviceType.Keyboard, null)
        {
            Key = key;
            Type = type;
            Modifiers = modifiers;
        }

        public RawKeyEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key,
            RawInputModifiers modifiers,
            PhysicalKey physicalKey,
            string? keySymbol)
            : this(device, timestamp, root, type, key, modifiers, physicalKey, KeyDeviceType.Keyboard, keySymbol) { }

        public RawKeyEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key,
            RawInputModifiers modifiers,
            PhysicalKey physicalKey,
            KeyDeviceType keyDeviceType,
            string? keySymbol)
            : base(device, timestamp, root)
        {
            Key = key;
            Modifiers = modifiers;
            Type = type;
            PhysicalKey = physicalKey;
            KeySymbol = keySymbol;
            KeyDeviceType = keyDeviceType;
        }

        public Key Key { get; set; }

        public RawInputModifiers Modifiers { get; set; }

        public RawKeyEventType Type { get; set; }

        public PhysicalKey PhysicalKey { get; set; }

        public KeyDeviceType KeyDeviceType { get; set; }

        public string? KeySymbol { get; set; }
    }
}
