// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Input.Raw
{
    public class RawTextInputEventArgs : RawInputEventArgs
    {
        public RawTextInputEventArgs(
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            string text)
            : base(device, timestamp, root)
        {
            Text = text;
        }

        public string Text { get; set; }
    }
}
