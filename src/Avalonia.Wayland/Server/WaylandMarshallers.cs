using System;
using Avalonia.Threading;

namespace Avalonia.Wayland.Server;

/// <summary>
/// Marshallers used by generated cross-thread proxies.
/// </summary>
/// <remarks>
/// The UI-thread marshaller is stateless and exposed as a singleton. The
/// worker-thread marshaller is per-worker and exposed via
/// <see cref="WaylandWorkerClient.Marshaller"/> — there is no global worker.
/// </remarks>
internal static class WaylandMarshallers
{
    public static Action<Action, DispatcherPriority> UIThread { get; } = (action, priority) =>
    {
        Dispatcher.UIThread.Post(action, priority);
    };
}
