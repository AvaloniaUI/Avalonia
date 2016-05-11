// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Input.Raw
{
    public class RawSizeEventArgs : EventArgs
    {
        public RawSizeEventArgs(Size size)
        {
            Size = size;
        }

        public RawSizeEventArgs(double width, double height)
        {
            Size = new Size(width, height);
        }

        public Size Size { get; private set; }
    }
}
