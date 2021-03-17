namespace Avalonia.Controls
{
    using System;

    using Avalonia.Controls.Generators;

    public class MenuFlyoutPresenter : MenuBase
    {
        private readonly MenuFlyout menuFlyout;

        public MenuFlyoutPresenter(MenuFlyout menuFlyout)
        {
            this.menuFlyout = menuFlyout;
        }

        public override void Close()
        {
            menuFlyout.Hide();
        }

        public override void Open()
        {
            throw new NotSupportedException("Use MenuFlyout instead.");
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }
    }
}
