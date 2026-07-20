using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Xunit;

namespace Avalonia.UnitTests.WpfCompare;

// WPF backend for the shared DispatcherExecutionContextTests (compiled from
// ..\Avalonia.Base.UnitTests\DispatcherExecutionContextTests.cs with WPFCOMPARE defined).
// The Avalonia project provides equivalent types in the Avalonia.Base.UnitTests namespace.

// The WPF dispatcher requires an STA thread, so the cross-framework [CrossFact] maps to [StaFact] here.
public sealed class CrossFactAttribute : StaFactAttribute
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
    // until a sentinel posted at that priority runs. WPF's PushFrame is static (Avalonia's is an instance
    // method) - that is the only difference from the Avalonia backend.
    public static void DrainQueue(Dispatcher dispatcher, DispatcherPriority priority)
    {
        var frame = new DispatcherFrame();
        dispatcher.InvokeAsync(() => frame.Continue = false, priority);
        PushFrame(dispatcher, frame);
    }

    // Pumps the dispatcher until the frame is stopped. WPF's PushFrame is static (Avalonia's is an
    // instance method) - that is the only difference from the Avalonia backend.
    public static void PushFrame(Dispatcher dispatcher, DispatcherFrame frame) => Dispatcher.PushFrame(frame);
}
