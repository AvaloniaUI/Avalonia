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
    using System.Linq;

    public class ItemsPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<ItemsPresenter>();

        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private Panel panel;

        public ItemsPresenter()
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

        protected override Size MeasureOverride(Size availableSize)
        {
            Panel panel = this.GetPanel();
            panel.Measure(availableSize);
            return panel.DesiredSize.Value;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            this.GetPanel().Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override IEnumerable<Visual> CreateVisualChildren()
        {
            return Enumerable.Repeat(this.GetPanel(), 1);
        }

        private Control CreateItemControl(object item)
        {
            ItemsControl i = this.TemplatedParent as ItemsControl;

            if (i != null)
            {
                return i.CreateItemControl(item);
            }
            else
            {
                return this.GetDataTemplate(item).Build(item) as Control;
            }
        }
         
        private IEnumerable<Control> CreateItemControls(IEnumerable items)
        {
            if (items != null)
            {
                return items
                    .Cast<object>()
                    .Select(x => this.CreateItemControl(x))
                    .OfType<Control>();
            }
            else
            {
                return Enumerable.Empty<Control>();
            }
        }

        private void ClearItemControls()
        {
            ItemsControl i = this.TemplatedParent as ItemsControl;

            if (i != null)
            {
                i.ClearItemControls();
            }
        }

        private void RemoveItemControls(IEnumerable items)
        {
            ItemsControl i = this.TemplatedParent as ItemsControl;

            if (i != null)
            {
                i.RemoveItemControls(items);
            }
        }

        private Panel GetPanel()
        {
            if (this.panel == null && this.ItemsPanel != null)
            {
                this.panel = this.ItemsPanel.Build();
                this.ItemsChanged(Tuple.Create(default(IEnumerable), this.Items));
            }

            return this.panel;
        }

        private void ItemsChanged(Tuple<IEnumerable, IEnumerable> value)
        {
            if (value.Item1 != null)
            {
                INotifyCollectionChanged incc = value.Item1 as INotifyCollectionChanged;

                if (incc != null)
                {
                    incc.CollectionChanged -= this.ItemsCollectionChanged;
                }
            }

            this.ClearItemControls();

            if (this.panel != null)
            {
                var controls = this.CreateItemControls(value.Item2).ToList();

                foreach (var control in controls)
                {
                    control.TemplatedParent = null;
                }

                this.panel.Children = new Controls(controls);

                INotifyCollectionChanged incc = value.Item2 as INotifyCollectionChanged;

                if (incc != null)
                {
                    incc.CollectionChanged += this.ItemsCollectionChanged;
                }
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.panel != null)
            {
                // TODO: Handle Move and Replace.
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        var controls = this.CreateItemControls(e.NewItems).ToList();

                        foreach (var control in controls)
                        {
                            control.TemplatedParent = null;
                        }

                        this.panel.Children.AddRange(controls);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        this.RemoveItemControls(e.OldItems);
                        break;
                        
                    case NotifyCollectionChangedAction.Reset:
                        this.ItemsChanged(Tuple.Create(this.Items, this.Items));
                        break;
                }
            }
        }
    }
}
