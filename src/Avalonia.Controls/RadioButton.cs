// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class RadioButton : ToggleButton
    {
        public RadioButton()
        {
            this.GetObservable(IsCheckedProperty).Subscribe(IsCheckedChanged);
        }

        protected override void Toggle()
        {
            if (!IsChecked.GetValueOrDefault())
            {
                IsChecked = true;
            }
        }

        private void IsCheckedChanged(bool? value)
        {
            var parent = this.GetVisualParent();

            if (value.GetValueOrDefault() && parent != null)
            {
                var siblings = parent
                    .GetVisualChildren()
                    .OfType<RadioButton>()
                    .Where(x => x != this);

                foreach (var sibling in siblings)
                {
                    if (sibling.IsChecked.GetValueOrDefault())
                        sibling.IsChecked = false;
                }
            }
        }
    }
}
