// -----------------------------------------------------------------------
// <copyright file="DispatcherFrame.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    public class DispatcherFrame
    {
        public DispatcherFrame()
        {
            this.Continue = true;
            this.ExitOnRequest = true;
        }

        public bool Continue { get; set; }

        public bool ExitOnRequest { get; set; }

        public Dispatcher Running { get; set; }

        public DispatcherFrame ParentFrame { get; set; }
    }
}
