// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;

namespace Avalonia.Controls.Primitives
{
    public class ToggleButton : Button
    {
        public static readonly DirectProperty<ToggleButton, bool?> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<ToggleButton, bool?>(
                nameof(IsChecked),
                o => o.IsChecked,
                (o, v) => o.IsChecked = v,
                unsetValue: null,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsThreeStateProperty =
            AvaloniaProperty.Register<ToggleButton, bool>(nameof(IsThreeState));

        private bool? _isChecked = false;

        public ToggleButton()
        {
            UpdatePseudoClasses(IsChecked);
        }

        public bool? IsChecked
        {
            get { return _isChecked; }
            set 
            { 
                SetAndRaise(IsCheckedProperty, ref _isChecked, value);
                UpdatePseudoClasses(value);
            }
        }

        public bool IsThreeState
        {
            get => GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        protected override void OnClick()
        {
            Toggle();
            base.OnClick();
        }

        protected virtual void Toggle()
        {
            if (IsChecked.HasValue)
                if (IsChecked.Value)
                    if (IsThreeState)
                        IsChecked = null;
                    else
                        IsChecked = false;
                else
                    IsChecked = true;
            else
                IsChecked = false;
        }

        private void UpdatePseudoClasses(bool? isChecked)
        {
            PseudoClasses.Set(":checked", isChecked == true);
            PseudoClasses.Set(":unchecked", isChecked == false);
            PseudoClasses.Set(":indeterminate", isChecked == null);
        }
    }
}
