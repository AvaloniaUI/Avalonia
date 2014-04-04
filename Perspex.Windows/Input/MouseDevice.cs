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
        public Interactive Captured
        {
            get;
            private set;
        }

        public void Capture(Interactive visual)
        {
            this.Captured = visual;
        }
    }
}
