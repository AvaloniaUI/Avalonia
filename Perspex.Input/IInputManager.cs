// -----------------------------------------------------------------------
// <copyright file="IInputManager.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Input.Raw;
    using Perspex.Layout;

    public interface IInputManager
    {
        IObservable<RawInputEventArgs> RawEventReceived { get; }

        void Process(RawInputEventArgs e);

        void SetPointerOver(IPointerDevice device, IVisual visual, Point p);
    }
}
