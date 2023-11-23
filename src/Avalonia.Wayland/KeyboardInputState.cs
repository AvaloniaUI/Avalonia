using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Wayland
{
    internal struct KeyboardInputState
    {
        public uint Time;
        public uint KeyCode;
        public XkbKey Sym;
        public RawKeyEventType EventType;
        public Key Key;
        public PhysicalKey PhysicalKey;
        public string? Text;
    }
}
