// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Input.Raw
{
    public class RawSizeEventArgs : EventArgs
    {
        public RawSizeEventArgs(Size size)
        {
            this.Size = size;
        }

        public RawSizeEventArgs(double width, double height)
        {
            this.Size = new Size(width, height);
        }

        public Size Size { get; private set; }
    }
}
