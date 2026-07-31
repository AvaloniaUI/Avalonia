using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawDragEvent : RawInputEventArgs
    {
        public Point Location { get; set; }

        public IDataTransfer DataTransfer { get; }

        public DragDropEffects Effects { get; set; }

        public RawDragEventType Type { get; }

        public KeyModifiers KeyModifiers { get; }

        public RawDragEvent(
            IDragDropDevice inputDevice,
            RawDragEventType type,
            IInputRoot root,
            Point location,
            IDataTransfer dataTransfer,
            DragDropEffects effects,
            RawInputModifiers modifiers)
            : base(inputDevice, 0, root)
        {
            Type = type;
            Location = location;
            DataTransfer = dataTransfer;
            Effects = effects;
            KeyModifiers = modifiers.ToKeyModifiers();
        }
    }
}
