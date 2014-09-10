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
    using System.Linq;
    using Perspex.Layout;

    public class ItemsPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.Register<ItemsControl, IEnumerable>("Items");

        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ItemsPanelTemplate>("ItemsPanel");

        private Panel panel;

        public ItemsPresenter()
        {
            this.GetObservable(ItemsProperty).Subscribe(this.ItemsChanged);
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

        IEnumerable<IVisual> IVisual.ExistingVisualChildren
        {
            get { return Enumerable.Repeat(this.panel, this.panel != null ? 1 : 0); }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get { return Enumerable.Repeat(this.GetPanel(), 1); }
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

        private IEnumerable<Control> CreateItems(IEnumerable items)
        {
            if (items != null)
            {
                return items
                    .Cast<object>()
                    .Select(x => this.GetDataTemplate(x).Build(x))
                    .OfType<Control>();
            }
            else
            {
                return Enumerable.Empty<Control>();
            }
        }

        private Panel GetPanel()
        {
            if (this.panel == null && this.ItemsPanel != null)
            {
                this.panel = this.ItemsPanel.Build();
                this.ItemsChanged(this.Items);
            }

            return this.panel;
        }

        private void ItemsChanged(IEnumerable items)
        {
            if (this.panel != null)
            {
                this.panel.Children = new PerspexList<Control>(this.CreateItems(items));
            }
        }
    }
}
