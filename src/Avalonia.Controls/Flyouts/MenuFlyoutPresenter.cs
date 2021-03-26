using System;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    public class MenuFlyoutPresenter : MenuBase
    {
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<FlyoutPresenter>();

        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

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
                for (int i = 0; i < LogicalChildren.Count; i++)
                {
                    if (LogicalChildren[i] is MenuItem item)
                    {
                        item.IsSubMenuOpen = false;
                    }
                }

                SelectedIndex = -1;
                host.IsOpen = false;                
            }
        }

        public override void Open()
        {
            throw new NotSupportedException("Use MenuFlyout.ShowAt(Control) instead");
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }
    }
}
