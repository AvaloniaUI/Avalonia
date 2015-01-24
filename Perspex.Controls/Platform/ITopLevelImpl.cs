// -----------------------------------------------------------------------
// <copyright file="IWindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Input.Raw;

    public interface ITopLevelImpl : IDisposable
    {
        Size ClientSize { get; set; }

        IPlatformHandle Handle { get; }

        Action Activated { get; set; }

        Action Closed { get; set; }

        Action Deactivated { get; set; }

        Action<RawInputEventArgs> Input { get; set; }

        Action<Rect, IPlatformHandle> Paint { get; set; }

        Action<Size> Resized { get; set; }

        void Invalidate(Rect rect);

        void SetOwner(TopLevel owner);

        Point PointToScreen(Point point);
    }
}
