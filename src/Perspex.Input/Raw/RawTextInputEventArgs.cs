// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Input.Raw
{
    public class RawTextInputEventArgs : RawInputEventArgs
    {
        public RawTextInputEventArgs(
            IKeyboardDevice device,
            IInputRoot root,
            uint timestamp, 
            string text) 
            : base(device, root, timestamp)
        {
            Text = text;
        }

        public string Text { get; set; }
    }
}
