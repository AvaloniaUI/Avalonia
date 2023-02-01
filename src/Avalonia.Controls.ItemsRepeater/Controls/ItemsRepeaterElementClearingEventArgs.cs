// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsRepeater.ElementClearing"/> event.
    /// </summary>
    public class ItemsRepeaterElementClearingEventArgs : EventArgs
    {
        internal ItemsRepeaterElementClearingEventArgs(Control element) => Element = element;

        /// <summary>
        /// Gets the element that is being cleared for re-use.
        /// </summary>
        public Control Element { get; private set; }

        internal void Update(Control element) => Element = element;
    }
}
