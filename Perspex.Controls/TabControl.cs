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
    using Perspex.Controls.Templates;
    using Perspex.Input;

    public class TabControl : SelectingItemsControl, ILogical
    {
        public static readonly PerspexProperty<object> SelectedContentProperty =
            PerspexProperty.Register<TabControl, object>("SelectedContent");

        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            PerspexProperty.Register<TabControl, TabItem>("SelectedTab");

        private PerspexReadOnlyListView<ILogical> logicalChildren = 
            new PerspexReadOnlyListView<ILogical>();

        static TabControl()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TabControl), false);
        }

        public TabControl()
        {
            this.GetObservable(SelectedItemProperty).Subscribe(x =>
            {
                ContentControl c = x as ContentControl;
                object content = (c != null) ? c.Content : c;
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

        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren
        {
            get { return this.logicalChildren; }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Don't handle keypresses.
        }

        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();

            var deck = this.GetTemplateChild<Deck>("deck");
            this.logicalChildren.Source = ((ILogical)deck).LogicalChildren;
        }
    }
}
