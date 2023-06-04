using System;
using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawDragEvent : RawInputEventArgs
    {
        public Point Location { get; set; }
        public IDataObject Data { get; }
        public DragDropEffects Effects { get; set; }
        public RawDragEventType Type { get; }
        public KeyModifiers KeyModifiers { get; }

        public RawDragEvent(IDragDropDevice inputDevice, RawDragEventType type, 
            IInputRoot root, Point location, IDataObject data, DragDropEffects effects, RawInputModifiers modifiers)
            :base(inputDevice, 0, root)
        {
            Type = type;
            Location = location;
            Data = data;
            Effects = effects;
            KeyModifiers = modifiers.ToKeyModifiers();
        }
    }
}
