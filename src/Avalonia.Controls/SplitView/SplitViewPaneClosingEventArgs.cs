using System;

namespace Avalonia.Controls
{
    public class SplitViewPaneClosingEventArgs : EventArgs
    {
        public bool Cancel { get; set; }

        public SplitViewPaneClosingEventArgs(bool cancel)
        {
            Cancel = cancel;
        }
    }
}
