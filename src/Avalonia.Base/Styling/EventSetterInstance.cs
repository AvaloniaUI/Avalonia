using System;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.PropertyStore;

namespace Avalonia.Styling;

internal class EventSetterInstance : ISetterInstance, IValueEntry
{
    private readonly InputElement _inputElement;
    private readonly RoutedEvent _routedEvent;
    private readonly Delegate _handler;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1010")]
    private static readonly StyledProperty<EventSetterInstance> s_eventSetterProperty = AvaloniaProperty
        .Register<InputElement, EventSetterInstance>("EventSetter");

    public EventSetterInstance(InputElement inputElement, RoutedEvent routedEvent, Delegate handler,
        RoutingStrategies routes, bool handledEventsToo)
    {
        _inputElement = inputElement;
        _routedEvent = routedEvent;
        _handler = handler;

        inputElement.AddHandler(routedEvent, handler, routes, handledEventsToo);
    }

    public AvaloniaProperty Property => s_eventSetterProperty;
    public bool HasValue() => false;
    public object GetValue() => BindingOperations.DoNothing;

    public bool GetDataValidationState(out BindingValueType state, out Exception? error)
    {
        state = BindingValueType.DoNothing;
        error = null;
        return false;
    }

    public void Unsubscribe()
    {
        _inputElement.RemoveHandler(_routedEvent, _handler);
    }
}
