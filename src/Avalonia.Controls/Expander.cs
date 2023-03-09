using System;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Direction in which an <see cref="Expander"/> control opens.
    /// </summary>
    public enum ExpandDirection
    {
        /// <summary>
        /// Opens down.
        /// </summary>
        Down,

        /// <summary>
        /// Opens up.
        /// </summary>
        Up,

        /// <summary>
        /// Opens left.
        /// </summary>
        Left,

        /// <summary>
        /// Opens right.
        /// </summary>
        Right
    }

    /// <summary>
    /// A control with a header that has a collapsible content section.
    /// </summary>
    [PseudoClasses(":expanded", ":up", ":down", ":left", ":right")]
    public class Expander : HeaderedContentControl
    {
        /// <summary>
        /// Defines the <see cref="ContentTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> ContentTransitionProperty =
            AvaloniaProperty.Register<Expander, IPageTransition?>(
                nameof(ContentTransition));

        /// <summary>
        /// Defines the <see cref="ExpandDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<ExpandDirection> ExpandDirectionProperty =
            AvaloniaProperty.Register<Expander, ExpandDirection>(
                nameof(ExpandDirection),
                ExpandDirection.Down);

        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<Expander, bool>(
                nameof(IsExpanded),
                defaultBindingMode: BindingMode.TwoWay,
                coerce: CoerceIsExpanded);

        /// <summary>
        /// Defines the <see cref="Collapsed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CollapsedEvent =
            RoutedEvent.Register<Expander, RoutedEventArgs>(
                nameof(Collapsed),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Collapsing"/> event.
        /// </summary>
        public static readonly RoutedEvent<CancelRoutedEventArgs> CollapsingEvent =
            RoutedEvent.Register<Expander, CancelRoutedEventArgs>(
                nameof(Collapsing),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Expanded"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ExpandedEvent =
            RoutedEvent.Register<Expander, RoutedEventArgs>(
                nameof(Expanded),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Expanding"/> event.
        /// </summary>
        public static readonly RoutedEvent<CancelRoutedEventArgs> ExpandingEvent =
            RoutedEvent.Register<Expander, CancelRoutedEventArgs>(
                nameof(Expanding),
                RoutingStrategies.Bubble);

        private bool _ignorePropertyChanged = false;
        private CancellationTokenSource? _lastTransitionCts;

        /// <summary>
        /// Initializes a new instance of the <see cref="Expander"/> class.
        /// </summary>
        public Expander()
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Gets or sets the transition used when expanding or collapsing the content.
        /// </summary>
        public IPageTransition? ContentTransition
        {
            get => GetValue(ContentTransitionProperty);
            set => SetValue(ContentTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets the direction in which the <see cref="Expander"/> opens.
        /// </summary>
        public ExpandDirection ExpandDirection
        {
            get => GetValue(ExpandDirectionProperty);
            set => SetValue(ExpandDirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Expander"/>
        /// content area is open and visible.
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Occurs after the content area has closed and only the header is visible.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Collapsed
        {
            add => AddHandler(CollapsedEvent, value);
            remove => RemoveHandler(CollapsedEvent, value);
        }

        /// <summary>
        /// Occurs as the content area is closing.
        /// </summary>
        /// <remarks>
        /// The event args <see cref="CancelRoutedEventArgs.Cancel"/> property may be set to true to cancel the event
        /// and keep the control open (expanded).
        /// </remarks>
        public event EventHandler<CancelRoutedEventArgs>? Collapsing
        {
            add => AddHandler(CollapsingEvent, value);
            remove => RemoveHandler(CollapsingEvent, value);
        }

        /// <summary>
        /// Occurs after the <see cref="Expander"/> has opened to display both its header and content.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Expanded
        {
            add => AddHandler(ExpandedEvent, value);
            remove => RemoveHandler(ExpandedEvent, value);
        }

        /// <summary>
        /// Occurs as the content area is opening.
        /// </summary>
        /// <remarks>
        /// The event args <see cref="CancelRoutedEventArgs.Cancel"/> property may be set to true to cancel the event
        /// and keep the control closed (collapsed).
        /// </remarks>
        public event EventHandler<CancelRoutedEventArgs>? Expanding
        {
            add => AddHandler(ExpandingEvent, value);
            remove => RemoveHandler(ExpandingEvent, value);
        }

        /// <summary>
        /// Invoked just before the <see cref="Collapsed"/> event.
        /// </summary>
        protected virtual void OnCollapsed(RoutedEventArgs eventArgs)
        {
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Invoked just before the <see cref="Collapsing"/> event.
        /// </summary>
        protected virtual void OnCollapsing(CancelRoutedEventArgs eventArgs)
        {
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Invoked just before the <see cref="Expanded"/> event.
        /// </summary>
        protected virtual void OnExpanded(RoutedEventArgs eventArgs)
        {
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Invoked just before the <see cref="Expanding"/> event.
        /// </summary>
        protected virtual void OnExpanding(CancelRoutedEventArgs eventArgs)
        {
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Starts the content transition (if set) and invokes the <see cref="Expanded"/>
        /// and <see cref="Collapsed"/> events when completed.
        /// </summary>
        private async void StartContentTransition()
        {
            if (Content != null && ContentTransition != null && Presenter is Visual visualContent)
            {
                bool forward = ExpandDirection == ExpandDirection.Left ||
                               ExpandDirection == ExpandDirection.Up;

                _lastTransitionCts?.Cancel();
                _lastTransitionCts = new CancellationTokenSource();

                if (IsExpanded)
                {
                    await ContentTransition.Start(null, visualContent, forward, _lastTransitionCts.Token);
                }
                else
                {
                    await ContentTransition.Start(visualContent, null, forward, _lastTransitionCts.Token);
                }
            }

            // Expanded/Collapsed events are invoked asynchronously to ensure other events,
            // such as Click, have time to complete first.
            Dispatcher.UIThread.Post(() =>
            {
                if (IsExpanded)
                {
                    OnExpanded(new RoutedEventArgs(ExpandedEvent, this));
                }
                else
                {
                    OnCollapsed(new RoutedEventArgs(CollapsedEvent, this));
                }
            });
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (_ignorePropertyChanged)
            {
                return;
            }

            if (change.Property == ExpandDirectionProperty)
            {
                UpdatePseudoClasses();
            }
            else if (change.Property == IsExpandedProperty)
            {
                // Expanded/Collapsed will be raised once transitions are complete
                StartContentTransition();

                UpdatePseudoClasses();
            }
        }

        /// <summary>
        /// Updates the visual state of the control by applying latest PseudoClasses.
        /// </summary>
        private void UpdatePseudoClasses()
        {
            var expandDirection = ExpandDirection;

            PseudoClasses.Set(":up", expandDirection == ExpandDirection.Up);
            PseudoClasses.Set(":down", expandDirection == ExpandDirection.Down);
            PseudoClasses.Set(":left", expandDirection == ExpandDirection.Left);
            PseudoClasses.Set(":right", expandDirection == ExpandDirection.Right);

            PseudoClasses.Set(":expanded", IsExpanded);
        }

        /// <summary>
        /// Called when the <see cref="IsExpanded"/> property has to be coerced.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        protected virtual bool OnCoerceIsExpanded(bool value)
        {
            CancelRoutedEventArgs eventArgs;

            if (value)
            {
                eventArgs = new CancelRoutedEventArgs(ExpandingEvent, this);
                OnExpanding(eventArgs);
            }
            else
            {
                eventArgs = new CancelRoutedEventArgs(CollapsingEvent, this);
                OnCollapsing(eventArgs);
            }

            if (eventArgs.Cancel)
            {
                // If the event was externally canceled we must still notify the value has changed.
                // This property changed notification will update any external code observing this property that itself may have set the new value.
                // We are essentially reverted any external state change along with ignoring the IsExpanded property set.
                // Remember IsExpanded is usually controlled by a ToggleButton in the control theme and is also used for animations.
                _ignorePropertyChanged = true;

                RaisePropertyChanged(
                    IsExpandedProperty,
                    oldValue: value,
                    newValue: !value,
                    BindingPriority.LocalValue,
                    isEffectiveValue: true);

                _ignorePropertyChanged = false;

                return !value;
            }

            return value;
        }

        /// <summary>
        /// Coerces/validates the <see cref="IsExpanded"/> property value.
        /// </summary>
        /// <param name="instance">The <see cref="Expander"/> instance.</param>
        /// <param name="value">The value to coerce.</param>
        /// <returns>The coerced/validated value.</returns>
        private static bool CoerceIsExpanded(AvaloniaObject instance, bool value)
        {
            if (instance is Expander expander)
            {
                return expander.OnCoerceIsExpanded(value);
            }

            return value;
        }
    }
}
