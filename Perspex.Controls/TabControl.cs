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
    using Perspex.Controls.Primitives;

    public class TabControl : SelectingItemsControl
    {
        public static readonly PerspexProperty<object> SelectedContentProperty =
            PerspexProperty.Register<TabControl, object>("SelectedContent");

        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            PerspexProperty.Register<TabControl, TabItem>("SelectedTab");

        private TabStrip tabStrip;

        public TabControl()
        {
            this.GetObservable(SelectedItemProperty).Subscribe(x =>
            {
                ContentControl c = x as ContentControl;
                object content = (c != null) ? c.Content : null;
                this.SetValue(SelectedContentProperty, content);
            });

            this.Bind(
                SelectedTabProperty, 
                this.GetObservable(SelectedItemProperty).Select(x => x as TabItem));
        }

        public object SelectedContent
        {
            get { return this.GetValue(SelectedContentProperty); }
            set { this.SetValue(SelectedContentProperty, value); }
        }

        public TabItem SelectedTab
        {
            get { return this.GetValue(SelectedTabProperty); }
            private set { this.SetValue(SelectedTabProperty, value); }
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TypedItemContainerGenerator<TabItem>(this);
        }

        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();
            this.tabStrip = this.GetTemplateControls().OfType<TabStrip>().FirstOrDefault();
            this.BindTwoWay(TabControl.SelectedItemProperty, this.tabStrip, TabControl.SelectedItemProperty);
        }
    }
}
