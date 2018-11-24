// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="DropDown"/>.
    /// </summary>
    public class DropDownItem : ListBoxItem
    {
        public DropDownItem()
        {
            this.GetObservable(DropDownItem.IsFocusedProperty).Where(focused => focused)
                .Subscribe(_ => (Parent as DropDown)?.ItemFocused(this));
        }
    }
}
