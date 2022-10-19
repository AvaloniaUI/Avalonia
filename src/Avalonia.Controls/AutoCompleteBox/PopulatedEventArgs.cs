// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populated" />
    /// event.
    /// </summary>
    public class PopulatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the list of possible matches added to the drop-down portion of
        /// the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The list of possible matches added to the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />.</value>
        public IEnumerable Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.PopulatedEventArgs" />.
        /// </summary>
        /// <param name="data">The list of possible matches added to the
        /// drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</param>
        public PopulatedEventArgs(IEnumerable data)
        {
            Data = data;
        }
    }
}
