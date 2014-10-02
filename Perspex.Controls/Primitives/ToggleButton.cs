// -----------------------------------------------------------------------
// <copyright file="ToggleButton.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;

    public class ToggleButton : Button
    {
        public static readonly PerspexProperty<bool> IsCheckedProperty =
            PerspexProperty.Register<ToggleButton, bool>("IsChecked");

        public ToggleButton()
        {
            this.Click += (s, e) => this.IsChecked = !this.IsChecked;
            this.AddPseudoClass(IsCheckedProperty, ":checked");
        }

        public bool IsChecked
        {
            get { return this.GetValue(IsCheckedProperty); }
            set { this.SetValue(IsCheckedProperty, value); }
        }
    }
}
