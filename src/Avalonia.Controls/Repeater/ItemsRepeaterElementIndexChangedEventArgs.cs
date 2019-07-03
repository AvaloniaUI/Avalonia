// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsRepeater.ElementIndexChanged"/> event.
    /// </summary>
    public class ItemsRepeaterElementIndexChangedEventArgs : EventArgs
    {
        internal ItemsRepeaterElementIndexChangedEventArgs(IControl element, int newIndex, int oldIndex)
        {
            Element = element;
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }

        /// <summary>
        /// Get the element for which the index changed.
        /// </summary>
        public IControl Element { get; private set; }

        /// <summary>
        /// Gets the index of the element after the change.
        /// </summary>
        public int NewIndex { get; private set; }

        /// <summary>
        /// Gets the index of the element before the change.
        /// </summary>
        public int OldIndex { get; private set; }

        internal void Update(IControl element, int newIndex, int oldIndex)
        {
            Element = element;
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }
    }
}
