using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    public partial class VirtualizingStackPanel : VirtualizingPanel
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            StackLayout.OrientationProperty.AddOwner<VirtualizingStackPanel>();

        /// <summary>
        /// Defines the VirtualizationMode attached property.
        /// </summary>
        public static readonly AttachedProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, ItemVirtualizationMode>(
                "VirtualizationMode", ItemVirtualizationMode.None);

        private ItemVirtualizationMode _virtualizationMode;

        public VirtualizingStackPanel()
        {
            _recycleElement = RecycleElement;
            _updateElementIndex = UpdateElementIndex;
        }

        /// <summary>
        /// Gets or sets the axis along which items are laid out.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the axis along which items are laid out.
        /// The default is Vertical.
        /// </value>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets the current <see cref="ItemVirtualizationMode"/> in use for the panel.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get => _virtualizationMode;
            set
            {
                if (_virtualizationMode != value)
                {
                    if (_virtualizationMode == ItemVirtualizationMode.Smooth)
                        EffectiveViewportChanged -= OnEffectiveViewportChanged;

                    _virtualizationMode = value;

                    if (_virtualizationMode == ItemVirtualizationMode.Smooth)
                        EffectiveViewportChanged += OnEffectiveViewportChanged;

                    Children.Clear();
                }
            }
        }

        public static ItemVirtualizationMode GetVirtualizationMode(Control c)
        {
            return c.GetValue(VirtualizationModeProperty);
        }

        public static void SetVirtualizationMode(Control c, ItemVirtualizationMode value)
        {
            c.SetValue(VirtualizationModeProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return VirtualizationMode switch
            {
                ItemVirtualizationMode.Smooth => MeasureOverrideSmooth(availableSize),
                _ => throw new NotImplementedException()
            };
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return VirtualizationMode switch
            {
                ItemVirtualizationMode.Smooth => ArrangeOverrideSmooth(finalSize),
                _ => throw new NotImplementedException()
            };
        }

        protected override void OnItemsControlChanged(ItemsControl? oldValue)
        {
            base.OnItemsControlChanged(oldValue);
            VirtualizationMode = ItemsControl is null ? ItemVirtualizationMode.None : GetVirtualizationMode(ItemsControl);
        }

        protected override void OnItemsChanged(IList items, NotifyCollectionChangedEventArgs e)
        {
            switch (VirtualizationMode)
            {
                case ItemVirtualizationMode.Smooth:
                    OnItemsChangedSmooth(e);
                    break;
            }
        }

        internal IReadOnlyList<Control?> GetRealizedElements()
        {
            return VirtualizationMode switch
            {
                ItemVirtualizationMode.Smooth => _realizedElements?.Elements ?? Array.Empty<Control>(),
                _ => Children,
            };
        }

        private protected override void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            base.OnItemsControlPropertyChanged(sender, e);

            if (e.Property == VirtualizationModeProperty)
            {
                VirtualizationMode = e.GetNewValue<ItemVirtualizationMode>();
            }
        }
    }
}
