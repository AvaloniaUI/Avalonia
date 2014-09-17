// -----------------------------------------------------------------------
// <copyright file="TabStrip.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;
    using System.Linq;
    using Perspex.Input;

    public class TabStrip : SelectingItemsControl
    {
        private static readonly ItemsPanelTemplate PanelTemplate = new ItemsPanelTemplate(
            () => new StackPanel());

        static TabStrip()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(TabStrip), PanelTemplate);
        }

        public TabStrip()
        {
            this.PointerPressed += this.OnPointerPressed;
            this.GetObservable(ItemsProperty).Subscribe(this.ItemsChanged);
            this.GetObservable(SelectedItemProperty).Subscribe(this.SelectedItemChanged);
        }

        protected override Control CreateItemControlOverride(object item)
        {
            TabItem result = item as TabItem;

            if (result == null)
            {
                result = new TabItem
                {
                    Content = this.GetDataTemplate(item).Build(item),
                };
            }

            result.IsSelected = this.SelectedItem == item;

            return result;
        }

        private void ItemsChanged(IEnumerable items)
        {
            if (items != null)
            {
                this.SelectedItem =
                    items.OfType<TabItem>().FirstOrDefault(x => x.IsSelected) ??
                    items.OfType<object>().FirstOrDefault();
            }
            else
            {
                this.SelectedItem = null;
            }
        }

        private void OnPointerPressed(object sender, PointerEventArgs e)
        {
            IVisual source = (IVisual)e.Source;
            ContentPresenter presenter = source.GetVisualAncestors()
                .OfType<ContentPresenter>()
                .FirstOrDefault();

            if (presenter !=  null)
            {
                TabItem item = presenter.TemplatedParent as TabItem;

                if (item != null)
                {
                    this.SelectedItem = item;
                }
            }
        }

        private void SelectedItemChanged(object selectedItem)
        {
            foreach (TabItem item in this.GetAllItemControls())
            {
                item.IsSelected = item == selectedItem;
            }
        }
    }
}
