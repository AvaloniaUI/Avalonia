// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;

    public class Button : ContentControl
    {
        public Button()
        {
            this.PointerPressed += (s, e) =>
            {
                this.Classes.Add(":pressed");
                e.Device.Capture(this);
            };

            this.PointerReleased += (s, e) =>
            {
                e.Device.Capture(null);
                this.Classes.Remove(":pressed");
            };
        }
    }
}
