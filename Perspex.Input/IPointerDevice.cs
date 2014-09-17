// -----------------------------------------------------------------------
// <copyright file="IPointerDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public interface IPointerDevice : IInputDevice
    {
        IInteractive Captured { get; }

        void Capture(IInteractive control);

        Point GetPosition(IVisual relativeTo);
    }
}
