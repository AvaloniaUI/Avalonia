// -----------------------------------------------------------------------
// <copyright file="ToggleButton.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;

    public class ToggleButton : Button
    {
        public static readonly PerspexProperty<bool> IsCheckedProperty =
            PerspexProperty.Register<ToggleButton, bool>("IsChecked");

        public ToggleButton()
        {
            this.Click += (s, e) => this.IsChecked = !this.IsChecked;

            this.GetObservable(IsCheckedProperty).Subscribe(x =>
            {
                if (x)
                {
                    this.Classes.Add(":checked");
                }
                else
                {
                    this.Classes.Remove(":checked");
                }
            });
        }

        public bool IsChecked
        {
            get { return this.GetValue(IsCheckedProperty); }
            set { this.SetValue(IsCheckedProperty, value); }
        }
    }
}
