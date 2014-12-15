// -----------------------------------------------------------------------
// <copyright file="RadioButton.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Perspex.Controls.Primitives;

    public class RadioButton : ToggleButton
    {
        public RadioButton()
        {
            this.GetObservable(IsCheckedProperty).Subscribe(this.IsCheckedChanged);
        }

        protected override void Toggle()
        {
            if (!this.IsChecked)
            {
                this.IsChecked = true;
            }
        }

        private void IsCheckedChanged(bool value)
        {
            var parent = this.GetVisualParent();

            if (value && parent != null)
            {
                var siblings = parent
                    .GetVisualChildren()
                    .OfType<RadioButton>()
                    .Where(x => x != this);

                foreach (var sibling in siblings)
                {
                    sibling.IsChecked = false;
                }
            }
        }
    }
}
