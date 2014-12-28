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

    public class TabControl : SelectingItemsControl, ILogical
    {
        public static readonly PerspexProperty<object> SelectedContentProperty =
            PerspexProperty.Register<TabControl, object>("SelectedContent");

        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            PerspexProperty.Register<TabControl, TabItem>("SelectedTab");

        private TabStrip tabStrip;

        private ContentPresenter presenter;

        private IDisposable presenterSubscription;

        private SingleItemPerspexList<ILogical> logicalChild = new SingleItemPerspexList<ILogical>();

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

        IReadOnlyPerspexList<ILogical> ILogical.LogicalChildren
        {
            get { return this.logicalChild; }
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TypedItemContainerGenerator<TabItem>(this);
        }

        protected override void OnTemplateApplied()
        {
            if (this.presenterSubscription != null)
            {
                this.presenterSubscription.Dispose();
                this.presenterSubscription = null;
            }

            this.presenter = this.FindTemplateChild<ContentPresenter>("contentPresenter");

            if (this.presenter != null)
            {
                this.presenterSubscription = this.presenter.ChildObservable
                    .Subscribe(x => this.logicalChild.SingleItem = x);
            }

            this.tabStrip = this.GetTemplateControls().OfType<TabStrip>().FirstOrDefault();
            this.BindTwoWay(TabControl.SelectedItemProperty, this.tabStrip, TabControl.SelectedItemProperty);
        }
    }
}
