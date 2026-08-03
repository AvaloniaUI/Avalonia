using System.Threading;

namespace Avalonia.Controls;

/// <summary>
/// Represents the event arguments of <see cref="IResourceHost.ResourcesChanged"/>.
/// The <see cref="SequenceNumber"/> identifies the changes.
/// </summary>
/// <param name="SequenceNumber">The sequence number used to identify the changes.</param>
/// <remarks>
/// For performance reasons, this type is a struct.
/// Avoid using a default instance of this type or its default constructor, call <see cref="Create"/> instead.
/// </remarks>
public readonly record struct ResourcesChangedEventArgs(int SequenceNumber)
{
    private static int s_lastSequenceNumber;

    /// <summary>
    /// Creates a new instance of <see cref="ResourcesChangedEventArgs"/> with an auto-incremented sequence number.
    /// </summary>
    /// <returns></returns>
    public static ResourcesChangedEventArgs Create()
        => new(Interlocked.Increment(ref s_lastSequenceNumber));
}
