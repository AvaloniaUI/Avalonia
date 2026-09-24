using System;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding;

namespace Avalonia.Wayland.Embedding.Tests;

internal static class WaylandTestHarness
{
    /// <summary>
    /// Drive the UI thread until it settles: each round runs posted dispatcher jobs (the compositor→UI
    /// drain is posted at <c>BeforeRender</c> priority, so this applies toplevel-mapped / surface-commit
    /// events) and then forces a render-timer tick (which lays out, renders the surface view, and posts the
    /// frame-rendered job back to the compositor). A few rounds cover the multi-pass settle of a freshly
    /// shown window.
    /// </summary>
    public static void Pump(int rounds = 8)
    {
        var dispatcher = Dispatcher.UIThread;
        for (var i = 0; i < rounds; i++)
        {
            dispatcher.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        dispatcher.RunJobs();
    }

    /// <summary>Make the compositor process the client's queued requests, then settle the UI.</summary>
    public static void RoundtripAndPump(WaylandTestClient client, int rounds = 8)
    {
        client.Roundtrip();
        Pump(rounds);
    }

    /// <summary>
    /// Settle from the server side with no client request to roundtrip — used after the client
    /// disconnects. The server roundtrip parks until the compositor has processed everything pending
    /// (including the just-observed disconnect, which the loop drains ahead of the sentinel) and applies the
    /// resulting events (e.g. toplevel-unmapped) on the UI thread.
    /// </summary>
    public static void SettleServerAndPump(int rounds = 8)
    {
        WaylandEmbeddingSubcompositor.Roundtrip();
        Pump(rounds);
    }
}
