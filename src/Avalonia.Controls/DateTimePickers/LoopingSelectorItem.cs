using System;
using Avalonia.Controls.Mixins;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Defines the containers used by the <see cref="LoopingSelector"/>
    /// </summary>
    public sealed class LoopingSelectorItem : ContentControl
    {
        static LoopingSelectorItem()
        {
            PressedMixin.Attach<LoopingSelectorItem>();
            IsSelectedProperty.Changed.AddClassHandler<LoopingSelectorItem>((x, e) => x.OnIsSelectedChanged(e));
        }

        /// <summary>
        /// Defines the <see cref="IsSelected"/> Property
        /// </summary>
        internal static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<LoopingSelectorItem, bool>(nameof(IsSelected));


        public static readonly RoutedEvent<RoutedEventArgs> SelectedEvent =
            RoutedEvent.Register<LoopingSelectorItem, RoutedEventArgs>(nameof(Selected), RoutingStrategies.Bubble);

        internal bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                //The selection event only raises when invoked by pointer events
                RaiseEvent(new RoutedEventArgs(SelectedEvent, this));
            }
        }

        private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = (bool)e.NewValue;
            PseudoClasses.Set(":selected", newValue);
        }

        public event EventHandler<RoutedEventArgs> Selected
        {
            add => AddHandler(SelectedEvent, value);
            remove => RemoveHandler(SelectedEvent, value);
        }
    }
}
