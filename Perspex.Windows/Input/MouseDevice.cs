// -----------------------------------------------------------------------
// <copyright file="MouseDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows.Input
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Input;

    public class MouseDevice : IMouseDevice
    {
        public IVisual Captured
        {
            get;
            private set;
        }

        public IDisposable Capture(IVisual visual)
        {
            this.Captured = visual;
            return Disposable.Create(() => this.Captured = null);
        }
    }
}
