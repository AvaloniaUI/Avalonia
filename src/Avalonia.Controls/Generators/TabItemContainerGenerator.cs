using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Generators
{    
    public class TabItemContainerGenerator : ItemContainerGenerator<TabItem>
    {
        public TabItemContainerGenerator(TabControl owner)
            : base(owner, ContentControl.ContentProperty, ContentControl.ContentTemplateProperty)
        {
            Owner = owner;
        }

        public new TabControl Owner { get; }

        protected override IControl CreateContainer(object item)
        {
            var tabItem = (TabItem)base.CreateContainer(item);

            tabItem.Bind(TabItem.TabStripPlacementProperty, new OwnerBinding<Dock>(
                tabItem,
                TabControl.TabStripPlacementProperty));

            if (tabItem.HeaderTemplate == null)
            {
                tabItem.Bind(TabItem.HeaderTemplateProperty, new OwnerBinding<IDataTemplate>(
                    tabItem,
                    TabControl.ItemTemplateProperty));
            }

            if (tabItem.Header == null)
            {
                if (item is IHeadered headered)
                {
                    tabItem.Header = headered.Header;
                }
                else
                {
                    if (!(tabItem.DataContext is IControl))
                    {
                        tabItem.Header = tabItem.DataContext;
                    }
                }
            }

            if (!(tabItem.Content is IControl))
            {
                tabItem.Bind(TabItem.ContentTemplateProperty, new OwnerBinding<IDataTemplate>(
                    tabItem,
                    TabControl.ContentTemplateProperty));
            }

            return tabItem;
        }

        private class OwnerBinding<T> : SingleSubscriberObservableBase<T>
        {
            private readonly TabItem _item;
            private readonly StyledProperty<T> _ownerProperty;
            private IDisposable _ownerSubscription;
            private IDisposable _propertySubscription;

            public OwnerBinding(TabItem item, StyledProperty<T> ownerProperty)
            {
                _item = item;
                _ownerProperty = ownerProperty;
            }

            protected override void Subscribed()
            {
                _ownerSubscription = ControlLocator.Track(_item, 0, typeof(TabControl)).Subscribe(OwnerChanged);
            }

            protected override void Unsubscribed()
            {
                _ownerSubscription?.Dispose();
                _ownerSubscription = null;
            }

            private void OwnerChanged(ILogical c)
            {
                _propertySubscription?.Dispose();
                _propertySubscription = null;

                if (c is TabControl tabControl)
                {
                    _propertySubscription = tabControl.GetObservable(_ownerProperty)
                        .Subscribe(x => PublishNext(x));
                }
            }
        }
    }
}
