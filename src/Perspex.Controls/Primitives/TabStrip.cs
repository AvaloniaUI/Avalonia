// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls.Generators;
using Perspex.Input;

namespace Perspex.Controls.Primitives
{
    public class TabStrip : SelectingItemsControl
    {
        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            TabControl.SelectedTabProperty.AddOwner<TabStrip>();

        static TabStrip()
        {
            SelectionModeProperty.OverrideDefaultValue<TabStrip>(SelectionMode.AlwaysSelected);
            FocusableProperty.OverrideDefaultValue(typeof(TabStrip), false);
        }

        public TabStrip()
        {
            GetObservable(SelectedItemProperty).Subscribe(x => SelectedTab = x as TabItem);
            GetObservable(SelectedTabProperty).Subscribe(x => SelectedItem = x as TabItem);
        }

        public TabItem SelectedTab
        {
            get { return GetValue(SelectedTabProperty); }
            set { SetValue(SelectedTabProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            TabControl tabControl = TemplatedParent as TabControl;
            IItemContainerGenerator result;

            if (tabControl != null)
            {
                result = tabControl.ItemContainerGenerator;
            }
            else
            {
                result = new ItemContainerGenerator<TabItem>(this);
            }

            return result;
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                UpdateSelectionFromEventSource(e.Source);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.MouseButton == MouseButton.Left)
            {
                UpdateSelectionFromEventSource(e.Source);
            }
        }
    }
}
