// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Input;
    using Perspex.Interactivity;

    public class SelectingItemsControl : ItemsControl
    {
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<SelectingItemsControl, object>("SelectedItem");

        static SelectingItemsControl()
        {
            SelectedItemProperty.Changed.Subscribe(x =>
            {
                var control = x.Sender as SelectingItemsControl;

                if (control != null)
                {
                    control.SelectedItemChanged(x.NewValue);
                }
            });
        }

        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            IVisual source = (IVisual)e.Source;
            var selectable = source.GetVisualAncestors()
                .OfType<ISelectable>()
                .OfType<Control>()
                .FirstOrDefault();

            if (selectable != null)
            {
                var container = this.ItemContainerGenerator.GetItemForContainer(selectable);

                if (container != null)
                {
                    this.SelectedItem = container;
                }
            }
        }

        private void SelectedItemChanged(object selected)
        {
            var containers = this.ItemContainerGenerator.GetAll()
                .Select(x => x.Item2)
                .OfType<ISelectable>();
            var selectedContainer = (selected != null) ?
                this.ItemContainerGenerator.GetContainerForItem(selected) :
                null;

            foreach (var item in containers)
            {
                item.IsSelected = item == selectedContainer;
            }
        }
    }
}
