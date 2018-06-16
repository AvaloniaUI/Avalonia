// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="DropDown"/>.
    /// </summary>
    public class DropDownItem : ContentControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<DropDownItem, bool>(nameof(IsSelected));

        /// <summary>
        /// Initializes static members of the <see cref="DropDownItem"/> class.
        /// </summary>
        static DropDownItem()
        {
            FocusableProperty.OverrideDefaultValue<DropDownItem>(true);
        }

        public DropDownItem()
        {
            this.GetObservable(DropDownItem.IsFocusedProperty).Subscribe(focused =>
            {
                PseudoClasses.Set(":selected", focused);                
            });
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
    }
}
