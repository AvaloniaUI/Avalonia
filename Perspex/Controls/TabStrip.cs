// -----------------------------------------------------------------------
// <copyright file="TabStrip.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Perspex.Input;

namespace Perspex.Controls
{
    public class TabStrip : ItemsControl
    {
        private static readonly ItemsPanelTemplate PanelTemplate = new ItemsPanelTemplate(
            () => new StackPanel());

        private static readonly DataTemplate TabTemplate = new DataTemplate(
            o => new TabItem { Content = o });

        static TabStrip()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(TabStrip), PanelTemplate);
            ItemTemplateProperty.OverrideDefaultValue(typeof(TabStrip), TabTemplate);
        }

        public TabStrip()
        {
            this.PointerPressed += this.OnPointerPressed;
        }

        private void OnPointerPressed(object sender, PointerEventArgs e)
        {
            IVisual source = (IVisual)e.Source;
            ContentPresenter presenter = source.GetVisualAncestor<ContentPresenter>();

            if (presenter !=  null)
            {
                TabItem item = presenter.TemplatedParent as TabItem;

                if (item != null && item.TemplatedParent == this)
                {
                    item.IsSelected = true;

                    foreach (var i in item.GetVisualSiblings<TabItem>())
                    {
                        i.IsSelected = false;
                    }
                }
            }
        }
    }
}
