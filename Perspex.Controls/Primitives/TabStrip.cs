// -----------------------------------------------------------------------
// <copyright file="TabStrip.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls.Generators;

    public class TabStrip : SelectingItemsControl
    {
        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            TabControl.SelectedTabProperty.AddOwner<TabStrip>();

        static TabStrip()
        {
            AutoSelectProperty.OverrideDefaultValue<TabStrip>(true);
            FocusableProperty.OverrideDefaultValue(typeof(TabStrip), false);
        }

        public TabStrip()
        {
            this.GetObservable(SelectedItemProperty).Subscribe(x => this.SelectedTab = x as TabItem);
            this.GetObservable(SelectedTabProperty).Subscribe(x => this.SelectedItem = x as TabItem);
        }

        public TabItem SelectedTab
        {
            get { return this.GetValue(SelectedTabProperty); }
            set { this.SetValue(SelectedTabProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            TabControl tabControl = this.TemplatedParent as TabControl;
            IItemContainerGenerator result;

            if (tabControl != null)
            {
                result = tabControl.ItemContainerGenerator;
            }
            else
            {
                result = new TypedItemContainerGenerator<TabItem>(this);
            }

            ////result.StateChanged += this.ItemsContainerGeneratorStateChanged;

            return result;
        }

        private void ItemsContainerGeneratorStateChanged(object sender, EventArgs e)
        {
            ////if (this.ItemContainerGenerator.State == ItemContainerGeneratorState.Generated)
            ////{
            ////    var tabs = this.ItemContainerGenerator.GetAll()
            ////        .Select(x => x.Item2)
            ////        .OfType<TabItem>()
            ////        .ToList();

            ////    this.SelectedItem = tabs.FirstOrDefault(x => x.IsSelected) ?? tabs.FirstOrDefault();
            ////}
        }
    }
}
