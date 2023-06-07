using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in a <see cref="TabControl"/>.
    /// </summary>
    [PseudoClasses(":pressed", ":selected")]
    public class TabItem : HeaderedContentControl, ISelectable
    {
        private Dock? _tabStripPlacement;
        private IDisposable? _ownerSubscriptions;

        /// <summary>
        /// Defines the <see cref="TabStripPlacement"/> property.
        /// </summary>
        public static readonly DirectProperty<TabItem, Dock?> TabStripPlacementProperty =
            AvaloniaProperty.RegisterDirect<TabItem, Dock?>(nameof(TabStripPlacement), o => o.TabStripPlacement);

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            SelectingItemsControl.IsSelectedProperty.AddOwner<TabItem>();

        /// <summary>
        /// Initializes static members of the <see cref="TabItem"/> class.
        /// </summary>
        static TabItem()
        {
            SelectableMixin.Attach<TabItem>(IsSelectedProperty);
            PressedMixin.Attach<TabItem>();
            FocusableProperty.OverrideDefaultValue(typeof(TabItem), true);
            DataContextProperty.Changed.AddClassHandler<TabItem>((x, e) => x.UpdateHeader(e));
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<TabItem>(AutomationControlType.TabItem);
        }

        /// <summary>
        /// Gets the placement of this tab relative to the outer <see cref="TabControl"/>, if there is one.
        /// </summary>
        public Dock? TabStripPlacement
        {
            get => _tabStripPlacement;
            private set => SetAndRaise(TabStripPlacementProperty, ref _tabStripPlacement, value);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ListItemAutomationPeer(this);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _ownerSubscriptions?.Dispose();
            _ownerSubscriptions = null;

            if (this.FindAncestorOfType<TabControl>() is { } owner && owner.IndexFromContainer(this) != -1)
            {
                SubscribeToOwnerProperties(owner);
            }
        }

        protected void SubscribeToOwnerProperties(AvaloniaObject owner)
        {
            _ownerSubscriptions = owner.GetObservable(TabControl.TabStripPlacementProperty).Subscribe(v => TabStripPlacement = v);
        }

        private void UpdateHeader(AvaloniaPropertyChangedEventArgs obj)
        {
            if (Header == null)
            {
                if (obj.NewValue is IHeadered headered)
                {
                    if (Header != headered.Header)
                    {
                        Header = headered.Header;
                    }
                }
                else
                {
                    if (!(obj.NewValue is Control))
                    {
                        Header = obj.NewValue;
                    }
                }
            }
            else
            {
                if (Header == obj.OldValue)
                {
                    Header = obj.NewValue;
                }
            }
        }
    }
}
