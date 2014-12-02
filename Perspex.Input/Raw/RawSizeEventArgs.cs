// -----------------------------------------------------------------------
// <copyright file="RawInputEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input.Raw
{
    using System;

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
