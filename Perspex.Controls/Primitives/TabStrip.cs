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

        private static readonly ItemsPanelTemplate PanelTemplate = new ItemsPanelTemplate(
            () => new StackPanel());

        static TabStrip()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(TabStrip), PanelTemplate);
        }

        public TabStrip()
        {
            this.Bind(
                SelectedTabProperty,
                this.GetObservable(SelectedItemProperty).Select(x => x as TabItem));
        }

        public TabItem SelectedTab
        {
            get { return this.GetValue(SelectedTabProperty); }
            private set { this.SetValue(SelectedTabProperty, value); }
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            TabControl tabControl = this.TemplatedParent as TabControl;
            ItemContainerGenerator result;

            if (tabControl != null)
            {
                result = tabControl.ItemContainerGenerator;
            }
            else
            {
                result = new TypedItemContainerGenerator<TabItem>(this);
            }

            result.StateChanged += this.ItemsContainerGeneratorStateChanged;

            return result;
        }

        private void ItemsContainerGeneratorStateChanged(object sender, EventArgs e)
        {
            if (this.ItemContainerGenerator.State == ItemContainerGeneratorState.Generated)
            {
                var tabs = this.ItemContainerGenerator.GetAll()
                    .Select(x => x.Item2)
                    .OfType<TabItem>()
                    .ToList();

                this.SelectedItem = tabs.FirstOrDefault(x => x.IsSelected) ?? tabs.FirstOrDefault();
            }
        }
    }
}
