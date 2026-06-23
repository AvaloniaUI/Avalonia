using System;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Base.UnitTests;

// Avalonia backend for the shared DispatcherExecutionContextTests. The WPF project
// (Avalonia.UnitTests.WpfCompare) provides its own version of these two types in its own namespace.

public sealed class CrossFactAttribute : FactAttribute
{
    public CrossFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }
}

internal static class DispatcherTestServices
{
    // Drains every dispatcher operation queued at or above <paramref name="priority"/> by pumping a frame
    // until a sentinel posted at that priority runs. Replaces the Avalonia-only impl.ExecuteSignal() so the
    // same body works against WPF too.
    public static void DrainQueue(Dispatcher dispatcher, DispatcherPriority priority)
    {
        var frame = new DispatcherFrame();
        dispatcher.InvokeAsync(() => frame.Continue = false, priority);
        PushFrame(dispatcher, frame);
    }

    // Pumps the dispatcher until the frame is stopped. Avalonia's PushFrame is an instance method
    // (WPF's is static) - that is the only difference from the WPF backend.
    public static void PushFrame(Dispatcher dispatcher, DispatcherFrame frame) => dispatcher.PushFrame(frame);
}
