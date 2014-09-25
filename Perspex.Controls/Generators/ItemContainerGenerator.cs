// -----------------------------------------------------------------------
// <copyright file="ItemContainerGenerator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Generators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class ItemContainerGenerator : IItemContainerGenerator
    {
        private Dictionary<object, Control> containersByItem = new Dictionary<object, Control>();

        private Dictionary<Control, object> itemsByContainer = new Dictionary<Control, object>();

        private ItemContainerGeneratorState state;

        public ItemContainerGenerator(ItemsControl owner)
        {
            this.Owner = owner;
        }

        public event EventHandler StateChanged;

        public ItemContainerGeneratorState State
        {
            get
            {
                return this.state;
            }

            set
            {
                if (this.state != value)
                {
                    this.state = value;

                    if (this.StateChanged != null)
                    {
                        this.StateChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        protected ItemsControl Owner
        {
            get;
            private set;
        }

        public Control GetContainerForItem(object item)
        {
            Control result;
            this.containersByItem.TryGetValue(item, out result);
            return result;
        }

        public object GetItemForContainer(Control container)
        {
            object result;
            this.itemsByContainer.TryGetValue(container, out result);
            return result;
        }

        public IEnumerable<Tuple<object, Control>> GetAll()
        {
            return this.containersByItem.Select(x => Tuple.Create(x.Key, x.Value));
        }

        IEnumerable<Control> IItemContainerGenerator.Generate(IEnumerable items)
        {
            List<Control> result = new List<Control>();

            this.State = ItemContainerGeneratorState.Generating;

            try
            {
                foreach (object item in items)
                {
                    Control container = this.CreateContainerOverride(item);
                    container.TemplatedParent = null;
                    this.containersByItem.Add(item, container);
                    this.itemsByContainer.Add(container, item);
                    result.Add(container);
                }
            }
            finally
            {
                this.State = ItemContainerGeneratorState.Generated;
            }

            return result;
        }

        IEnumerable<Control> IItemContainerGenerator.Remove(IEnumerable items)
        {
            List<Control> result = new List<Control>();

            foreach (var item in items)
            {
                Control container = this.containersByItem[item];
                this.containersByItem.Remove(item);
                this.itemsByContainer.Remove(container);
                result.Add(container);
            }

            return result;
        }

        void IItemContainerGenerator.RemoveAll()
        {
            this.containersByItem.Clear();
            this.itemsByContainer.Clear();
        }

        protected virtual Control CreateContainerOverride(object item)
        {
            return this.Owner.ApplyDataTemplate(item);
        }
    }
}
