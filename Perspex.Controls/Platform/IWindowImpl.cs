// -----------------------------------------------------------------------
// <copyright file="IWindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using Perspex.Controls;
    using Perspex.Input.Raw;

    public interface IWindowImpl
    {
        event EventHandler Activated;

        event EventHandler Closed;

        event EventHandler<RawInputEventArgs> Input;

        event EventHandler<RawSizeEventArgs> Resized;

        Size ClientSize { get; }

        IPlatformHandle Handle { get; }

        void SetTitle(string title);

        void SetOwner(Window window);

        void Show();
    }
}
