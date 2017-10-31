// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Interactivity;
using Avalonia.Data;

namespace Avalonia.Controls.Primitives
{
    public class ToggleButton : Button
    {
        public const string IsCheckedPropertyName = "IsChecked";

        public static readonly DirectProperty<ToggleButton, bool> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<ToggleButton, bool>(
                IsCheckedPropertyName,
                o => o.IsChecked,
                (o,v) => o.IsChecked = v,
                defaultBindingMode: BindingMode.TwoWay);

        private bool _isChecked;

        static ToggleButton()
        {
            PseudoClass(IsCheckedProperty, ":checked");
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetAndRaise(IsCheckedProperty, ref _isChecked, value); }
        }

        protected override void OnClick()
        {
            Toggle();
            base.OnClick();
        }

        protected virtual void Toggle()
        {
            IsChecked = !IsChecked;
        }
    }
}
