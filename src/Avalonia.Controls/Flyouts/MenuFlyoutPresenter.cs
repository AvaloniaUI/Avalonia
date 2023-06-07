using System;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    public class MenuFlyoutPresenter : MenuBase
    {
        public MenuFlyoutPresenter()
            :base(new DefaultMenuInteractionHandler(true))
        {
        }

        public MenuFlyoutPresenter(IMenuInteractionHandler menuInteractionHandler)
            : base(menuInteractionHandler)
        {
        }


        public override void Close()
        {
            // DefaultMenuInteractionHandler calls this
            var host = this.FindLogicalAncestorOfType<Popup>();
            if (host != null)
            {
                SelectedIndex = -1;
                host.IsOpen = false;
            }
        }

        public override void Open()
        {
            throw new NotSupportedException("Use MenuFlyout.ShowAt(Control) instead");
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            foreach (var i in LogicalChildren)
            {
                if (i is MenuItem menuItem)
                {
                    menuItem.IsSubMenuOpen = false;
                }
            }
        }
    }
}
