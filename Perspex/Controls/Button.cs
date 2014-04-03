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
            this.GetObservable(PointerPressedEvent).Subscribe(e =>
            {
                this.Classes.Add(":pressed");
            });

            this.GetObservable(PointerReleasedEvent).Subscribe(e =>
            {
                this.Classes.Remove(":pressed");
            });
        }
    }
}
