// -----------------------------------------------------------------------
// <copyright file="TabControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    public class TabControl : SelectingItemsControl
    {
        public static readonly PerspexProperty<object> SelectedContentProperty =
            PerspexProperty.Register<TabControl, object>("SelectedContent");

        private TabStrip tabStrip;

        public TabControl()
        {
            this.GetObservable(SelectedItemProperty).Skip(1).Subscribe(this.SelectedItemChanged);
        }

        protected override void OnTemplateApplied()
        {
            this.tabStrip = this.GetTemplateControls()
                .OfType<TabStrip>()
                .FirstOrDefault();

            if (this.tabStrip != null)
            {
                if (this.IsSet(SelectedItemProperty))
                {
                    this.SelectedItem = SelectedItem;
                }

                this.tabStrip.GetObservable(TabStrip.SelectedItemProperty).Subscribe(x =>
                {
                    this.SelectedItem = x;
                });
            }
        }

        private void SelectedItemChanged(object item)
        {
            this.SelectedItem = item;

            ContentControl content = item as ContentControl;

            if (content != null)
            {
                this.SetValue(SelectedContentProperty, content.Content);
            }
            else
            {
                this.SetValue(SelectedContentProperty, item);
            }
        }
    }
}
