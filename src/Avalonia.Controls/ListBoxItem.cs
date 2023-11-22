using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="ListBox"/>.
    /// </summary>
    [PseudoClasses(":pressed", ":selected")]
    public class ListBoxItem : ContentControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            SelectingItemsControl.IsSelectedProperty.AddOwner<ListBoxItem>();

        private static readonly Point s_invalidPoint = new Point(double.NaN, double.NaN);
        private Point _pointerDownPoint = s_invalidPoint;

        /// <summary>
        /// Initializes static members of the <see cref="ListBoxItem"/> class.
        /// </summary>
        static ListBoxItem()
        {
            SelectableMixin.Attach<ListBoxItem>(IsSelectedProperty);
            PressedMixin.Attach<ListBoxItem>();
            FocusableProperty.OverrideDefaultValue<ListBoxItem>(true);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ListItemAutomationPeer(this);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            _pointerDownPoint = s_invalidPoint;

            if (e.Handled)
                return;

            if (!e.Handled && ItemsControl.ItemsControlFromItemContainer(this) is ListBox owner)
            {
                var p = e.GetCurrentPoint(this);

                if (p.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed or 
                    PointerUpdateKind.RightButtonPressed)
                {
                    if (p.Pointer.Type == PointerType.Mouse)
                    {
                        // If the pressed point comes from a mouse, perform the selection immediately.
                        e.Handled = owner.UpdateSelectionFromPointerEvent(this, e);
                    }
                    else
                    {
                        // Otherwise perform the selection when the pointer is released as to not
                        // interfere with gestures.
                        _pointerDownPoint = p.Position;

                        // Ideally we'd set handled here, but that would prevent the scroll gesture
                        // recognizer from working.
                        ////e.Handled = true;
                    }
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!e.Handled && 
                !double.IsNaN(_pointerDownPoint.X) &&
                e.InitialPressMouseButton is MouseButton.Left or MouseButton.Right)
            {
                var point = e.GetCurrentPoint(this);
                var settings = TopLevel.GetTopLevel(e.Source as Visual)?.PlatformSettings;
                var tapSize = settings?.GetTapSize(point.Pointer.Type) ?? new Size(4, 4);
                var tapRect = new Rect(_pointerDownPoint, new Size())
                    .Inflate(new Thickness(tapSize.Width, tapSize.Height));

                if (new Rect(Bounds.Size).ContainsExclusive(point.Position) &&
                    tapRect.ContainsExclusive(point.Position) &&
                    ItemsControl.ItemsControlFromItemContainer(this) is ListBox owner)
                {
                    if (owner.UpdateSelectionFromPointerEvent(this, e))
                    {
                        // As we only update selection from touch/pen on pointer release, we need to raise
                        // the pointer event on the owner to trigger a commit.
                        if (e.Pointer.Type != PointerType.Mouse)
                        {
                            var sourceBackup = e.Source;
                            owner.RaiseEvent(e);
                            e.Source = sourceBackup;
                        }

                        e.Handled = true;
                    }
                }
            }

            _pointerDownPoint = s_invalidPoint;
        }

    }
}
