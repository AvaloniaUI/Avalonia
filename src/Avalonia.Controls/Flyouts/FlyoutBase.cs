using System;

namespace Avalonia.Controls.Primitives
{
    public abstract class FlyoutBase : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="IsOpen"/> property
        /// </summary>
        public static readonly DirectProperty<FlyoutBase, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, bool>(nameof(IsOpen),
                x => x.IsOpen);

        /// <summary>
        /// Defines the <see cref="Target"/> property
        /// </summary>
        public static readonly DirectProperty<FlyoutBase, Control?> TargetProperty =
            AvaloniaProperty.RegisterDirect<FlyoutBase, Control?>(nameof(Target), x => x.Target);

        /// <summary>
        /// Defines the AttachedFlyout property
        /// </summary>
        public static readonly AttachedProperty<FlyoutBase?> AttachedFlyoutProperty =
            AvaloniaProperty.RegisterAttached<FlyoutBase, Control, FlyoutBase?>("AttachedFlyout", null);

        private bool _isOpen;
        private Control? _target;

        public event EventHandler? Opened;
        public event EventHandler? Closed;
        
        /// <summary>
        /// Gets whether this Flyout is currently Open
        /// </summary>
        public bool IsOpen
        {
            get => _isOpen;
            protected set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
        }

        /// <summary>
        /// Gets the Target used for showing the Flyout
        /// </summary>
        public Control? Target
        {
            get => _target;
            protected set => SetAndRaise(TargetProperty, ref _target, value);
        }
        
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

        public abstract void ShowAt(Control placementTarget);
        
        public abstract void Hide();
        
        protected virtual void OnOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
