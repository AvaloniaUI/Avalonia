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
    using Perspex.Input.Raw;
    using Splat;

    public class MouseDevice : IMouseDevice
    {
        private static MouseDevice instance = new MouseDevice();

        public static MouseDevice Instance
        {
            get { return instance; }
        }

        public Interactive Captured
        {
            get;
            private set;
        }

        public Window CurrentWindow
        {
            get;
            set;
        }

        public Point Position
        {
            get;
            set;
        }

        public void Capture(Interactive visual)
        {
            this.Captured = visual;

            if (visual == null)
            {
                RawMouseEventArgs e = new RawMouseEventArgs(
                    this,
                    this.CurrentWindow,
                    RawMouseEventType.Move,
                    this.Position);

                IInputManager inputManager = Locator.Current.GetService<IInputManager>();
                inputManager.Process(e);
            }
        }
    }
}
