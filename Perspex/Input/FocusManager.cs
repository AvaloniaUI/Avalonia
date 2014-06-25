// -----------------------------------------------------------------------
// <copyright file="IFocusManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public class FocusManager : IFocusManager
    {
        public IFocusable Current
        {
            get;
            private set;
        }

        public void Focus(IFocusable control)
        {
            if (this.Current != null)
            {
                this.Current.IsFocused = false;
            }

            if (control != null)
            {
                control.IsFocused = true;
                this.Current = control;
            }
        }
    }
}
