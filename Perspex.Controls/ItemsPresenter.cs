// -----------------------------------------------------------------------
// <copyright file="ItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Reactive.Linq;
    using Perspex.Controls.Generators;

    public class ItemsPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<ItemsPresenter>();

        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private Panel panel;

        public ItemsPresenter()
        {
            this.GetObservableWithHistory(ItemsProperty).Skip(1).Subscribe(this.ItemsChanged);
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

        protected override void CreateVisualChildren()
        {
            this.AddVisualChild(this.GetPanel());
            this.ItemsChanged(Tuple.Create(default(IEnumerable), this.Items));
        }

        private IItemContainerGenerator GetGenerator()
        {
            ItemsControl i = this.TemplatedParent as ItemsControl;

            if (i == null)
            {
                throw new InvalidOperationException("ItemsPresenter must be part of an ItemsControl template.");
            }

            return i.ItemContainerGenerator;
        }

        private Panel GetPanel()
        {
            if (this.panel == null && this.ItemsPanel != null)
            {
                this.panel = this.ItemsPanel.Build();
            }

            return this.panel;
        }

        private void ItemsChanged(Tuple<IEnumerable, IEnumerable> value)
        {
            var generator = this.GetGenerator();

            if (value.Item1 != null)
            {
                this.panel.Children.RemoveAll(generator.Remove(value.Item1));

                INotifyCollectionChanged incc = value.Item1 as INotifyCollectionChanged;

                if (incc != null)
                {
                    incc.CollectionChanged -= this.ItemsCollectionChanged;
                }
            }

            if (this.panel != null)
            {
                if (value.Item2 != null)
                {
                    this.panel.Children.AddRange(generator.Generate(this.Items));

                    INotifyCollectionChanged incc = value.Item2 as INotifyCollectionChanged;

                    if (incc != null)
                    {
                        incc.CollectionChanged += this.ItemsCollectionChanged;
                    }
                }
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.panel != null)
            {
                var generator = this.GetGenerator();

                // TODO: Handle Move and Replace etc.
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        this.panel.Children.AddRange(generator.Generate(e.NewItems));
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        this.panel.Children.RemoveAll(generator.Remove(e.OldItems));
                        break;
                }

                this.InvalidateMeasure();
            }
        }
    }
}
