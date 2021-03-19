using Avalonia.Controls.Generators;
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

        public override void Close()
        {
            // DefaultMenuInteractionHandler calls this
            var host = this.FindLogicalAncestorOfType<Popup>();
            if (host != null)
            {
                host.IsOpen = false;
            }
        }

        public override void Open()
        {
            //Ignore
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }
    }
}
