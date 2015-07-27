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
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Controls.Utils;
    using Perspex.Styling;

    public class ItemsControl : TemplatedControl, ILogical
    {
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Needs to be before or a NullReferenceException is thrown.")]
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel());

        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.Register<ItemsControl, IEnumerable>("Items");

        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ItemsPanelTemplate>("ItemsPanel", defaultValue: DefaultPanel);

        private ItemContainerGenerator itemContainerGenerator;

        private PerspexReadOnlyListView<IVisual, ILogical> logicalChildren = 
            new PerspexReadOnlyListView<IVisual, ILogical>(x => (ILogical)x);

        private IItemsPresenter presenter;

        static ItemsControl()
        {
            ItemsProperty.Changed.Subscribe(e =>
            {
                var control = e.Sender as ItemsControl;
                control?.ItemsChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
            });
        }

        public ItemsControl()
        {
            this.ItemsChanged(null, null);
        }

        public ItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (this.itemContainerGenerator == null)
                {
                    this.itemContainerGenerator = this.CreateItemContainerGenerator();
                }

                return this.itemContainerGenerator;
            }
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

        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren
        {
            get
            {
                this.ApplyTemplate();
                return this.logicalChildren;
            }
        }

        protected IItemsPresenter Presenter
        {
            get
            {
                return this.presenter;
            }

            set
            {
                this.presenter = value;
                this.logicalChildren.Source = ((IVisual)value?.Panel)?.VisualChildren;
            }
        }

        protected virtual ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator(this);
        }

        protected override void OnTemplateApplied()
        {
            this.Presenter = this.FindTemplateChild<IItemsPresenter>("itemsPresenter");
        }

        protected virtual void ItemsChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            var incc = oldValue as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += this.ItemsCollectionChanged;
            }

            if (newValue == null || newValue.Count() == 0)
            {
                this.Classes.Add(":empty");
            }
            else
            {
                this.Classes.Remove(":empty");
            }

            incc = newValue as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += this.ItemsCollectionChanged;
            }
        }

        protected virtual void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ICollection;

            if (collection.Count == 0)
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
