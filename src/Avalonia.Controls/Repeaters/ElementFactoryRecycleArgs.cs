using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    public sealed class ElementFactoryRecycleArgs : EventArgs
    {
        public IControl Element { get; set; }
        public IControl Parent { get; set; }
    }
}
