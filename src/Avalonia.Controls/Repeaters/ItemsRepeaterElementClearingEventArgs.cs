// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

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
