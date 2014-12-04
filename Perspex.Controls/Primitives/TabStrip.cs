// -----------------------------------------------------------------------
// <copyright file="TabStrip.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Collections;
    using System.Linq;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Presenters;
    using Perspex.Input;

    public class TabStrip : SelectingItemsControl
    {
        private static readonly ItemsPanelTemplate PanelTemplate = new ItemsPanelTemplate(
            () => new StackPanel());

        static TabStrip()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(TabStrip), PanelTemplate);
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

            result.StateChanged += ItemsContainerGeneratorStateChanged;

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
