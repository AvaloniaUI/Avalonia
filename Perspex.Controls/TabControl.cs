// -----------------------------------------------------------------------
// <copyright file="TabControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;

    public class TabControl : SelectingItemsControl
    {
        public static readonly PerspexProperty<object> SelectedContentProperty =
            PerspexProperty.Register<TabControl, object>("SelectedContent");

        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            PerspexProperty.Register<TabControl, TabItem>("SelectedTab");

        private SingleItemPerspexList<ILogical> logicalChild = new SingleItemPerspexList<ILogical>();

        public TabControl()
        {
            this.GetObservable(SelectedItemProperty).Subscribe(x =>
            {
                ContentControl c = x as ContentControl;
                object content = (c != null) ? c.Content : null;
                this.SetValue(SelectedContentProperty, content);
            });

            this.BindTwoWay(SelectedTabProperty, this, SelectingItemsControl.SelectedItemProperty);
        }

        public object SelectedContent
        {
            get { return this.GetValue(SelectedContentProperty); }
            set { this.SetValue(SelectedContentProperty, value); }
        }

        public TabItem SelectedTab
        {
            get { return this.GetValue(SelectedTabProperty); }
            set { this.SetValue(SelectedTabProperty, value); }
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TypedItemContainerGenerator<TabItem>(this);
        }
    }
}
