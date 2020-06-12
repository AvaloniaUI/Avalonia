using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Defines the containers used by the <see cref="LoopingSelector"/>
    /// </summary>
    public sealed class LoopingSelectorItem : ContentControl
    {
        static LoopingSelectorItem()
        {
            IsPressedProperty.Changed.AddClassHandler<LoopingSelectorItem>((x, e) => x.OnIsPressedChanged(e));
            IsSelectedProperty.Changed.AddClassHandler<LoopingSelectorItem>((x, e) => x.OnIsSelectedChanged(e));
        }

        /// <summary>
        /// Defines the <see cref="IsPressed"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> IsPressedProperty =
            AvaloniaProperty.Register<LoopingSelectorItem, bool>("IsPressed");

        /// <summary>
        /// Defines the <see cref="IsSelected"/> Property
        /// </summary>
        internal static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<LoopingSelectorItem, bool>("IsSelected");

        /// <summary>
        /// Gets whether the item is currently pressed
        /// </summary>
        public bool IsPressed
        {
            get => GetValue(IsPressedProperty);
            private set => SetValue(IsPressedProperty, value);
        }

        internal bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private void OnIsPressedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            PseudoClasses.Set(":pressed", (bool)e.NewValue);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                IsPressed = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (IsPressed)
            {
                var pt = e.GetPosition(this);
                if (pt.X < 0 || pt.Y < 0 || pt.X > Bounds.Width || pt.Y > Bounds.Height)
                    IsPressed = false;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (IsPressed && e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                IsPressed = false;
                //The selection event only raises when invoked by pointer events
                RaiseEvent(new RoutedEventArgs(SelectedEvent, this));
            }
        }

        private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = (bool)e.NewValue;
            PseudoClasses.Set(":selected", newValue);
        }

        public static readonly RoutedEvent<RoutedEventArgs> SelectedEvent =
            RoutedEvent.Register<LoopingSelectorItem, RoutedEventArgs>("Selected", RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs> Selected
        {
            add => AddHandler(SelectedEvent, value);
            remove => RemoveHandler(SelectedEvent, value);
        }
    }
}
