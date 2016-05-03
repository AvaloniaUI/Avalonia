// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Interactivity;
using Perspex.Data;

namespace Perspex.Controls.Primitives
{
    public class ToggleButton : Button
    {
        public static readonly DirectProperty<ToggleButton, bool> IsCheckedProperty =
            PerspexProperty.RegisterDirect<ToggleButton, bool>(
                "IsChecked",
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

        protected override void OnClick(RoutedEventArgs e)
        {
            Toggle();
            base.OnClick(e);
        }

        protected override void DataValidationChanged(PerspexProperty property, IValidationStatus status)
        {
            if (property == IsCheckedProperty)
            {
                UpdateValidationState(status);
            }
        }

        protected virtual void Toggle()
        {
            IsChecked = !IsChecked;
        }
    }
}
