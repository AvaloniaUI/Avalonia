// -----------------------------------------------------------------------
// <copyright file="IPointerDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;

    public interface IPointerDevice : IInputDevice
    {
        Interactive Captured { get; }

        void Capture(Interactive visual);
    }
}
