// -----------------------------------------------------------------------
// <copyright file="ListBoxItem.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Controls.Primitives;
    using Perspex.Interactivity;

    /// <summary>
    /// An selectable item in a <see cref="ListBox"/>.
    /// </summary>
    public class ListBoxItem : ContentControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            PerspexProperty.Register<ListBoxItem, bool>(nameof(IsSelected));

        /// <summary>
        /// Initializes static members of the <see cref="ListBoxItem"/> class.
        /// </summary>
        static ListBoxItem()
        {
            Control.PseudoClass(IsSelectedProperty, ":selected");
            IsSelectedProperty.Changed.Subscribe(IsSelectedChanged);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Called when the <see cref="IsSelected"/> property changes on an object.
        /// </summary>
        /// <param name="e">The sender.</param>
        private static void IsSelectedChanged(PerspexPropertyChangedEventArgs e)
        {
            var interactive = e.Sender as IInteractive;

            if (interactive != null)
            {
                interactive.RaiseEvent(new RoutedEventArgs(SelectingItemsControl.IsSelectedChangedEvent));
            }
        }
    }
}
