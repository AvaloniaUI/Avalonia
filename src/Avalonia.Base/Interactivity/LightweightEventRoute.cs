using System;
using System.Diagnostics;
using Avalonia.Collections.Pooled;

namespace Avalonia.Interactivity;

/// <summary>
/// Struct counterpart of <see cref="EventRoute"/> to avoid allocations unless there are listeners.
/// </summary>
internal struct LightweightEventRoute : IDisposable
{
    private readonly RoutedEvent _event;
    private PooledList<RouteItem>? _route;

    public LightweightEventRoute(RoutedEvent e)
        => _event = e;

    public bool HasHandlers
        => _route is not null || _event.HasRouteFinishedSubscriptions;

    public void Add(
        Interactive target,
        Delegate handler,
        RoutingStrategies routes,
        bool handledEventsToo,
        Action<Delegate, object, RoutedEventArgs>? adapter)
    {
        _route ??= new PooledList<RouteItem>(16);
        _route.Add(new RouteItem(target, handler, adapter, routes, handledEventsToo));
    }

    public void AddClassHandler(Interactive target)
    {
        _route ??= new PooledList<RouteItem>(16);
        _route.Add(new RouteItem(target, null, null, 0, false));
    }

    public RoutedEventArgs? RaiseEvent<TContext>(
        Interactive source,
        Func<RoutedEvent, TContext, RoutedEventArgs> eventArgsFactory,
        TContext context)
    {
        if (!HasHandlers)
        {
            return null;
        }

        var e = eventArgsFactory(_event, context);
        RaiseEventWithArgs(source, e);
        return e;
    }

    public RoutedEventArgs? RaiseEvent(
        Interactive source,
        Func<RoutedEvent, RoutedEventArgs> eventArgsFactory)
    {
        if (!HasHandlers)
        {
            return null;
        }

        var e = eventArgsFactory(_event);
        RaiseEventWithArgs(source, e);
        return e;
    }

    public TEventArgs? RaiseEvent<TEventArgs, TContext>(
        Interactive source,
        Func<RoutedEvent<TEventArgs>, TContext, TEventArgs> eventArgsFactory,
        TContext context)
        where TEventArgs : RoutedEventArgs
    {
        if (!HasHandlers)
        {
            return null;
        }

        var e = eventArgsFactory((RoutedEvent<TEventArgs>)_event, context);
        RaiseEventWithArgs(source, e);
        return e;
    }

    public TEventArgs? RaiseEvent<TEventArgs>(
        Interactive source,
        Func<RoutedEvent<TEventArgs>, TEventArgs> eventArgsFactory)
        where TEventArgs : RoutedEventArgs
    {
        if (!HasHandlers)
        {
            return null;
        }

        var e = eventArgsFactory((RoutedEvent<TEventArgs>)_event);
        RaiseEventWithArgs(source, e);
        return e;
    }

    public void RaiseEventWithArgs(Interactive source, RoutedEventArgs e)
    {
        if (e.RoutedEvent != _event)
        {
            throw new InvalidOperationException(
                $"Event {e.RoutedEvent?.ToString() ?? "<null>"} from the event arguments differs from the route's event {_event}");
        }

        e.Source = source;

        if (_event.RoutingStrategies == RoutingStrategies.Direct)
        {
            RaiseEventWithStrategy(e, RoutingStrategies.Direct);
        }
        else
        {
            if ((_event.RoutingStrategies & RoutingStrategies.Tunnel) != 0)
            {
                RaiseEventWithStrategy(e, RoutingStrategies.Tunnel);
            }

            if ((_event.RoutingStrategies & RoutingStrategies.Bubble) != 0)
            {
                RaiseEventWithStrategy(e, RoutingStrategies.Bubble);
            }
        }
    }

    private void RaiseEventWithStrategy(RoutedEventArgs e, RoutingStrategies strategy)
    {
        e.Route = strategy;
        RaiseEventImpl(e);
        _event.InvokeRouteFinished(e);
    }

    private void RaiseEventImpl(RoutedEventArgs e)
    {
        if (_route is null)
        {
            return;
        }

        Interactive? lastTarget = null;
        var start = 0;
        var end = _route.Count;
        var step = 1;

        if (e.Route == RoutingStrategies.Tunnel)
        {
            start = end - 1;
            step = end = -1;
        }

        for (var i = start; i != end; i += step)
        {
            var entry = _route[i];

            // If we've got to a new control then call any RoutedEvent.Raised listeners.
            if (entry.Target != lastTarget)
            {
                _event.InvokeRaised(entry.Target, e);

                // If this is a direct event and we've already raised events then we're finished.
                if (e.Route == RoutingStrategies.Direct && lastTarget is not null)
                {
                    return;
                }

                lastTarget = entry.Target;
            }

            // Raise the event handler.
            if (entry.Handler is not null &&
                entry.Routes.HasAllFlags(e.Route) &&
                (!e.Handled || entry.HandledEventsToo))
            {
                if (entry.Adapter is not null)
                {
                    entry.Adapter(entry.Handler, entry.Target, e);
                }
                else
                {
                    entry.Handler.DynamicInvoke(entry.Target, e);
                }
            }
        }
    }

    public void Dispose()
    {
        _route?.Dispose();
        _route = null;
    }

    private readonly struct RouteItem
    {
        public RouteItem(
            Interactive target,
            Delegate? handler,
            Action<Delegate, object, RoutedEventArgs>? adapter,
            RoutingStrategies routes,
            bool handledEventsToo)
        {
            Target = target;
            Handler = handler;
            Adapter = adapter;
            Routes = routes;
            HandledEventsToo = handledEventsToo;
        }

        public Interactive Target { get; }
        public Delegate? Handler { get; }
        public Action<Delegate, object, RoutedEventArgs>? Adapter { get; }
        public RoutingStrategies Routes { get; }
        public bool HandledEventsToo { get; }
    }
}
