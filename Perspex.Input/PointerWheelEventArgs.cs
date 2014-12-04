// -----------------------------------------------------------------------
// <copyright file="PointerWheelEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public class PointerWheelEventArgs : PointerEventArgs
    {
        public Vector Delta { get; set; }
    }
}
