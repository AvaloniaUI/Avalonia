// -----------------------------------------------------------------------
// <copyright file="ItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ItemsControl : TemplatedControl
    {
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Vertical });

        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.Register<ItemsControl, IEnumerable>("Items");

        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ItemsPanelTemplate>("ItemsPanel", defaultValue: DefaultPanel);

        public static readonly PerspexProperty<DataTemplate> ItemTemplateProperty =
            PerspexProperty.Register<ItemsControl, DataTemplate>("ItemTemplate");

        private Dictionary<object, Control> itemControls = new Dictionary<object, Control>();

        public IEnumerable Items
        {
            get { return this.GetValue(ItemsProperty); }
            set { this.SetValue(ItemsProperty, value); }
        }

        public ItemsPanelTemplate ItemsPanel
        {
            get { return this.GetValue(ItemsPanelProperty); }
            set { this.SetValue(ItemsPanelProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return this.GetValue(ItemTemplateProperty); }
            set { this.SetValue(ItemTemplateProperty, value); }
        }

        public Control GetControlForItem(object item)
        {
            Control result;
            this.itemControls.TryGetValue(item, out result);
            return result;
        }

        public IEnumerable<Control> GetAllItemControls()
        {
            return this.itemControls.Values;
        }

        internal Control CreateItemControl(object item)
        {
            Control control = this.CreateItemControlOverride(item);
            this.itemControls.Add(item, control);
            return control;
        }

        protected virtual Control CreateItemControlOverride(object item)
        {
            Control control = item as Control;
            DataTemplate template = this.ItemTemplate;

            if (control != null)
            {
                return control;
            }
            else if (template != null)
            {
                return template.Build(item);
            }
            else
            {
                return this.GetDataTemplate(item).Build(item);
            }
        }
    }
}
