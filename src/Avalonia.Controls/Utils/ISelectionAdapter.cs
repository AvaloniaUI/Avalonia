// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Defines an item collection, selection members, and key handling for the
    /// selection adapter contained in the drop-down portion of an
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
    /// </summary>
    public interface ISelectionAdapter
    {
        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <value>The currently selected item.</value>
        object? SelectedItem { get; set; }

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Avalonia.Controls.Utils.ISelectionAdapter.SelectedItem" />
        /// property value changes.
        /// </summary>
        event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        
        /// <summary>
        /// Gets or sets a collection that is used to generate content for the
        /// selection adapter.
        /// </summary>
        /// <value>The collection that is used to generate content for the
        /// selection adapter.</value>
        IEnumerable? ItemsSource { get; set; }

        /// <summary>
        /// Occurs when a selected item is not cancelled and is committed as the
        /// selected item.
        /// </summary>
        event EventHandler<RoutedEventArgs>? Commit;

        /// <summary>
        /// Occurs when a selection has been canceled.
        /// </summary>
        event EventHandler<RoutedEventArgs>? Cancel;

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.Input.InputElement.KeyDown" /> event that occurs
        /// when a key is pressed while the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has focus.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.Input.KeyEventArgs" />
        /// that contains data about the
        /// <see cref="E:Avalonia.Input.InputElement.KeyDown" /> event.</param>
        void HandleKeyDown(KeyEventArgs e);
    }

}
