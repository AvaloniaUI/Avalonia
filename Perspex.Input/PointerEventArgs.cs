// -----------------------------------------------------------------------
// <copyright file="PointerEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public class PointerEventArgs : RoutedEventArgs
    {
        public IPointerDevice Device { get; set; }

        public Point GetPosition(IVisual relativeTo)
        {
            return this.Device.GetPosition(relativeTo);
        }
    }
}
