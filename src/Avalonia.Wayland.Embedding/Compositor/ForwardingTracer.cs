using System;
using NWayland.Interop;
using NWayland.Server;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Server tracer that forwards to a <see cref="CallbackWaylandServerTracer"/> only while tracing is
/// enabled (i.e. <c>WaylandEmbeddingSubcompositor.ProtocolTrace</c> has subscribers). When disabled the
/// null-conditional forwards short-circuit, so no per-message formatting happens on the hot path.
/// </summary>
internal sealed class ForwardingTracer : IWaylandServerTracer
{
    private volatile IWaylandServerTracer? _inner;

    public void Enable(Action<string> sink) => _inner = new CallbackWaylandServerTracer(sink);
    public void Disable() => _inner = null;

    public void TraceEvent(WlResource resource, WlMessageDescription method, ReadOnlySpan<WlTracedArgument> args)
        => _inner?.TraceEvent(resource, method, args);

    public void TraceRequest(WlResource resource, WlMessageDescription method, ReadOnlySpan<WlTracedArgument> args)
        => _inner?.TraceRequest(resource, method, args);

    public void TraceDestroy(WlResource resource)
        => _inner?.TraceDestroy(resource);

    public void TraceUnconsumedNewId(WlResource targetResource, WlMessageDescription method, WlResource unconsumedResource)
        => _inner?.TraceUnconsumedNewId(targetResource, method, unconsumedResource);
}
