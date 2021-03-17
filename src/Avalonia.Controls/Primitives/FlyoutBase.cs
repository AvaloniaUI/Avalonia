#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using Avalonia.Input;

namespace Avalonia.Controls.Primitives
{
    public abstract class FlyoutBase : AvaloniaObject
    {
        public static readonly StyledProperty<FlyoutPlacementMode> PlacementProperty =
            AvaloniaProperty.Register<FlyoutBase, FlyoutPlacementMode>(nameof(Placement), FlyoutPlacementMode.Top);

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<FlyoutBase, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, bool>(nameof(IsOpen), o => o.IsOpen);

        public static readonly DirectProperty<FlyoutBase, Control?> TargetProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, Control?>(nameof(Target), o => o.Target);

        public static readonly AttachedProperty<FlyoutBase?> AttachedFlyoutProperty =
            AvaloniaProperty.RegisterAttached<FlyoutBase, Control, FlyoutBase?>("AttachedFlyout", null);

        private bool _isOpen;
        private Control? _target;
        private Popup? _popup;
        private IInputElement? _previousFocus;

        protected FlyoutBase()
        {

        }

        public bool IsOpen
        {
            get => _isOpen;
            private set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
        }

        public FlyoutPlacementMode Placement
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public Control? Target
        {
            get => _target;
            private set => SetAndRaise(TargetProperty, ref _target, value);
        }

        public void ShowAt(Control placementTarget)
        {
            ShowAtCore(placementTarget);
        }

        public void Hide()
        {
            Hide(canCancel: true);
        }

        protected virtual Control? CreatePresenter() { return null; }

        public event EventHandler? Closed;
        public event EventHandler<CancelEventArgs>? Closing;
        public event EventHandler? Opened;
        public event EventHandler? Opening;

        public static FlyoutBase? GetAttachedFlyout(Control element)
        {
            return element.GetValue(AttachedFlyoutProperty);
        }

        public static void SetAttachedFlyout(Control element, FlyoutBase? value)
        {
            element.SetValue(AttachedFlyoutProperty, value);
        }

        public static void ShowAttachedFlyout(Control flyoutOwner)
        {
            var flyout = GetAttachedFlyout(flyoutOwner);
            flyout?.ShowAt(flyoutOwner);
        }

        internal void Hide(bool canCancel)
        {
            if (!IsOpen)
            {
                return;
            }

            if (canCancel)
            {
                bool cancel = false;

                var closing = new CancelEventArgs();
                Closing?.Invoke(this, closing);
                if (cancel || closing.Cancel)
                {
                    return;
                }
            }

            Close();
            IsOpen = false;

            Closed?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void Close()
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }
        }

        protected internal virtual void Open()
        {
            if (_popup != null)
            {
                // SetPopupPositionPartial(Target, _popupPositionInTarget);

                _popup.PlacementTarget = Target;
                _popup.IsOpen = true;
            }
        }

        private void EnsurePopupCreated()
        {
            if (_popup == null)
            {
                _popup = new Popup()
                {
                    Child = CreatePresenter(),
                    IsLightDismissEnabled = true
                };

                _popup.Opened += OnPopupOpened;
                _popup.Closed += OnPopupClosed;
            }
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            Hide(canCancel: false);
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            if (((Popup)sender).Child is Control child)
            {
                //SizeChangedEventHandler handler = (_, __) => SetPopupPositionPartial(Target, _popupPositionInTarget);

                //child.SizeChanged += handler;

                //_sizeChangedDisposable.Disposable = Disposable
                //    .Create(() => child.SizeChanged -= handler);
            }
        }

        private protected virtual void ShowAtCore(Control placementTarget)
        {
            EnsurePopupCreated();

            if (IsOpen)
            {
                if (placementTarget == Target)
                {
                    return;
                }
                else
                {
                    // Close at previous placement target before opening at new one (without raising Closing)
                    Hide(canCancel: false);
                }
            }

            Target = placementTarget;

            //if (showOptions != null)
            //{
            //    _popupPositionInTarget = showOptions.Position;
            //}

            if (_popup.Parent != placementTarget)
            {
                ((ISetLogicalParent)_popup).SetParent(null);
                ((ISetLogicalParent)_popup).SetParent(placementTarget);
            }

            Opening?.Invoke(this, EventArgs.Empty);
            Open();
            IsOpen = true;
            Opened?.Invoke(this, EventArgs.Empty);
        }
    }
}
