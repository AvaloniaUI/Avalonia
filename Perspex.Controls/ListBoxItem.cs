// -----------------------------------------------------------------------
// <copyright file="ListBoxItem.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Controls.Mixins;

    /// <summary>
    /// A selectable item in a <see cref="ListBox"/>.
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
            SelectableMixin.Attach<ListBoxItem>(IsSelectedProperty);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }
    }
}
