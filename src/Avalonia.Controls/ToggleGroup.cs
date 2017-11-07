// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Data;

namespace Avalonia.Controls
{
    public class ToggleGroup : TemplatedControl
    {
        public const string SelectedItemPropertyName = "SelectedItem";

        public static readonly DirectProperty<ToggleGroup, ToggleButton> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<ToggleGroup, ToggleButton>(
                SelectedItemPropertyName,
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        private ToggleButton _selectedItem;

        public ToggleGroup()
        {
            this.VisualChildren.TrackItemPropertyChanged(OnItemPropertyChanged);
        }

        public ToggleButton SelectedItem
        {
            get => _selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        }

        private void OnItemPropertyChanged(Tuple<object, PropertyChangedEventArgs> args)
        {
            if (args.Item1 is ToggleButton && args.Item2.PropertyName == ToggleButton.IsCheckedPropertyName)
            {
                OnItemToggled((ToggleButton)args.Item1);
            }
        }

        private void OnItemToggled(ToggleButton button)
        {
            // if the selected item is still checked, then a different button was 
            // toggled (and is now the selected item). Otherwise, the selectedItem was
            // toggled off (and there is now no selected item).
            ToggleButton newlySelectedItem = SelectedItem.IsChecked ? button : null;

            if (SelectedItem != null && SelectedItem.IsChecked)
            {
                SelectedItem.IsChecked = false;
            }

            SelectedItem = newlySelectedItem;
        }
    }
}
