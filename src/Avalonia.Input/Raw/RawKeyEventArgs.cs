// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Input.Raw
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
            ulong timestamp,
            RawKeyEventType type,
            Key key, InputModifiers modifiers)
            : base(device, timestamp)
        {
            Key = key;
            Type = type;
            Modifiers = modifiers;
        }

        public Key Key { get; set; }

        public InputModifiers Modifiers { get; set; }

        public RawKeyEventType Type { get; set; }
    }
}
