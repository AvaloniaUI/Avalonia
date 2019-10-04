// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerPrepared"/> and 
    /// <see cref="ItemsRepeater.ElementPrepared"/> events.
    /// </summary>
    public class ElementPreparedEventArgs
    {
        public ElementPreparedEventArgs(IControl element, int index)
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
