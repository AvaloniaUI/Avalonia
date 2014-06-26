// -----------------------------------------------------------------------
// <copyright file="IMouseDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public interface IMouseDevice : IPointerDevice
    {
        Point Position { get; }
    }
}
