// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    public class ItemsRepeaterElementPreparedEventArgs
    {
        internal ItemsRepeaterElementPreparedEventArgs(IControl element, int index)
        {
            Element = element;
            Index = index;
        }

        public IControl Element { get; private set; }

        public int Index { get; private set; }

        internal void Update(IControl element, int index)
        {
            Element = element;
            Index = index;
        }
    }
}
