// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Interactivity;

namespace Perspex.Controls.Primitives
{
    public class ToggleButton : Button
    {
        public static readonly PerspexProperty<bool> IsCheckedProperty =
            PerspexProperty.Register<ToggleButton, bool>("IsChecked");

        static ToggleButton()
        {
            PseudoClass(IsCheckedProperty, ":checked");
        }

        public ToggleButton()
        {
        }

        public bool IsChecked
        {
            get { return GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        protected override void OnClick(RoutedEventArgs e)
        {
            Toggle();
        }

        protected virtual void Toggle()
        {
            IsChecked = !IsChecked;
        }
    }
}
