// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

            tabItem.ParentTabControl = Owner;

            if (tabItem.Header == null)
            {
                if (item is IHeadered headered)
                {
                    if (tabItem.Header != headered.Header)
                    {
                        tabItem.Header = headered.Header;
                    }
                }
                else
                {
                    if (!(tabItem.DataContext is IControl))
                    {
                        tabItem.Header = tabItem.DataContext;
                    }
                }
            }

            if (tabItem.HeaderTemplate == null)
            {
                tabItem[~TabItem.HeaderTemplateProperty] = Owner[~TabControl.ItemTemplateProperty];
            }

            if (tabItem.Content == null)
            {              
                tabItem[~TabItem.ContentProperty] = tabItem[~TabItem.DataContextProperty];
            }
           
            if (!(tabItem.Content is IControl))
            {
                tabItem[~TabItem.ContentTemplateProperty] = Owner[~TabControl.ContentTemplateProperty];
            }

            return tabItem;
        }
    }
}
