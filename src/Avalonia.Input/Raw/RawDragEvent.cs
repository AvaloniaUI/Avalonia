using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Input.Raw
{
    public class RawDragEvent : RawInputEventArgs
    {
        public IInputElement InputRoot { get; }
        public Point Location { get; }
        public IDataObject Data { get; }
        public DragDropEffects Effects { get; set; }
        public RawDragEventType Type { get; }

        public RawDragEvent(IDragDropDevice inputDevice, RawDragEventType type, 
            IInputElement inputRoot, Point location, IDataObject data, DragDropEffects effects)
            :base(inputDevice, 0)
        {
            Type = type;
            InputRoot = inputRoot;
            Location = location;
            Data = data;
            Effects = effects;
        }
    }
}