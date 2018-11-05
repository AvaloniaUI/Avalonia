// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Input.Raw
{
    [Obsolete("Use RawTextInputHandlerSelectionEventArgs")]
    public class RawTextInputEventArgs : RawInputEventArgs
    {
        public string Text { get; set; }

        public RawTextInputEventArgs(IKeyboardDevice device, uint timestamp, string text) : base(device, timestamp)
        {
            Text = text;
        }
    }
}
