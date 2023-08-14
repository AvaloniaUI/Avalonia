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
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key,
            RawInputModifiers modifiers)
            : this(device, timestamp, root, type, key, modifiers, PhysicalKey.None, null)
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
            : base(device, timestamp, root)
        {
            Key = key;
            Modifiers = modifiers;
            Type = type;
            PhysicalKey = physicalKey;
            KeySymbol = keySymbol;
        }

        public Key Key { get; set; }

        public RawInputModifiers Modifiers { get; set; }

        public RawKeyEventType Type { get; set; }

        public PhysicalKey PhysicalKey { get; set; }

        public string? KeySymbol { get; set; }
    }
}
