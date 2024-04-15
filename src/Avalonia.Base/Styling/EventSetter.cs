using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.PropertyStore;
using Avalonia.Threading;

namespace Avalonia.Styling;

/// <summary>
/// Represents an event setter in a style. Event setters invoke the specified event handlers in response to events.
/// </summary>
public sealed class EventSetter : SetterBase, ISetterInstance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSetter"/> class.
    /// </summary>
    public EventSetter()
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSetter"/> class, using the provided event and handler parameters.
    /// </summary>
    /// <param name="eventName">The particular routed event that the <see cref="EventSetter"/> responds to.</param>
    /// <param name="handler">The handler to assign in this setter.</param>
    public EventSetter(RoutedEvent eventName, Delegate handler)
    {
        Event = eventName;
        Handler = handler;
    }

    /// <summary>
    /// Gets or sets the particular routed event that this EventSetter responds to.
    /// </summary>
    public RoutedEvent? Event { get; set; }

    /// <summary>
    /// Gets or sets the reference to a handler for a routed event in the setter.
    /// </summary>
    public Delegate? Handler { get; set; }

    /// <summary>
    /// The routing strategies to listen to.
    /// </summary>
    public RoutingStrategies Routes { get; set; } = RoutingStrategies.Bubble | RoutingStrategies.Direct;

    /// <summary>
    /// Gets or sets a value that determines whether the handler assigned to the setter should still be invoked, even if the event is marked handled in its event data.
    /// </summary>
    public bool HandledEventsToo { get; set; }

    internal override ISetterInstance Instance(IStyleInstance styleInstance, StyledElement target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        if (Event is null)
            throw new InvalidOperationException($"{nameof(EventSetter)}.{nameof(Event)} must be set.");

        if (Handler is null)
            throw new InvalidOperationException($"{nameof(EventSetter)}.{nameof(Handler)} must be set.");

        if (target is not InputElement inputElement)
            throw new ArgumentException($"{nameof(EventSetter)} target must be a {nameof(InputElement)}", nameof(target));

        if (styleInstance.HasActivator)
            throw new InvalidOperationException("EventSetter cannot be used in styles with activators, i.e. styles with complex selectors.");

        return new EventSetterInstance(inputElement, Event, Handler, Routes, HandledEventsToo);
    }
}
