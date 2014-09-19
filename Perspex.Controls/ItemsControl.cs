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
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    public class ItemsControl : TemplatedControl
    {
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Vertical });

        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.Register<ItemsControl, IEnumerable>("Items");

        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ItemsPanelTemplate>("ItemsPanel", defaultValue: DefaultPanel);

        private Dictionary<object, Control> controlsByItem = new Dictionary<object, Control>();

        private Dictionary<Control, object> itemsByControl = new Dictionary<Control, object>();

        public ItemsControl()
        {
            this.GetObservableWithHistory(ItemsProperty).Subscribe(this.ItemsChanged);
        }

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

        public Control GetControlForItem(object item)
        {
            Control result;
            this.controlsByItem.TryGetValue(item, out result);
            return result;
        }

        public object GetItemForControl(Control control)
        {
            object result;
            this.itemsByControl.TryGetValue(control, out result);
            return result;
        }

        public IEnumerable<Control> GetAllItemControls()
        {
            return this.controlsByItem.Values;
        }

        internal Control CreateItemControl(object item)
        {
            Control control = this.CreateItemControlOverride(item);
            this.itemsByControl.Add(control, item);
            this.controlsByItem.Add(item, control);
            return control;
        }

        internal void RemoveItemControls(IEnumerable items)
        {
            foreach (object i in items)
            {
                Control control = this.GetControlForItem(i);
                this.controlsByItem.Remove(i);
                this.itemsByControl.Remove(control);
            }
        }

        internal void ClearItemControls()
        {
            this.controlsByItem.Clear();
            this.itemsByControl.Clear();
        }

        protected virtual Control CreateItemControlOverride(object item)
        {
            return this.ApplyDataTemplate(item);
        }

        private void ItemsChanged(Tuple<IEnumerable, IEnumerable> value)
        {
            INotifyPropertyChanged inpc = value.Item1 as INotifyPropertyChanged;

            if (inpc != null)
            {
                inpc.PropertyChanged -= ItemsPropertyChanged;
            }

            if (value.Item2 == null || !value.Item2.OfType<object>().Any())
            {
                this.Classes.Add(":empty");
            }
            else
            {
                this.Classes.Remove(":empty");
            }

            inpc = value.Item2 as INotifyPropertyChanged;

            if (inpc != null)
            {
                inpc.PropertyChanged += ItemsPropertyChanged;
            }
        }

        private void ItemsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Count")
            {
                if (((IList)sender).Count == 0)
                {
                    this.Classes.Add(":empty");
                }
                else
                {
                    this.Classes.Remove(":empty");
                }
            }
        }
    }
}
