using System.Threading;

namespace Avalonia.Controls;

internal record struct ResourcesChangedToken(int SequenceNumber)
{
    private static int s_lastSequenceNumber;

    public static ResourcesChangedToken Create()
        => new(Interlocked.Increment(ref s_lastSequenceNumber));
}
