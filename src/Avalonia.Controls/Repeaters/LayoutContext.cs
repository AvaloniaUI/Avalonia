using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    public class LayoutContext : AvaloniaObject
    {
        public object LayoutState { get; set; }

        protected virtual object LayoutStateCore { get; set; }
    }
}
