using System;

namespace Avalonia.Controls.Repeaters
{
    public class ItemsRepeaterElementClearingEventArgs : EventArgs
    {
        internal ItemsRepeaterElementClearingEventArgs(IControl element) => Element = element;
        public IControl Element { get; private set; }
        internal void Update(IControl element) => Element = element;
    }
}
