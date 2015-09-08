// -----------------------------------------------------------------------
// <copyright file="RawKeyEventArgs.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    public enum RawKeyEventType
    {
        KeyDown,
        KeyUp
    }

    public class RawKeyEventArgs : RawInputEventArgs
    {
        public RawKeyEventArgs(
            IKeyboardDevice device,
            uint timestamp,
            RawKeyEventType type,
            Key key, ModifierKeys modifiers)
            : base(device, timestamp)
        {
            this.Key = key;
            this.Type = type;
            this.Modifiers = modifiers;
        }

        public Key Key { get; set; }

        public ModifierKeys Modifiers { get; set; }
        
        public RawKeyEventType Type { get; set; }
    }
}
