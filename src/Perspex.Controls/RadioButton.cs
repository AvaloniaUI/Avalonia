// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Controls.Primitives;
using Perspex.VisualTree;

namespace Perspex.Controls
{
    public class RadioButton : ToggleButton
    {
        public RadioButton()
        {
            this.GetObservable(IsCheckedProperty).Subscribe(IsCheckedChanged);
        }

        protected override void Toggle()
        {
            if (!IsChecked)
            {
                IsChecked = true;
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
