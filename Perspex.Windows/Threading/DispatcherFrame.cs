// -----------------------------------------------------------------------
// <copyright file="DispatcherFrame.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows.Threading
{
    public class DispatcherFrame
    {
        public DispatcherFrame()
        {
            this.Continue = true;
            this.ExitOnRequest = true;
        }

        public bool Continue { get; set; }

        internal bool ExitOnRequest { get; set; }

        internal Dispatcher Running { get; set; }

        internal DispatcherFrame ParentFrame { get; set; }
    }
}
