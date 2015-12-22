// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Generators;
using Perspex.Controls.Templates;
using Perspex.Input;

namespace Perspex.Controls.Primitives
{
    public class TabStrip : SelectingItemsControl
    {
        private static IMemberSelector s_MemberSelector = new FuncMemberSelector<object, object>(SelectHeader);

        static TabStrip()
        {
            MemberSelectorProperty.OverrideDefaultValue<TabStrip>(s_MemberSelector);
            SelectionModeProperty.OverrideDefaultValue<TabStrip>(SelectionMode.AlwaysSelected);
            FocusableProperty.OverrideDefaultValue(typeof(TabStrip), false);
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<TabStripItem>(this, ContentControl.ContentProperty);
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.MouseButton == MouseButton.Left)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        private static object SelectHeader(object o)
        {
            var headered = o as IHeadered;
            return (headered != null) ? (headered.Header ?? string.Empty) : o;
        }
    }
}
