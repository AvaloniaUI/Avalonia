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
            KeyboardDevice device, 
            uint timestamp,
            RawKeyEventType type, 
            Key key, 
            string text)
            : base(device, timestamp)
        {
            this.Key = key;
            this.Type = type;
            this.Text = text;
        }

        public Key Key { get; set; }

        public string Text { get; set; }

        public RawKeyEventType Type { get; set; }
    }
}
