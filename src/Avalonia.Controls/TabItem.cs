using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in a <see cref="TabControl"/>.
    /// </summary>
    [PseudoClasses(":pressed", ":selected")]
    public class TabItem : HeaderedContentControl, ISelectable
    {
        private Dock? _tabStripPlacement;

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
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> IconProperty =
            AvaloniaProperty.Register<TabItem, Control?>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="IndicatorTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> IndicatorTemplateProperty =
            AvaloniaProperty.Register<TabItem, IDataTemplate?>(nameof(IndicatorTemplate));

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
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideDefaultValue<TabItem>(IsOffscreenBehavior.FromClip);
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<TabItem>(OnAccessKeyPressed);
        }

        /// <summary>
        /// Gets the placement of this tab relative to the outer <see cref="TabControl"/>, if there is one.
        /// </summary>
        public Dock? TabStripPlacement
        {
            get => _tabStripPlacement;
            internal set => SetAndRaise(TabStripPlacementProperty, ref _tabStripPlacement, value);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon displayed alongside the tab header.
        /// </summary>
        public Control? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to render the selection indicator.
        /// </summary>
        public IDataTemplate? IndicatorTemplate
        {
            get => GetValue(IndicatorTemplateProperty);
            set => SetValue(IndicatorTemplateProperty, value);
        }

        /// <inheritdoc />
        protected override void OnAccessKey(RoutedEventArgs e)
        {
            Focus();
            SetCurrentValue(IsSelectedProperty, true);
            e.Handled = true;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ListItemAutomationPeer(this);

        private static void OnAccessKeyPressed(TabItem tabItem, AccessKeyPressedEventArgs e)
        {
            if (e.Handled || (e.Target != null && tabItem.IsSelected))
                return;

            e.Target = tabItem;
            e.Handled = true;
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateSelectionFromEvent(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            UpdateSelectionFromEvent(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            UpdateSelectionFromEvent(e);
        }

        protected bool UpdateSelectionFromEvent(RoutedEventArgs e) => SelectingItemsControl.ItemsControlFromItemContainer(this)?.UpdateSelectionFromEvent(this, e) ?? false;

        private void UpdateHeader(AvaloniaPropertyChangedEventArgs obj)
        {
            if (Header == null)
            {
                if (obj.NewValue is IHeadered headered)
                {
                    if (Header != headered.Header)
                    {
                        SetCurrentValue(HeaderProperty, headered.Header);
                    }
                }
                else
                {
                    if (!(obj.NewValue is Control))
                    {
                        SetCurrentValue(HeaderProperty, obj.NewValue);
                    }
                }
            }
            else
            {
                if (Header == obj.OldValue)
                {
                    SetCurrentValue(HeaderProperty, obj.NewValue);
                }
            }
        }
    }
}
