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
        public RawKeyEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key,
            RawInputModifiers modifiers,
            PhysicalKey physicalKey,
            string? keySymbol,
            KeyDeviceType keyDeviceType = KeyDeviceType.Keyboard)
            : base(device, timestamp, root)
        {
            Type = type;
            Key = key;
            Modifiers = modifiers;
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
