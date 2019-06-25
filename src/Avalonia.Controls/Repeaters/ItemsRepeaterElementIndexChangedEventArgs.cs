// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls.Repeaters
{
    public class ItemsRepeaterElementIndexChangedEventArgs : EventArgs
    {
        internal ItemsRepeaterElementIndexChangedEventArgs(IControl element, int newIndex, int oldIndex)
        {
            Element = element;
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }

        public IControl Element { get; private set; }

        public int NewIndex { get; private set; }

        public int OldIndex { get; private set; }

        internal void Update(IControl element, int newIndex, int oldIndex)
        {
            Element = element;
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }
    }
}
