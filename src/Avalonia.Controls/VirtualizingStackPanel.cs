using System;

namespace Avalonia.Controls
{
    public class VirtualizingStackPanel : StackPanel
    {
        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, ItemVirtualizationMode>(
                nameof(VirtualizationMode));

        public ItemVirtualizationMode VirtualizationMode
        {
            get => GetValue(VirtualizationModeProperty);
            set => SetValue(VirtualizationModeProperty, value);
        }
    }
}
