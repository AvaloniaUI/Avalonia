// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsRepeater.ElementPrepared"/> event.
    /// </summary>
    public class ItemsRepeaterElementPreparedEventArgs
    {
        internal ItemsRepeaterElementPreparedEventArgs(IControl element, int index)
        {
            Element = element;
            Index = index;
        }

        /// <summary>
        /// Gets the prepared element.
        /// </summary>
        public IControl Element { get; private set; }

        /// <summary>
        /// Gets the index of the item the element was prepared for.
        /// </summary>
        public int Index { get; private set; }

        internal void Update(IControl element, int index)
        {
            Element = element;
            Index = index;
        }
    }
}
