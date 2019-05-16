// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a non-scrollable grid that contains <see cref="T:Avalonia.Controls.DataGrid" /> row headers.
    /// </summary>
    public class DataGridFrozenGrid : Grid
    {
        public static readonly AvaloniaProperty<bool> IsFrozenProperty =
            AvaloniaProperty.RegisterAttached<DataGridFrozenGrid, Control, bool>("IsFrozen");

        /// <summary>
        /// Gets a value that indicates whether the grid is frozen.
        /// </summary>
        /// <param name="element">
        /// The object to get the <see cref="P:Avalonia.Controls.Primitives.DataGridFrozenGrid.IsFrozen" /> value from.
        /// </param>
        /// <returns>true if the grid is frozen; otherwise, false. The default is true.</returns>
        public static bool GetIsFrozen(Control element)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            return element.GetValue(IsFrozenProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether the grid is frozen.
        /// </summary>
        /// <param name="element">The object to set the <see cref="P:Avalonia.Controls.Primitives.DataGridFrozenGrid.IsFrozen" /> value on.</param>
        /// <param name="value">true if <paramref name="element" /> is frozen; otherwise, false.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="element" /> is null.</exception>
        public static void SetIsFrozen(Control element, bool value)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            element.SetValue(IsFrozenProperty, value);
        }
    }
}
